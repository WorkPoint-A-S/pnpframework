﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PnP.Framework.Http
{
    /// <summary>
    /// Retry handler for http requests
    /// Based upon: https://github.com/microsoftgraph/msgraph-sdk-dotnet-core/blob/dev/src/Microsoft.Graph.Core/Extensions/HttpRequestMessageExtensions.cs
    /// </summary>
    internal class RetryHandler : DelegatingHandler
    {
        private const string RETRY_AFTER = "Retry-After";
        private const string RETRY_ATTEMPT = "Retry-Attempt";
        internal const int MAXDELAY = 300;

        #region Construction
        public RetryHandler()
        {
        }
        #endregion

        internal bool UseRetryAfterHeader { get; set; } = true;
        internal int MaxRetries { get; set; } = 10;
        internal int DelayInSeconds { get; set; } = 1;
        internal bool IncrementalDelay { get; set; } = true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposing will prevent cloning of the request needed for the retry")]
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int retryCount = 0;

            while (true)
            {
                HttpResponseMessage response = null;

                try
                {
                    response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    if (!ShouldRetry(response.StatusCode))
                    {
                        return response;
                    }

                    if (retryCount >= MaxRetries)
                    {
                        // Drain response content to free connections. Need to perform this
                        // before retry attempt and before the TooManyRetries ServiceException.
                        if (response.Content != null)
                        {
#if NET5_0_OR_GREATER
                            await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                            await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
                        }

                        throw new Exception($"Too many http request retries: {retryCount}");
                    }
                }
                catch (Exception ex)
                {
                    // Find innermost exception and check if it is a SocketException
                    Exception innermostEx = ex;
                    
                    while (innermostEx.InnerException != null) innermostEx = innermostEx.InnerException;
                    if (!(innermostEx is SocketException))
                    {
                        throw;
                    }

                    if (retryCount >= MaxRetries)
                    {
                        throw;
                    }
                }

                // Drain response content to free connections. Need to perform this
                // before retry attempt and before the TooManyRetries ServiceException.
                if (response?.Content != null)
                {
#if NET5_0_OR_GREATER
                    await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                    await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
                }

                // Call Delay method to get delay time from response's Retry-After header or by exponential backoff 
                Task delay = Delay(response, retryCount, DelayInSeconds, cancellationToken);

                // general clone request with internal CloneAsync (see CloneAsync for details) extension method 
                // do not dispose this request as that breaks the request cloning
                request = await request.CloneAsync().ConfigureAwait(false);
                // Increase retryCount and then update Retry-Attempt in request header if needed
                retryCount++;
                AddOrUpdateRetryAttempt(request, retryCount);

                // Delay time
                await delay.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Update Retry-Attempt header in the HTTP request
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>needs to be sent.</param>
        /// <param name="retryCount">Retry times</param>
        private void AddOrUpdateRetryAttempt(HttpRequestMessage request, int retryCount)
        {
            if (UseRetryAfterHeader)
            {
                if (request.Headers.Contains(RETRY_ATTEMPT))
                {
                    request.Headers.Remove(RETRY_ATTEMPT);
                }
                request.Headers.Add(RETRY_ATTEMPT, retryCount.ToString());
            }
        }

        private Task Delay(HttpResponseMessage response, int retryCount, int delay, CancellationToken cancellationToken)
        {
            double delayInSeconds = delay;

            if (UseRetryAfterHeader && response != null && response.Headers.TryGetValues(RETRY_AFTER, out IEnumerable<string> values))
            {
                // Can we use the provided retry-after header?
                string retryAfter = values.First();
                if (Int32.TryParse(retryAfter, out int delaySeconds))
                {
                    delayInSeconds = delaySeconds;
                }
            }
            else
            {
                // Custom delay
                if (IncrementalDelay)
                {
                    // Incremental delay, the wait time between each delay exponentially gets bigger
                    double power = Math.Pow(2, retryCount);
                    delayInSeconds = power * delay;
                }
                else
                {
                    // Linear delay
                    delayInSeconds = delay;
                }
            }

            // If the delay goes beyond our max wait time for a delay then cap it
            TimeSpan delayTimeSpan = TimeSpan.FromSeconds(Math.Min(delayInSeconds, RetryHandler.MAXDELAY));

            return Task.Delay(delayTimeSpan, cancellationToken);
        }

        internal static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return (statusCode == HttpStatusCode.ServiceUnavailable ||
                    statusCode == HttpStatusCode.GatewayTimeout ||
                    statusCode == (HttpStatusCode)429);
        }
    }
}
