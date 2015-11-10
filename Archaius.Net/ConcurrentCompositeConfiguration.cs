using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Archaius
{
    /// <summary>
    /// This class maintains a hierarchy of configurations in a list structure. The order of the list stands for the descending
    /// priority of the configurations when a property value is to be determined.
    /// For example, if you add Configuration1, and then Configuration2,
    /// <see cref="GetProperty"/> will return any properties defined by Configuration1.
    /// Only if Configuration1 doesn't have the property, then
    /// Configuration2 will be checked. 
    /// There are two internal configurations for properties that are programmatically set:
    /// <ul>
    /// <li>Configuration to hold any property introduced by <see cref="AddProperty"/> or <see cref="SetProperty"/>
    /// called directly on this class. This configuration will be called "container configuration" as it serves as the container of
    /// such properties. By default, this configuration remains at the last of the configurations list. It can be treated as 
    /// a "base line" configuration that holds hard-coded parameters that can be overridden by any of other configurations added at runtime. 
    /// You can replace this configuration by your own and change the position of the configuration in the list by calling
    /// <see cref="SetContainerConfiguration"/>.</li>
    /// <li>Configuration to hold properties that are programmatically set (using <see cref="SetOverrideProperty"/>) to override values from any other 
    /// configurations on the list. As contrast to container configuration, this configuration is always consulted first in 
    /// <see cref="GetProperty"/>. </li>
    /// </ul>
    /// 
    /// When adding configuration to this class, it is recommended to convert it into
    /// <see cref="ConcurrentDictionaryConfiguration"/> or  <see cref="ConcurrentDictionaryConfiguration"/>
    /// to achieve maximal performance and thread safety.
    ///
    /// Example:
    /// <pre>
    /// // Configuration from environment variables
    /// EnvironmentConfiguration environmentConfiguration = new EnvironmentConfiguration(;
    /// // Configuration from app settings
    /// string fileName = "...";
    /// AppSettingsConfiguration appSettingsConfiguration = new AppSettingsConfiguration();
    /// // configuration from a dynamic source
    /// PolledConfigurationSource source = CreateMyOwnSource();
    /// AbstractPollingScheduler scheduler = CreateMyOwnScheduler();
    /// DynamicConfiguration dynamicConfiguration = new DynamicConfiguration(source, scheduler);
    /// 
    /// // Create a hierarchy of configuration that makes
    /// // 1) Dynamic configuration source override system properties and,
    /// // 2) AppSettings override environment variables
    /// ConcurrentCompositeConfiguration finalConfig = new ConcurrentCompositeConfiguration();
    /// finalConfig.add(dynamicConfiguration, "dynamicConfig");
    /// finalConfig.add(appSettingsConfiguration, "appSettingsConfig");
    /// finalConfig.add(environmentConfiguration, "envConfig");
    ///
    /// // Register with DynamicPropertyFactory so that finalConfig becomes the source of dynamic properties
    /// DynamicPropertyFactory.initWithConfigurationSource(finalConfig);
    /// </pre>
    /// </summary>
    public class ConcurrentCompositeConfiguration : ConcurrentDictionaryConfiguration
    {
        internal const ConfigurationEventType ConfigurationSourceChanged = (ConfigurationEventType)1001;

        private readonly IDictionary<string, IConfiguration> m_NamedConfigurations = new ConcurrentDictionary<string, IConfiguration>();

        private readonly List<IConfiguration> m_Configurations = new List<IConfiguration>();
        private AbstractConfiguration m_OverrideProperties;

        /// <summary>
        /// Configuration that holds properties set directly with <see cref="IConfiguration.SetProperty"/>.
        /// </summary>
        private AbstractConfiguration m_ContainerConfiguration;

        /// <summary>
        /// Stores a flag whether the current in-memory configuration is also a child configuration.
        /// </summary>
        private volatile bool m_ContainerConfigurationChanged = true;

        /// <summary>
        /// Creates an empty CompositeConfiguration object which can then be added some other Configuration files
        /// </summary>
        public ConcurrentCompositeConfiguration()
        {
            Clear();
        }

        /// <summary>
        /// Creates a ConcurrentCompositeConfiguration object with a specified <em>container  configuration</em>.
        /// This configuration will store any changes made by <see cref="IConfiguration.SetProperty"/>
        /// and <see cref="IConfiguration.AddProperty"/>
        /// </summary>
        /// <param name="containerConfiguration"></param>
        public ConcurrentCompositeConfiguration(AbstractConfiguration containerConfiguration)
        {
            m_Configurations.Clear();
            m_ContainerConfiguration = containerConfiguration;
        }

        /// <summary>
        /// Creates a ConcurrentCompositeConfiguration with a specified <em>container configuration</em>,
        /// and then adds the given collection of configurations.
        /// </summary>
        /// <param name="containerConfiguration">container configuration to use</param>
        /// <param name="configurations">the collection of configurations to add</param>
        public ConcurrentCompositeConfiguration(AbstractConfiguration containerConfiguration, IEnumerable<IConfiguration> configurations)
            : this(containerConfiguration)
        {
            if (configurations != null)
            {
                foreach (var configuration in configurations)
                {
                    AddConfiguration(configuration);
                }
            }
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        public override bool Empty
        {
            get
            {
                return m_OverrideProperties.Empty && m_Configurations.All(configuration => configuration.Empty);
            }
        }

        public override IEnumerable<string> Keys
        {
            get
            {
                var keys = new HashSet<string>();
                foreach (var key in m_OverrideProperties.Keys.Concat(m_Configurations.SelectMany(c => c.Keys)))
                {
                    keys.Add(key);
                }
                return keys;
            }
        }

        /// <summary>
        /// Gets the configurations added.
        /// </summary>
        /// <returns></returns>
        public IList<IConfiguration> ConfigurationList
        {
            get
            {
                return m_Configurations.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the number of configurations.
        /// </summary>
        public int NumberOfConfigurations
        {
            get
            {
                return m_Configurations.Count;
            }
        }

        /// <summary>
        /// Gets the <em>container configuration</em> In this configuration changes are stored.
        /// </summary>
        public IConfiguration ContainerConfiguration
        {
            get
            {
                return m_ContainerConfiguration;
            }
        }

        /// <summary>
        /// Gets the <em>container configuration</em> In this configuration changes are stored.
        /// </summary>
        public int ContainerConfigurationIndex
        {
            get
            {
                return m_Configurations.IndexOf(m_ContainerConfiguration);
            }
        }

        /// <summary>
        /// Delimiter used to convert single values to lists
        /// </summary>
        public override char ListDelimiter
        {
            get
            {
                return base.ListDelimiter;
            }
            set
            {
                base.ListDelimiter = value;
                m_ContainerConfiguration.ListDelimiter = value;
                m_OverrideProperties.ListDelimiter = value;
            }
        }

        /// <summary>
        /// When set to true the given configuration delimiter will not be used
        /// while parsing for this configuration.
        /// </summary>
        public override bool DelimiterParsingDisabled
        {
            get
            {
                return base.DelimiterParsingDisabled;
            }
            set
            {
                base.DelimiterParsingDisabled = value;
                m_ContainerConfiguration.DelimiterParsingDisabled = value;
                m_OverrideProperties.DelimiterParsingDisabled = value;
            }
        }

        public IList<string> GetConfigurationNameList()
        {
            var names = new List<string>(m_Configurations.Count);
            foreach (var configuration in m_Configurations)
            {
                var namedPair = m_NamedConfigurations.FirstOrDefault(p => p.Value == configuration);
                names.Add(namedPair.Key);
            }
            return names;
        }

        /// <summary>
        /// Return the configuration at the specified index.
        /// </summary>
        /// <param name="index">The index of the configuration to retrieve</param>
        /// <returns>The configuration at this index</returns>
        public IConfiguration GetConfiguration(int index)
        {
            return m_Configurations[index];
        }

        /// <summary>
        /// Returns the configuration with the given name. This can be <b>null</b> if no such configuration exists.
        /// </summary>
        /// <param name="name">The name of the configuration</param>
        /// <returns>The configuration with this name</returns>
        public IConfiguration GetConfiguration(string name)
        {
            IConfiguration configuration;
            m_NamedConfigurations.TryGetValue(name, out configuration);
            return configuration;
        }

        /// <summary>
        /// Adds a new child configuration to this configuration with an optional name.
        /// The configuration will be added to the end of the list if <em>container configuration</em> has been changed to new one
        /// or no longer at the end of the list. Otherwise it will be added in front of the <em>container configuration</em>.
        /// </summary>
        /// <param name="config">the configuration to add (must not be <b>null</b>)</param>
        /// <param name="name">the name of this configuration (can be <b>null</b>)</param>
        public void AddConfiguration(IConfiguration config, string name = null)
        {
            var targetIndex = m_ContainerConfigurationChanged ? m_Configurations.Count : m_Configurations.IndexOf(m_ContainerConfiguration);
            AddConfigurationAtIndex(config, name, targetIndex);
        }

        public void AddConfigurationAtFront(IConfiguration config, String name)
        {
            AddConfigurationAtIndex(config, name, 0);
        }

        /// <summary>
        /// Add a configuration with a name at a particular index.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="name"></param>
        /// <param name="index"></param>
        public void AddConfigurationAtIndex(IConfiguration config, string name, int index)
        {
            if (m_Configurations.Contains(config))
            {
                return;
            }
            CheckIndex(index);
            m_Configurations.Insert(index, config);
            if (name != null)
            {
                m_NamedConfigurations[name] = config;
            }
            RegisterEventHandlers(config);
            RaiseOnEventSourceChangedEvent();
        }

        public int GetIndexOfConfiguration(IConfiguration config)
        {
            return m_Configurations.IndexOf(config);
        }

        public int GetIndexOfContainerConfiguration()
        {
            return m_Configurations.IndexOf(m_ContainerConfiguration);
        }

        public void SetContainerConfiguration(AbstractConfiguration configuration, string name, int index)
        {
            if (m_Configurations.Contains(configuration))
            {
                return;
            }
            if (m_ContainerConfiguration == configuration)
            {
                SetContainerConfigurationIndex(index);
                var currentContainerName = GetNameForConfiguration(m_ContainerConfiguration);
                if (currentContainerName != name)
                {
                    m_NamedConfigurations.Remove(currentContainerName);
                    m_NamedConfigurations[name] = configuration;
                }
            }
            else
            {
                m_ContainerConfigurationChanged = true;
                UnregisterEventHandlers(m_ContainerConfiguration);
                m_ContainerConfiguration = configuration;
                m_Configurations.Remove(m_ContainerConfiguration);
                AddConfigurationAtIndex(configuration, name, index);
            }
        }

        public void SetContainerConfigurationIndex(int newIndex)
        {
            if (newIndex < 0 || newIndex >= m_Configurations.Count)
            {
                throw new IndexOutOfRangeException("Cannot change to the new index " + newIndex + " in the list of size " + m_Configurations.Count);
            }
            if (newIndex == ContainerConfigurationIndex)
            {
                return;
            }
            m_ContainerConfigurationChanged = true;
            m_Configurations.Remove(m_ContainerConfiguration);
            m_Configurations.Insert(newIndex, m_ContainerConfiguration);
        }

        public override sealed void Clear()
        {
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.Clear, true));
            foreach (var configuration in m_Configurations)
            {
                UnregisterEventHandlers(configuration);
            }
            m_Configurations.Clear();
            m_NamedConfigurations.Clear();

            // recreate the in memory configuration
            UnregisterEventHandlers(m_ContainerConfiguration);
            m_ContainerConfiguration = new ConcurrentDictionaryConfiguration();
            m_ContainerConfiguration.ListDelimiter = ListDelimiter;
            m_ContainerConfiguration.DelimiterParsingDisabled = DelimiterParsingDisabled;
            RegisterEventHandlers(m_ContainerConfiguration);
            m_Configurations.Add(m_ContainerConfiguration);

            UnregisterEventHandlers(m_OverrideProperties);
            m_OverrideProperties = new ConcurrentDictionaryConfiguration();
            m_OverrideProperties.ListDelimiter = ListDelimiter;
            m_OverrideProperties.DelimiterParsingDisabled = DelimiterParsingDisabled;
            RegisterEventHandlers(m_OverrideProperties);

            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.Clear, false));
            m_ContainerConfigurationChanged = false;
            Invalidate();
        }

        /// <summary>
        /// Remove a configuration. The container configuration cannot be removed.
        /// </summary>
        /// <param name="config">The configuration to remove</param>
        /// <returns></returns>
        public bool RemoveConfiguration(IConfiguration config)
        {
            // Make sure that you can't remove the inMemoryConfiguration from
            // the CompositeConfiguration object
            if (config == m_ContainerConfiguration)
            {
                throw new InvalidOperationException("Can't remove container configuration");
            }

            var configName = GetNameForConfiguration(config);
            if (configName != null)
            {
                m_NamedConfigurations.Remove(configName);
            }
            var ret = m_Configurations.Remove(config);
            UnregisterEventHandlers(config);
            RaiseOnEventSourceChangedEvent();
            return ret;
        }

        public IConfiguration RemoveConfigurationAt(int index)
        {
            var config = m_Configurations[index];
            m_Configurations.RemoveAt(index);
            var nameFound = GetNameForConfiguration(config);
            if (nameFound != null)
            {
                m_NamedConfigurations.Remove(nameFound);
            }
            UnregisterEventHandlers(config);
            RaiseOnEventSourceChangedEvent();
            return config;
        }

        /// <summary>
        /// Removes the configuration with the specified name.
        /// </summary>
        /// <param name="name">The name of the configuration to be removed</param>
        /// <returns>
        /// The removed configuration (<b>null</b> if this configuration
        //  was not found)
        /// </returns>
        public IConfiguration RemoveConfiguration(String name)
        {
            var config = GetConfiguration(name);
            if (config == null)
            {
                return null;
            }
            if (config == m_ContainerConfiguration)
            {
                throw new InvalidOperationException("Can't remove container configuration");
            }
            m_Configurations.Remove(config);
            m_NamedConfigurations.Remove(name);
            UnregisterEventHandlers(config);
            RaiseOnEventSourceChangedEvent();
            return config;
        }

        /// <summary>
        /// Returns the configuration source, in which the specified key is defined.
        /// This method will iterate over all existing child configurations and check
        /// whether they contain the specified key. The following constellations are
        /// possible:
        /// <ul>
        /// <li>If the child configurations contains this key, the first one is returned.</li>
        /// <li>If none of the child configurations contain the key, <b>null</b> is   returned.</li>
        /// </ul>
        /// </summary>
        /// <param name="key">The key to be checked</param>
        /// <returns>The source configuration of this key</returns>
        public IConfiguration GetSource(string key)
        {
            return GetSource(key, null);
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        /// <param name="key">The key whose presence in this configuration is to be tested</param>
        /// <returns><code>true</code> if the configuration contains a value for this key, <code>false</code> otherwise</returns>
        public override bool ContainsKey(string key)
        {
            return m_OverrideProperties.ContainsKey(key) || m_Configurations.Any(c => c.ContainsKey(key));
        }

        public override string[] GetStringArray(string key)
        {
            var list = GetList(key);
            if (list == null)
            {
                return new string[0];
            }
            var tokens = list.Cast<object>().Select(t => t.ToString()).ToArray();
            return tokens;
        }

        public override IList GetList(string key, IList defaultValue = null)
        {
            var list = new ArrayList();

            // Add all elements from the first configuration containing the requested key
            if (m_OverrideProperties.ContainsKey(key))
            {
                AppendListProperty(list, m_OverrideProperties, key);
            }
            foreach (var configuration in m_Configurations)
            {
                if ((configuration == m_ContainerConfiguration && !m_ContainerConfigurationChanged) || !configuration.ContainsKey(key))
                {
                    continue;
                }
                AppendListProperty(list, configuration, key);
                if (list.Count != 0)
                {
                    break;
                }
            }

            // Add all elements from the in memory configuration
            if (list.Count == 0)
            {
                AppendListProperty(list, m_ContainerConfiguration, key);
            }

            if (list.Count == 0)
            {
                return defaultValue;
            }

            for (int i = 0; i < list.Count; ++i)
            {
                list[i] = Interpolate(list[i]);
            }

            return list;
        }

        /// <summary>
        ///  Add the property with the <em>container configuration</em>. 
        /// <b>Warning: </b>{@link #getProperty(String)} on this key may not return the same value set by this method
        /// if there is any other configuration that contain the same property and is in front of the 
        /// <em>container configuration</em> in the configurations list.
        /// </summary>
        /// <param name="key">The key to add the property to.</param>
        /// <param name="value">The value to add.</param>
        public override void AddProperty(string key, object value)
        {
            m_ContainerConfiguration.AddProperty(key, value);
        }

        /// <summary>
        /// Read property from underlying composite. It first checks if the property has been overridden
        /// by <see cref="SetOverrideProperty"/> and if so return the overriding value.
        /// Otherwise, it iterates through the list of sub configurations until it finds one that contains the
        /// property and return the value from that sub configuration. It returns null of the property does
        /// not exist.
        /// </summary>
        /// <param name="key">The property to retrieve</param>
        /// <returns>
        /// The value to which this configuration maps the specified key,
        /// or null if the configuration contains no mapping for this key.
        /// </returns>
        public override object GetProperty(string key)
        {
            var source = GetSource(key);
            return source != null ? source.GetProperty(key) : null;
        }

        /// <summary>
        ///  Set the property with the <em>container configuration</em>. 
        /// <b>Warning: </b>{@link #getProperty(String)} on this key may not return the same value set by this method
        /// if there is any other configuration that contain the same property and is in front of the 
        /// <em>container configuration</em> in the configurations list.
        /// </summary>
        /// <param name="key">The key of the property to change</param><param name="value">The new value</param>
        public override void SetProperty(string key, object value)
        {
            m_ContainerConfiguration.SetProperty(key, value);
        }

        /// <summary>
        /// Clear the property with the <em>container configuration</em>. 
        /// <b>Warning: </b>{@link #getProperty(String)} on this key may still return some value 
        /// if there is any other configuration that contain the same property and is in front of the 
        /// <em>container configuration</em> in the configurations list.
        /// </summary>
        /// <param name="key">The key to remove along with corresponding value.</param>
        public override void ClearProperty(string key)
        {
            m_ContainerConfiguration.ClearProperty(key);
        }

        /// <summary>
        /// Override the same property in any other configurations in the list.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="finalValue"></param>
        public void SetOverrideProperty(string key, object finalValue)
        {
            m_OverrideProperties.SetProperty(key, finalValue);
        }

        /// <summary>
        /// Remove the overriding property set by <see cref="SetOverrideProperty"/>
        /// </summary>
        /// <param name="key"></param>
        public void clearOverrideProperty(string key)
        {
            m_OverrideProperties.ClearProperty(key);
        }

        /// <summary>
        /// Adds the value of a property to the given list.
        /// This method is used by <see cref="GetList"/> for gathering property values from the child configurations.
        /// </summary>
        /// <param name="dest">The list for collecting the data</param>
        /// <param name="config">The configuration to query</param>
        /// <param name="key">The key of the property</param>
        private static void AppendListProperty(IList dest, IConfiguration config, string key)
        {
            var value = config.GetProperty(key);
            if (value == null)
            {
                return;
            }
            if (value is IList)
            {
                foreach (var item in (IList)value)
                {
                    dest.Add(item);
                }
            }
            else
            {
                dest.Add(value);
            }
        }

        private IConfiguration GetSource(string key, IList<IConfiguration> skippedConfigurations)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (m_OverrideProperties.ContainsKey(key) && (skippedConfigurations == null || !skippedConfigurations.Contains(m_OverrideProperties)))
            {
                return m_OverrideProperties;
            }

            var configuration =
                m_Configurations.FirstOrDefault(c => c.ContainsKey(key) && (skippedConfigurations == null || !skippedConfigurations.Contains(c)));
            return configuration;
        }

        public virtual void Invalidate()
        {
        }

        private string GetNameForConfiguration(IConfiguration config)
        {
            return m_NamedConfigurations.FirstOrDefault(p => p.Value == config).Key;
        }

        private void RegisterEventHandlers(IConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }
            configuration.ConfigurationChanged += OnConfigurationChanged;
        }

        private void UnregisterEventHandlers(IConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }
            configuration.ConfigurationChanged -= OnConfigurationChanged;
        }

        private void OnConfigurationChanged(object sender, ConfigurationEventArgs e)
        {
            var eventSource = (IConfiguration)sender;
            switch (e.Type)
            {
                case ConfigurationSourceChanged:
                case ConfigurationEventType.Clear:
                    RaiseConfigurationChangedEvent(e);
                    break;
                case ConfigurationEventType.AddProperty:
                case ConfigurationEventType.SetProperty:
                    if (e.BeforeOperation)
                    {
                        RaiseConfigurationChangedEvent(e);
                    }
                    else
                    {
                        var propertySource = GetSource(e.Name);
                        if (propertySource == null || propertySource == eventSource)
                        {
                            RaiseConfigurationChangedEvent(e);
                        }
                    }
                    break;
                case ConfigurationEventType.ClearProperty:
                {
                    var propertySource = GetSource(e.Name);
                    var finalValue = GetProperty(e.Name);
                    if (propertySource == eventSource)
                    {
                        RaiseConfigurationChangedEvent(e);
                    }
                    else
                    {
                        RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.SetProperty, e.Name, finalValue, e.BeforeOperation));
                    }
                    break;
                }
            }
        }

        private void CheckIndex(int newIndex)
        {
            if (newIndex < 0 || newIndex > m_Configurations.Count)
            {
                throw new IndexOutOfRangeException(newIndex + " is out of bounds of the size of configuration list " + m_Configurations.Count);
            }
        }

        private void RaiseOnEventSourceChangedEvent()
        {
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationSourceChanged, false));
        }
    }
}