using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace WorkflowAppWorker.Framework
{
    public static class ProgramAzureAppConfiguration
    {
        private static readonly TimeSpan azureAppConfigRefreshCacheExpiration =
            TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initializes the connection with an azure app config service using a connection string.
        /// Overload taking a set of refresh keys.
        /// This is intended to be used when the runtime context's identity has not been granted any specific permissions
        /// as it relates to the azure App Config Service.
        /// NOTE: Since we need to connect to a Key Vault, we did not pursue connecting to Azure App Config using a managed identity.
        /// and therefore are still relying on using a connection string (persited in Key Vault).
        /// </summary>
        /// <param name="options">App Config Setup Options</param>
        /// <param name="appConfigConnectionString">REQUIRED app config connection string</param>
        /// <param name="appConfigurationFilterKeys">filter keys</param>
        /// <param name="appConfigurationRefreshKeys">refresh keys</param>
        public static void SetAzureAppConfigurationOptions(
            AzureAppConfigurationOptions options,
            string appConfigConnectionString,
            IEnumerable<string> appConfigurationFilterKeys,
            IEnumerable<string> appConfigurationRefreshKeys) =>
                options
                    .Connect(appConfigConnectionString)
                    // Avoid frequent refreshes of feature flags.  Default cache is 30 seconds
                    .UseFeatureFlags(featureFlagOptions => {
                        featureFlagOptions.CacheExpirationInterval = TimeSpan.FromDays(30);
                    })
                    .SetupAzureAppConfigurationRefresh(appConfigurationFilterKeys, appConfigurationRefreshKeys);

        /// <summary>
        /// Initializes the connection with an azure app config service using a connection string.
        /// Overload taking a single refresh key.
        /// This is intended to be used when the runtime context's identity has not been granted any specific permissions
        /// as it relates to the azure App Config Service.
        /// </summary>
        /// <param name="options">App Config Setup Options</param>
        /// <param name="appConfigConnectionString">REQUIRED app config connection string</param>
        /// <param name="appConfigurationFilterKey">filter key</param>
        /// <param name="appConfigurationRefreshKey">refresh key</param>
        public static void SetAzureAppConfigurationOptions(
            AzureAppConfigurationOptions options,
            string appConfigConnectionString,
            string appConfigurationFilterKey,
            string appConfigurationRefreshKey) =>
                options
                    .Connect(appConfigConnectionString)
                    // Avoid frequent refreshes of feature flags.  Default cache is 30 seconds
                    .UseFeatureFlags(featureFlagOptions => {
                        featureFlagOptions.CacheExpirationInterval = TimeSpan.FromDays(30);
                    })
                    .SetupAzureAppConfigurationRefresh(appConfigurationFilterKey, appConfigurationRefreshKey);

        private static void SetupAzureAppConfigurationRefresh(
            this AzureAppConfigurationOptions options,
            string appConfigurationFilterKey,
            string appConfigurationRefreshKey)
        {
            options
                .Select(keyFilter: string.Concat(appConfigurationFilterKey, ":*"))
                .TrimKeyPrefix(string.Concat(appConfigurationFilterKey, ":"))
                // Avoid frequent refreshes of feature flags.  Default cache is 30 seconds
                .UseFeatureFlags(featureFlagOptions => {
                    featureFlagOptions.CacheExpirationInterval = TimeSpan.FromDays(30);
                })
                .ConfigureRefresh(refreshOptions =>
                    SetAzureAppConfigurationRefreshOptions(
                        refreshOptions,
                        appConfigurationRefreshKey))

                // Configure Azure App Config use Azure Key Vault, connecting with managed
                //  identity for Azure resources
                .ConfigureKeyVault(keyVault =>
                    keyVault.SetCredential(new DefaultAzureCredential()));
        }

        private static void SetupAzureAppConfigurationRefresh(
            this AzureAppConfigurationOptions options,
            IEnumerable<string> appConfigurationFilterKeys,
            IEnumerable<string> appConfigurationRefreshKeys)
        {
            foreach ( string filterKey in appConfigurationFilterKeys )
            {
                _ = options
                    .Select(string.Concat(filterKey, ":*"))
                    .TrimKeyPrefix(string.Concat(filterKey, ":"));
            }

            options
                .ConfigureRefresh(refreshOptions =>
                    SetAzureAppConfigurationRefreshOptions(
                        refreshOptions,
                        appConfigurationRefreshKeys))

                // Configure Azure App Config use Azure Key Vault, connecting with managed
                //  identity for Azure resources
                .ConfigureKeyVault(keyVault =>
                    keyVault.SetCredential(new DefaultAzureCredential()));
        }

        // Register a refresh key with the AzureAppConfigurationRefreshOptions and set refresh
        //  cache expiration.
        private static AzureAppConfigurationRefreshOptions SetAzureAppConfigurationRefreshOptions(
            AzureAppConfigurationRefreshOptions refreshOption,
            string refreshKey) =>
                // Any one of these options can be used to referesh the configuration in the
                // project. Refresh or reload the configuration based on the changes in a
                // key such as "EquityApi:Version"
                refreshOption
                    .Register(
                        key: refreshKey,
                        refreshAll: true,
                        label: LabelFilter.Null)

                    // Refresh or reload the configuration based on cache expiration time.
                    .SetCacheExpiration(azureAppConfigRefreshCacheExpiration);

        // Register a collection of refresh keys with the AzureAppConfigurationRefreshOptions and
        //  set the refresh cache expiration.
        private static AzureAppConfigurationRefreshOptions SetAzureAppConfigurationRefreshOptions(
            AzureAppConfigurationRefreshOptions refreshOptions,
            IEnumerable<string> refreshKeys)
        {
            // register all refresh keys
            foreach (string refreshKey in refreshKeys)
            {
                _ = refreshOptions.Register(
                        key: refreshKey,
                        refreshAll: true,
                        label: LabelFilter.Null);
            }

            // Refresh or reload the configuration based on cache expiration time.
            return refreshOptions.SetCacheExpiration(azureAppConfigRefreshCacheExpiration);
        }
    }
}
