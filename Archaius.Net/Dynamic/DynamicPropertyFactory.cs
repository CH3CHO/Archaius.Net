using System;
using System.Reflection;
using Archaius.Source;
using log4net;

namespace Archaius.Dynamic
{
    /// <summary>
    ///  A factory that creates instances of dynamic properties and associates them with an underlying configuration
    /// or <see cref="IDynamicPropertySupport"/> where the properties could be changed dynamically at runtime.
    ///
    /// It is recommended to initialize this class with a configuration or DynamicPropertySupport before the first call to
    /// <see cref="GetInstance"/>. Otherwise, it will be lazily initialized with a <see cref="ConcurrentCompositeConfiguration"/>,
    /// where an EnvironmentConfiguration and <see cref="DynamicUrlConfiguration"/> will be added.
    ///
    /// Example:
    /// <pre>
    /// import com.netflix.config.DynamicProperty;
    /// 
    /// class MyClass
    /// {
    ///     private static DynamicIntProperty maxWaitMillis = DynamicPropertyFactory.GetInstance().GetIntProperty("myclass.sleepMillis", 250);
    ///     // ...
    ///
    ///     // Add a callback when this property is changed
    ///     maxWaitMillis.PropertyChanged  += () => {
    ///                                                 int currentValue = maxWaitMillis.get();
    ///                                                 // ...
    ///                                             };
    /// 
    ///     // ...
    /// 
    ///     // Wait for a configurable amount of time for condition to become true.
    ///     // Note that the time can be changed on-the-fly.
    ///     someCondition.WaitOne(maxWaitMillis.Value);
    /// 
    ///     // ...
    /// }
    /// </pre>
    /// 
    /// Please note that you should not cache the property value if you expect the value to change on-the-fly.
    /// For example, in the following code the change of the value is ineffective:
    /// <p/>
    /// <pre>
    /// int maxWaitMillis = DynamicPropertyFactory.GetInstance().GetIntProperty("myclass.sleepMillis", 250).Get();
    /// // ...
    /// someCondition.WaitOne(maxWaitMillis);
    /// </pre>
    /// </summary>
    public class DynamicPropertyFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly DynamicPropertyFactory m_Instance = new DynamicPropertyFactory();
        private static volatile IDynamicPropertySupport m_Config;
        private static volatile bool m_InitializedWithDefaultConfig;

        /// <summary>
        /// Get the instance to create dynamic properties.
        /// 
        /// It will first try to initialize itself with a default <see cref="ConcurrentCompositeConfiguration"/>, with the following two sub configurations:
        /// <ul>
        /// <li>An EnvironmentConfiguration, which contains all the AppConfig properties.</li>
        /// <li>A <see cref="DynamicUrlConfiguration"/>, which at a fixed interval polls a set of URLs specified via an AppSettings property (see <see cref="UrlConfigurationSource.ConfigUrlPropertyName"/>).</li>
        /// </ul>
        /// Between the two sub-configurations, the EnvironmentConfiguration will take the precedence when determining property values.
        /// </summary>
        /// <returns></returns>
        public static DynamicPropertyFactory GetInstance()
        {
            if (m_Config == null)
            {
                lock (typeof(ConfigurationManager))
                {
                    if (m_Config == null)
                    {
                        var configFromManager = ConfigurationManager.GetConfigInstance();
                        if (configFromManager != null)
                        {
                            InitWithConfigurationSource(configFromManager);
                            m_InitializedWithDefaultConfig = !ConfigurationManager.IsConfigurationInstalled;
                            m_Log.Info("DynamicPropertyFactory is initialized with configuration sources: " + configFromManager);
                        }
                    }
                }
            }
            return m_Instance;
        }

        private DynamicPropertyFactory()
        {
        }

        public static bool InitializedWithDefaultConfig
        {
            get
            {
                return m_InitializedWithDefaultConfig;
            }
        }

        /// <summary>
        /// Get the backing configuration from the factory. This can be cased to a <see cref="ConcurrentCompositeConfiguration"/>
        /// if the default configuration is installed.
        /// 
        /// For example:
        /// <pre>
        ///     Configuration config = DynamicPropertyFactory.GetInstance().BackingConfigurationSource;
        ///     if (DynamicPropertyFactory.InitializedWithDefaultConfig()) {
        ///         ConcurrentCompositeConfiguration composite = (ConcurrentCompositeConfiguration) config;
        ///         // ...
        ///     }
        /// </pre>
        /// </summary>
        public static object BackingConfigurationSource
        {
            get
            {
                if (m_Config is ConfigurationBackedDynamicPropertySupport)
                {
                    return ((ConfigurationBackedDynamicPropertySupport)m_Config).Configuration;
                }
                else
                {
                    return m_Config;
                }
            }
        }

