using PnP.Framework.Utilities.JsonConverters;
using System;
using System.Text.Json.Serialization;

namespace PnP.Framework.ALM
{
    /// <summary>
    /// App/solution metadata for apps stored in the corporate catalog
    /// </summary>
    public class AppMetadata
    {
        /// <summary>
        /// Unique ID of the library list item of the app/solution.
        /// </summary>
        [JsonPropertyName("ID")]
        public Guid Id { get; set; }
        /// <summary>        
        /// </summary>
        public Guid? AadAppId { get; set; }
        /// <summary>        
        /// </summary>
        public string AadPermissions { get; set; }
        /// <summary>
        /// Returns version of the app / solution int the app catalog.
        /// </summary>
        [JsonConverter(typeof(VersionConverter))]
        public Version AppCatalogVersion { get; set; }
        /// <summary>
        /// Returns whether an existing instance of an app/solution can be upgraded. 
        /// True if there's newer version available in app catalog compared to instance in site.
        /// </summary>
        public bool CanUpgrade { get; set; }
        /// <summary>
        /// </summary>
        public string CDNLocation { get; set; }
        /// <summary>
        /// </summary>
        public bool ContainsTenantWideExtension { get; set; }
        /// <summary>
        /// </summary>
        public bool CurrentVersionDeployed { get; set; }
        /// <summary>
        /// Returns whether app/solution has been deployed to the context site. 
        /// True if particular app/solution has been installed to the site.
        /// </summary>
        public bool Deployed { get; set; }
        /// <summary>
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// Returns version of the installed app/solution in the site context. 
        /// </summary>
        [JsonConverter(typeof(VersionConverter))]
        public Version InstalledVersion { get; set; }
        /// <summary>
        /// Returns whether app/solution is SharePoint Framework client-side solution. 
        /// True for SPFx, False for app/add-in.
        /// </summary>
        public bool IsClientSideSolution { get; set; }
        /// <summary>
        /// </summary>
        public bool IsEnabled { get; set; }
        /// <summary>
        /// </summary>
        public bool IsPackageDefaultSkipFeatureDeployment { get; set; }
        /// <summary>
        /// </summary>
        public bool IsValidAppPackage { get; set; }
        /// <summary>
        /// </summary>
        public Guid ProductId { get; set; }
        /// <summary>
        /// </summary>
        public string ShortDescription { get; set; }
        /// <summary>
        /// Returns whether app/solution is deployed to all sites.
        /// True if particular app/solution has been deployed to all sites.
        /// </summary>
        public bool SkipDeploymentFeature { get; set; }
        /// <summary>
        /// </summary>
        public bool SupportTeamsTabs { get; set; }
        /// <summary>
        /// </summary>
        public string ThumbnailUrl { get; set; }
        /// <summary>
        /// Title of the solution
        /// </summary>
        public string Title { get; set; }
    }
}
