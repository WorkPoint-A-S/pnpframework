using Microsoft.VisualStudio.TestTools.UnitTesting;
using PnP.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PnP.Framework.Test.Utilities
{
    [TestClass]
    public class RESTUtilitiesTests
    {
        private const string NotFoundPage = "<html><head><title>404 FILE NOT FOUND</title></head><body>404 FILE NOT FOUND</body></html>";

        [TestMethod]
        public async Task RetryOnTransientNotFound_NotFoundThenSuccess_ReturnsResult()
        {
            var calls = 0;

            var result = await RESTUtilities.RetryOnTransientNotFoundAsync(() =>
            {
                calls++;
                if (calls == 1)
                {
                    throw new Exception(NotFoundPage);
                }
                return Task.FromResult("ok");
            }, "/_api/web/SetChromeOptions", _ => TimeSpan.Zero);

            Assert.AreEqual("ok", result);
            Assert.AreEqual(2, calls);
        }

        [TestMethod]
        public async Task RetryOnTransientNotFound_PersistentNotFound_RethrowsAfterRetries()
        {
            var calls = 0;

            var ex = await Assert.ThrowsExceptionAsync<Exception>(() => RESTUtilities.RetryOnTransientNotFoundAsync<string>(() =>
            {
                calls++;
                throw new Exception(NotFoundPage);
            }, "/_api/web/SetChromeOptions", _ => TimeSpan.Zero));

            StringAssert.Contains(ex.Message, "404 FILE NOT FOUND");
            Assert.AreEqual(RESTUtilities.TransientNotFoundRetryCount + 1, calls);
        }

        [TestMethod]
        public async Task RetryOnTransientNotFound_OtherError_IsNotRetried()
        {
            var calls = 0;

            await Assert.ThrowsExceptionAsync<Exception>(() => RESTUtilities.RetryOnTransientNotFoundAsync<string>(() =>
            {
                calls++;
                throw new Exception("{\"error\":{\"message\":\"Access denied.\"}}");
            }, "/_api/web/SetChromeOptions", _ => TimeSpan.Zero));

            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void IsTransientNotFound_OnlyMatchesPlainExceptionWithNotFoundPage()
        {
            Assert.IsTrue(RESTUtilities.IsTransientNotFound(new Exception(NotFoundPage)));
            Assert.IsFalse(RESTUtilities.IsTransientNotFound(new Exception("Access denied.")));
            Assert.IsFalse(RESTUtilities.IsTransientNotFound(new FileNotFoundException("404 FILE NOT FOUND")));
            Assert.IsFalse(RESTUtilities.IsTransientNotFound(new InvalidOperationException("404 FILE NOT FOUND")));
            Assert.IsFalse(RESTUtilities.IsTransientNotFound(null));
        }

        [TestMethod]
        public async Task RetryOnTransientNotFound_UsesDelayPerRetryAttempt()
        {
            var requestedAttempts = new List<int>();

            await Assert.ThrowsExceptionAsync<Exception>(() => RESTUtilities.RetryOnTransientNotFoundAsync<string>(
                () => throw new Exception(NotFoundPage),
                "/_api/web/SetChromeOptions",
                attempt =>
                {
                    requestedAttempts.Add(attempt);
                    return TimeSpan.Zero;
                }));

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, requestedAttempts);
            Assert.AreEqual(TimeSpan.FromSeconds(10), RESTUtilities.TransientNotFoundRetryDelay(1));
            Assert.AreEqual(TimeSpan.FromSeconds(30), RESTUtilities.TransientNotFoundRetryDelay(3));
        }
    }
}