        /// <summary>
        /// Initialize the factory with an AbstractConfiguration.
        /// 
        /// The initialization will register a ConfigurationListener with the configuration so that <see cref="DynamicProperty"/>
        /// will receives a callback and refresh its value when a property is changed in the configuration.
        /// 
        /// If the factory is already initialized with a default configuration source (see <see cref="GetInstance"/>), it will re-register
        /// itself with the new configuration source passed to this method. Otherwise, this method will throw IllegalArgumentException
        /// if it has been initialized with a different and non-default configuration source. This method should be only called once.
        /// </summary>
        /// <param name="config">Configuration to be attached with DynamicProperty</param>
        /// <returns>the instance of DynamicPropertyFactory</returns>
        public static DynamicPropertyFactory InitWithConfigurationSource(AbstractConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            lock (typeof(ConfigurationManager))
            {
                if (ConfigurationManager.IsConfigurationInstalled && config != ConfigurationManager.GetConfigInstance())
                {
                    throw new InvalidOperationException("ConfigurationManager is already initialized with configuration "
                                                        + ConfigurationManager.GetConfigInstance());
                }
                if (config is IDynamicPropertySupport)
                {
                    return InitWithConfigurationSource((IDynamicPropertySupport)config);
                }
                return InitWithConfigurationSource(new ConfigurationBackedDynamicPropertySupport(config));
            }
        }

        /// <summary>
        /// Initialize the factory with a <see cref="IDynamicPropertySupport"/>.
        ///
        /// The initialization will register a {@link PropertyListener} with the DynamicPropertySupport so that DynamicProperty
        /// will receives a callback and refresh its value when a property is changed.
        /// 
        /// If the factory is already initialized with a default configuration source (see <see cref="GetInstance"/>), it will re-register
        /// itself with the new configuration source passed to this method. Otherwise, this method will throw IllegalArgumentException
        /// if it has been initialized with a non-default configuration source. This method should be only called once.
        /// </summary>
        /// <param name="dynamicPropertySupport">DynamicPropertySupport to be associated with the DynamicProperty</param>
        /// <returns>the instance of DynamicPropertyFactory</returns>
        internal static DynamicPropertyFactory InitWithConfigurationSource(IDynamicPropertySupport dynamicPropertySupport)
        {
            if (dynamicPropertySupport == null)
            {
                throw new ArgumentNullException("dynamicPropertySupport");
            }
            lock (typeof(ConfigurationManager))
            {
                AbstractConfiguration configuration = null;
                if (dynamicPropertySupport is AbstractConfiguration)
                {
                    configuration = (AbstractConfiguration)dynamicPropertySupport;
                }
                else if (dynamicPropertySupport is ConfigurationBackedDynamicPropertySupport)
                {
                    configuration = ((ConfigurationBackedDynamicPropertySupport)dynamicPropertySupport).Configuration;
                }
                if (InitializedWithDefaultConfig)
                {
                    m_Config = null;
                }
                else if (m_Config != null && m_Config != dynamicPropertySupport)
                {
                    throw new InvalidOperationException("DynamicPropertyFactory is already initialized with a diffrerent configuration source: "
                                                        + m_Config);
                }
                if (ConfigurationManager.IsConfigurationInstalled
                    && (configuration != null && configuration != ConfigurationManager.GetConfigInstance()))
                {
                    throw new InvalidOperationException("ConfigurationManager is already initialized with configuration "
                                                        + ConfigurationManager.GetConfigInstance());
                }
                if (configuration != null && configuration != ConfigurationManager.GetConfigInstance())
                {
                    ConfigurationManager.SetDirect(configuration);
                }
                SetDirect(dynamicPropertySupport);
                return m_Instance;
            }
        }

        private static void SetDirect(IDynamicPropertySupport support)
        {
            lock (typeof(ConfigurationManager))
            {
                m_Config = support;
                DynamicProperty.RegisterWithDynamicPropertySupport(support);
                m_InitializedWithDefaultConfig = false;
            }
        }

