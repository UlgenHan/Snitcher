namespace Snitcher.UI.Desktop.Configuration
{
    /// <summary>
    /// Configuration settings for the UI application
    /// </summary>
    public static class UIConfiguration
    {
        /// <summary>
        /// Application metadata
        /// </summary>
        public static class AppInfo
        {
            public const string Name = "Snitcher";
            public const string Version = "1.0.0";
            public const string Description = "Advanced HTTP/HTTPS Proxy Inspector";
        }

        /// <summary>
        /// UI behavior settings
        /// </summary>
        public static class Behavior
        {
            public const bool EnableAnimations = true;
            public const bool EnableDebugMode = false;
            public const int MaxFlowHistory = 1000;
            public const int ProxyDefaultPort = 8080;
            public const string ProxyDefaultHost = "127.0.0.1";
        }

        /// <summary>
        /// Feature flags
        /// </summary>
        public static class Features
        {
            public const bool EnableHttpsInterception = true;
            public const bool EnableRequestBuilder = true;
            public const bool EnableAutomation = true;
            public const bool EnableCollections = true;
            public const bool EnableWorkspaceManagement = true;
        }

        /// <summary>
        /// File paths and storage settings
        /// </summary>
        public static class Storage
        {
            public const string DefaultDatabaseName = "snitcher.db";
            public const string FlowsFolderName = "Flows";
            public const string CertificatesFolderName = "Certificates";
            public const string LogsFolderName = "Logs";
        }
    }
}
