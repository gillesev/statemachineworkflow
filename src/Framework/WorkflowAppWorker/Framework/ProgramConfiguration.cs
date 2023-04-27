using System.Reflection;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace WorkflowAppWorker.Framework
{
    public static class ProgramConfiguration
    {
        /// <summary>
        /// Default KV key for the Azure App Config Service Connection String.
        /// </summary>
        private const string DefaultAppCfgServiceConnectionStringKeyName = "AppConfigurationConnectionString";
        /// <summary>
        /// KV key for the Azure App Config Service Connection String to override default one.
        /// </summary>
        private const string AppCfgServiceConnectionStringKeyName = "AppCfgConnectionStringKVKey";
        /// <summary>
        /// Env variable name to indicate whether the runtime should use a locally defined configuration or pull it from a remote endpoint (Azure App Config Service)
        /// </summary>
        public const string UseLocalConfigurationKeyName = "UseLocalConfiguration";

        /// <summary>
        /// Configure the application configuration using optionally Azure App Configuration with a list of
        /// filter/refresh keys.
        /// </summary>
        public static void ConfigureAppConfiguration(
            IConfigurationBuilder configBuilder,
            IEnumerable<string> appConfigurationFilterKeys,
            IEnumerable<string> appConfigurationRefreshKeys,
            IConfiguration processLocalConfig = null) =>
                ConfigureAppConfiguration(
                    configBuilder,
                    appConfigurationFilterKeys,
                    appConfigurationRefreshKeys,
                    (configBuilder, connectionString, filterKeys, refreshKeys) =>
                        configBuilder.AddAzureAppConfiguration(options =>
                            ProgramAzureAppConfiguration.SetAzureAppConfigurationOptions(
                                options,
                                connectionString,
                                filterKeys,
                                refreshKeys)
                        ),
                    processLocalConfig);

        /// <summary>
        /// Loads app configuration locally or from azure key vault + azure app config.
        /// </summary>
        private static void ConfigureAppConfiguration<TFilterKey, TRefreshKey>(
            IConfigurationBuilder configBuilder,
            TFilterKey appConfigurationFilterKey,
            TRefreshKey appConfigurationRefreshKey,
            Action<IConfigurationBuilder, string, TFilterKey, TRefreshKey> configureAzureAppConfigurationAction,
            IConfiguration processLocalConfig = null)
        {
            if ( processLocalConfig == null )
            {
                processLocalConfig =
                    GetAppSettingsAndEnvironmentVariables();
            }
            // To switch between local configurations and azure configuration
            string useLocalConfiguration = processLocalConfig[UseLocalConfigurationKeyName];
            var useAzureConfig =
                string.IsNullOrEmpty(useLocalConfiguration) ||
                !bool.Parse(useLocalConfiguration);

            if ( useAzureConfig )
            {
                // step 1, load configuration from Key Vault.
                IConfiguration localConfigsWithKeyVaultSecrets =
                    GetAzureKeyVaultSecrets(configBuilder, processLocalConfig);

                // step 2, do we have an override key vault lookup key to get the azure app config connection string?

                string appCfgServiceConnectionStringKeyName = processLocalConfig.GetValue<string>(AppCfgServiceConnectionStringKeyName);

                // retrieve the key vault lookup key
                var appConfigConnectionString = (string.IsNullOrWhiteSpace(appCfgServiceConnectionStringKeyName)) ?
                    localConfigsWithKeyVaultSecrets[DefaultAppCfgServiceConnectionStringKeyName] :
                    localConfigsWithKeyVaultSecrets[appCfgServiceConnectionStringKeyName];

                // bail if the connection string is not defined.
                if ( string.IsNullOrEmpty(appConfigConnectionString) )
                {
                    throw new KeyNotFoundException($"{appCfgServiceConnectionStringKeyName} key not found in Key Vault. Cannot connect to the Azure App Config. Exiting.");
                }

                configureAzureAppConfigurationAction(
                    configBuilder,
                    appConfigConnectionString,
                    appConfigurationFilterKey,
                    appConfigurationRefreshKey);

                // Adding an optional json file allows overriding values that come from
                // Key Vault and Azure App Config
                string appSettingsOverridesJsonPath =
                    JsonFilename(ConfigurationConstants.CONFIG_OVERRIDES_APPSETTING);
                configBuilder.AddJsonFile(appSettingsOverridesJsonPath, true);
            }
        }

        /// <summary>
        /// Read the connection strings from azure key-vault using managed identity
        /// </summary>
        private static IConfiguration GetAzureKeyVaultSecrets(
            IConfigurationBuilder configBuilder,
            IConfiguration localConfigs)
        {
            string keyVaultUrl = localConfigs["KeyVault:BaseUrl"];

            var keyVaultReloadIntervalInMinutes = GetConfigValue(localConfigs, "KeyVault:ReloadIntervalInMinutes", 5);
            var keyVaultCacheFetchedEntries = GetConfigValue(localConfigs, "KeyVault:CacheFetchedEntries", false);
            var keyVaultCacheEntryExpiryInMinutes = GetConfigValue(localConfigs, "KeyVault:CacheEntryExpiryInMinutes", 30);

            // Set the AzureKeyVaultConfigurationOptions, including ReloadInterval from configuration
            var azureKeyVaultConfigurationOptions = new AzureKeyVaultConfigurationOptions();
            localConfigs.Bind(nameof(AzureKeyVaultConfigurationOptions), azureKeyVaultConfigurationOptions);

            // If not specified by config, set a ReloadInterval of 5 min
            if ( azureKeyVaultConfigurationOptions.ReloadInterval.GetValueOrDefault() == default(TimeSpan) )
            {
                azureKeyVaultConfigurationOptions.ReloadInterval = TimeSpan.FromMinutes(keyVaultReloadIntervalInMinutes);
            }

            // Set the SecretClientOptions, including Retry from configuration
            var secretClientOptions = new SecretClientOptions();
            localConfigs.Bind(nameof(SecretClientOptions), secretClientOptions);

            var deafultSecretClientOptions = new SecretClientOptions();
            // If not specified by config, set a Retry with exponential backoff
            if ( CompareRetryOptions(secretClientOptions.Retry, deafultSecretClientOptions.Retry) )
            {
                secretClientOptions = new SecretClientOptions
                {
                    Retry =
                    {
                        Delay= TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(16),
                        MaxRetries = 5,
                        Mode = RetryMode.Exponential
                    }
                };
            }

            if ( keyVaultCacheFetchedEntries )
            {
                secretClientOptions.AddPolicy(new KeyVaultProxy(TimeSpan.FromMinutes(keyVaultCacheEntryExpiryInMinutes)), HttpPipelinePosition.PerCall);
            }

            if ( !string.IsNullOrEmpty(keyVaultUrl) )
            {
                var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(), secretClientOptions);
                //_ = configBuilder.AddAzureKeyVault(client, azureKeyVaultConfigurationOptions);
            }

            return configBuilder.Build();
        }

        private static int GetConfigValue(IConfiguration localConfig, string key, int defaultValue)
        {
            var keyStringValue = localConfig[key];
            if ( !int.TryParse(keyStringValue, out var keyIntValue) )
            {
                keyIntValue = defaultValue;
            }

            return keyIntValue;
        }

        private static bool GetConfigValue(IConfiguration localConfig, string key, bool defaultValue)
        {
            var keyStringValue = localConfig[key];
            if ( !bool.TryParse(keyStringValue, out var keyboolValue) )
            {
                keyboolValue = defaultValue;
            }

            return keyboolValue;
        }

        /// <summary>
        /// Returns configuration from appsettings.json union environment variables
        /// </summary>
        private static IConfiguration GetAppSettingsAndEnvironmentVariables()
        {
            string environment =
                Environment.GetEnvironmentVariable(ConfigurationConstants.ENV_ASPNETCORE) ?? ConfigurationConstants.ENV_DEFAULT;
            string appSettingsEnvironmentFilenameBase =
                string.Concat(ConfigurationConstants.CONFIG_APPSETTING, ".", environment);

            string appSettingsJsonPath =
                JsonFilename(ConfigurationConstants.CONFIG_APPSETTING);
            string appSettingsEnvironmentJsonPath =
                JsonFilename(appSettingsEnvironmentFilenameBase);

            return new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile(appSettingsJsonPath, optional: false, reloadOnChange: false)
                .AddJsonFile(appSettingsEnvironmentJsonPath, optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static string JsonFilename(string filenameBase) =>
            string.Concat(filenameBase, ".json");

        private static bool CompareRetryOptions(RetryOptions first, RetryOptions second)
        {
            return first.Delay == second.Delay &&
                   first.MaxDelay == second.MaxDelay &&
                   first.MaxRetries == second.MaxRetries &&
                   first.Mode == second.Mode &&
                   first.NetworkTimeout == second.NetworkTimeout;
        }
    }
}