        private static void CheckAndWarn(string propName)
        {
            if (m_Config == null)
            {
                m_Log.WarnFormat("DynamicProperty {0} is created without a configuration source for callback.", propName);
            }
        }

        /// <summary>
        /// Create a new property whose value is a string and subject to change on-the-fly.
        /// </summary>
        /// <param name="propName">property name</param>
        /// <param name="defaultValue">default value if the property is not defined in underlying configuration</param>
        /// <param name="propertyChangeCallback">an action to be called when the property is changed</param>
        /// <returns></returns>
        public DynamicStringProperty GetStringProperty(string propName, string defaultValue, EventHandler propertyChangeCallback = null)
        {
            CheckAndWarn(propName);
            var property = new DynamicStringProperty(propName, defaultValue);
            AddCallback(propertyChangeCallback, property);
            return property;
        }

        /// <summary>
        /// Create a new property whose value is an integer and subject to change on-the-fly.
        /// </summary>
        /// <param name="propName">property name</param>
        /// <param name="defaultValue">default value if the property is not defined in underlying configuration</param>
        /// <param name="propertyChangeCallback">an action to be called when the property is changed</param>
        /// <returns></returns>
        public DynamicIntProperty GetIntProperty(string propName, int defaultValue, EventHandler propertyChangeCallback = null)
        {
            CheckAndWarn(propName);
            var property = new DynamicIntProperty(propName, defaultValue);
            AddCallback(propertyChangeCallback, property);
            return property;
        }

        /// <summary>
        /// Create a new property whose value is a long and subject to change on-the-fly.
        /// </summary>
        /// <param name="propName">property name</param>
        /// <param name="defaultValue">default value if the property is not defined in underlying configuration</param>
        /// <param name="propertyChangeCallback">an action to be called when the property is changed</param>
        /// <returns></returns>
        public DynamicLongProperty GetLongProperty(string propName, long defaultValue, EventHandler propertyChangeCallback = null)
        {
            CheckAndWarn(propName);
            var property = new DynamicLongProperty(propName, defaultValue);
            AddCallback(propertyChangeCallback, property);
            return property;
        }

        /// <summary>
        /// Create a new property whose value is a long and subject to change on-the-fly.
        /// </summary>
        /// <param name="propName">property name</param>
        /// <param name="defaultValue">default value if the property is not defined in underlying configuration</param>
        /// <param name="propertyChangeCallback">an action to be called when the property is changed</param>
        /// <returns></returns>
        public DynamicBooleanProperty GetBooleanProperty(string propName, bool defaultValue, EventHandler propertyChangeCallback)
        {
            CheckAndWarn(propName);
            var property = new DynamicBooleanProperty(propName, defaultValue);
            AddCallback(propertyChangeCallback, property);
            return property;
        }

        /// <summary>
        /// Create a new property whose value is a float and subject to change on-the-fly.
        /// </summary>
        /// <param name="propName">property name</param>
        /// <param name="defaultValue">default value if the property is not defined in underlying configuration</param>
        /// <param name="propertyChangeCallback">an action to be called when the property is changed</param>
        /// <returns></returns>
        public DynamicFloatProperty GetFloatProperty(string propName, float defaultValue, EventHandler propertyChangeCallback = null)
        {
            CheckAndWarn(propName);
            var property = new DynamicFloatProperty(propName, defaultValue);
            AddCallback(propertyChangeCallback, property);
            return property;
        }

        /// <summary>
        /// Create a new property whose value is a double and subject to change on-the-fly.
        /// </summary>
        /// <param name="propName">property name</param>
        /// <param name="defaultValue">default value if the property is not defined in underlying configuration</param>
        /// <param name="propertyChangeCallback">an action to be called when the property is changed</param>
        /// <returns></returns>
        public DynamicDoubleProperty GetDoubleProperty(string propName, double defaultValue, EventHandler propertyChangeCallback = null)
        {
            CheckAndWarn(propName);
            var property = new DynamicDoubleProperty(propName, defaultValue);
            AddCallback(propertyChangeCallback, property);
            return property;
        }

        private static void AddCallback<T>(EventHandler callback, PropertyWrapper<T> wrapper)
        {
            if (callback != null)
            {
                wrapper.PropertyChanged += callback;
            }
        }
    }
}