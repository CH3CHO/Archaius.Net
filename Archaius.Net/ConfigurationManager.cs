using System;
using System.Linq;
using System.Reflection;
using Archaius.Dynamic;
using log4net;

namespace Archaius
{
    /// <summary>
    /// The configuration manager is a central place where it manages the system wide Configuration and deployment context.
    /// </summary>
    public class ConfigurationManager
    {
        public static readonly string UrlConfigName = "Archaius.DynamicPropertyFactory.UrlConfig";
        public static readonly string AppSettingsConfigName = "Archaius.DynamicPropertyFactory.AppSettingsConfig";
        public static readonly string EnvConfigName = "Archaius.DynamicPropertyFactory.EnvConfig";
        public static readonly string ApplicationProperties = "ApplicationProperties";

        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static volatile AbstractConfiguration m_Instance;
        private static volatile bool m_CustomConfigurationInstalled;
        private static readonly object m_ClassLock = new object();

        public static bool IsConfigurationInstalled
        {
            get
            {
                lock (m_ClassLock)
                {
                    return m_CustomConfigurationInstalled;
                }
            }
        }

        /// <summary>
        /// Install the system wide configuration with the ConfigurationManager. This will also install 
        /// the configuration with the <see cref="DynamicPropertyFactory"/> by calling <see cref="DynamicPropertyFactory.InitWithConfigurationSource"/>.
        /// This call can be made only once, otherwise IllegalStateException will be thrown.
        /// </summary>
        /// <param name="config"></param>
        public static void Install(AbstractConfiguration config)
        {
            lock (m_ClassLock)
            {
                if (m_CustomConfigurationInstalled)
                {
                    throw new InvalidOperationException("A non-default configuration is already installed");
                }
                SetDirect(config);
                if (DynamicPropertyFactory.BackingConfigurationSource != config)
                {
                    DynamicPropertyFactory.InitWithConfigurationSource(config);
                }
            }
        }

        /// <summary>
        /// Get the current system wide configuration. If there has not been set, it will return a default
        /// <see cref="ConcurrentCompositeConfiguration"/> which contains a SystemConfiguration from Apache Commons
        /// Configuration and a <see cref="DynamicUrlConfiguration"/>
        /// </summary>
        /// <returns></returns>
        public static AbstractConfiguration GetConfigInstance()
        {
            if (m_Instance == null)
            {
                lock (typeof(ConfigurationManager))
                {
                    if (m_Instance == null)
                    {
                        m_Instance = CreateDefaultConfigInstance();
                    }
                }
            }
            return m_Instance;
        }

        private static AbstractConfiguration CreateDefaultConfigInstance()
        {
            var config = new ConcurrentCompositeConfiguration();
            try
            {
                var defaultURLConfig = new DynamicUrlConfiguration();
                config.AddConfiguration(defaultURLConfig, UrlConfigName);
            }
            catch (Exception e)
            {
                m_Log.Warn("Failed to create default dynamic configuration", e);
            }
            var appSettingsConfig = new AppSettingsConfiguration();
            config.AddConfiguration(appSettingsConfig, AppSettingsConfigName);
            var envConfig = new EnvironmentConfiguration();
            config.AddConfiguration(envConfig, EnvConfigName);
            var appOverrideConfig = new ConcurrentCompositeConfiguration();
            config.AddConfiguration(appOverrideConfig, ApplicationProperties);
            config.SetContainerConfigurationIndex(config.GetIndexOfConfiguration(appOverrideConfig));
            return config;
        }

        internal static void SetDirect(AbstractConfiguration config)
        {
            lock (m_ClassLock)
            {
                if (m_Instance != null)
                {
                    // Transfer properties which are not in conflict with new configuration
                    foreach (var key in m_Instance.Keys)
                    {
                        var value = m_Instance.GetProperty(key);
                        if (value != null && !config.ContainsKey(key))
                        {
                            config.SetProperty(key, value);
                        }
                    }
                    // Transfer listeners
                    foreach (var handler in m_Instance.ConfigurationChangedEventHandlers)
                    {
                        if (handler.Method.DeclaringType == typeof(DynamicProperty))
                        {
                            // No need to transfer the fast property listener as it should be set later
                            // with the new configuration
                            continue;
                        }
                        config.ConfigurationChanged += handler;
                    }
                }
                RemoveDefaultConfiguration();
                m_Instance = config;
                m_CustomConfigurationInstalled = true;
            }
        }

        private static void RemoveDefaultConfiguration()
        {
            lock (m_ClassLock)
            {
                if (m_Instance == null || m_CustomConfigurationInstalled)
                {
                    return;
                }
                var defaultConfig = (ConcurrentCompositeConfiguration)m_Instance;
                // Stop loading of the configuration
                var defaultFileConfig = (DynamicUrlConfiguration)defaultConfig.GetConfiguration(UrlConfigName);
                if (defaultFileConfig != null)
                {
                    defaultFileConfig.StopLoading();
                }
                // Find the listener and remove it so that DynamicProperty will no longer receives 
                // callback from the default configuration source
                var dynamicPropertyEventHandler =
                    defaultConfig.ConfigurationChangedEventHandlers.FirstOrDefault(handler => handler.Method.DeclaringType == typeof(DynamicProperty));
                if (dynamicPropertyEventHandler != null)
                {
                    defaultConfig.ConfigurationChanged -= dynamicPropertyEventHandler;
                }
                m_Instance = null;
            }
        }
    }
}