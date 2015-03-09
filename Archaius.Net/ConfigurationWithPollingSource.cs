using System;
using System.Collections;
using System.Collections.Generic;
using Archaius.Source;

namespace Archaius
{
    /// <summary>
    /// This class delegates property read/write to an another configuration but is also attached with 
    /// a dynamic configuration source and polling scheduler so that its properties can be changed dynamically
    /// at runtime. In other words, if the same property is defined in both the original configuration 
    /// and the dynamic configuration source, the value in the original configuration will be overridden.
    /// 
    /// This class can be served as a decorator to an existing configuration to make the property values 
    /// dynamic.
    /// </summary>
    public class ConfigurationWithPollingSource : IConfiguration
    {
        #region [Private Fields]
        private readonly IConfiguration m_Config;
        private readonly AbstractPollingScheduler m_Scheduler;
        #endregion

        #region [Constructor]
        /// <summary>
        /// Create an instance and start polling the configuration source
        /// </summary>
        /// <param name="config">Configuration to delegate to</param>
        /// <param name="source"><see cref="IPolledConfigurationSource"/> to poll get new/changed properties</param>
        /// <param name="scheduler"><see cref="AbstractPollingScheduler"/> to provide the polling schedule</param>
        public ConfigurationWithPollingSource(IConfiguration config, IPolledConfigurationSource source, AbstractPollingScheduler scheduler)
        {
            m_Config = config;
            m_Scheduler = scheduler;
            scheduler.StartPolling(source, this);
        }
        #endregion

        #region [Event]
        public event EventHandler<ConfigurationEventArgs> ConfigurationChanged;
        #endregion

        #region [Public Properties]
        public IConfiguration Configuration
        {
            get
            {
                return m_Config;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                return m_Config.Keys;
            }
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        public bool Empty
        {
            get
            {
                return m_Config.Empty;
            }
        }

        /// <summary>
        /// Delimiter used to convert single values to lists
        /// </summary>
        public char ListDelimiter
        {
            get
            {
                return m_Config.ListDelimiter;
            }
            set
            {
                m_Config.ListDelimiter = value;
            }
        }

        /// <summary>
        /// When set to true the given configuration delimiter will not be used
        /// while parsing for this configuration.
        /// </summary>
        public bool DelimiterParsingDisabled
        {
            get
            {
                return m_Config.DelimiterParsingDisabled;
            }
            set
            {
                m_Config.DelimiterParsingDisabled = value;
            }
        }
        #endregion

        #region [Public Methods]
        public void StopPolling()
        {
            m_Scheduler.StopPolling();
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        /// <param name="key">The key whose presence in this configuration is to be tested</param>
        /// <returns><code>true</code> if the configuration contains a value for this key, <code>false</code> otherwise</returns>
        public bool ContainsKey(string key)
        {
            return m_Config.ContainsKey(key);
        }

        /// <summary>
        /// Gets a property from the configuration.
        /// This is the most basic get method for retrieving values of properties.
        /// In a typical implementation  of the <see cref="IConfiguration"/> interface the other get methods
        /// (that return specific data types) will internally make use of this method.
        /// On this level variable substitution is not yet performed. The returned
        /// object is an internal representation of the property value for the passed
        /// in key. It is owned by the <see cref="IConfiguration"/> object. So a caller
        /// should not modify this object. It cannot be guaranteed that this object
        /// will stay constant over time (i.e. further update operations on the
        /// configuration may change its internal state).
        /// </summary>
        /// <param name="key">The property to retrieve</param>
        /// <returns>
        /// The value to which this configuration maps the specified key,
        /// or null if the configuration contains no mapping for this key.
        /// </returns>
        public object GetProperty(string key)
        {
            return m_Config.GetProperty(key);
        }

        /// <summary>
        /// Add a property to the configuration.
        /// If it already exists then the value stated here will be added to the configuration entry.
        /// </summary>
        /// <param name="key">The key to add the property to.</param>
        /// <param name="value">The value to add.</param>
        public void AddProperty(string key, object value)
        {
            m_Config.AddProperty(key, value);
        }

        /// <summary>
        /// Remove a property from the configuration.
        /// </summary>
        /// <param name="key">The key to remove along with corresponding value.</param>
        public void ClearProperty(string key)
        {
            m_Config.ClearProperty(key);
        }

        /// <summary>
        /// Set a property, this will replace any previously set values.
        ///  Set values is implicitly a call to ClearProperty(key), AddProperty(key, value).
        /// </summary>
        /// <param name="key">The key of the property to change</param>
        /// <param name="value">The new value</param>
        public void SetProperty(string key, object value)
        {
            m_Config.SetProperty(key, value);
        }

        /// <summary>
        /// Remove all properties from the configuration.
        /// </summary>
        public void Clear()
        {
            m_Config.Clear();
        }

        public bool GetBoolean(string key, bool defaultValue = false)
        {
            return m_Config.GetBoolean(key, defaultValue);
        }

        public byte GetByte(string key, byte defaultValue = 0)
        {
            return m_Config.GetByte(key, defaultValue);
        }

        public short GetShort(string key, short defaultValue = 0)
        {
            return m_Config.GetShort(key, defaultValue);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return m_Config.GetInt(key, defaultValue);
        }

        public long GetLong(string key, long defaultValue = 0)
        {
            return m_Config.GetLong(key, defaultValue);
        }

        public decimal GetDecimal(string key, decimal defaultValue = 0)
        {
            return m_Config.GetDecimal(key, defaultValue);
        }

        public float GetFloat(string key, float defaultValue = 0)
        {
            return m_Config.GetFloat(key, defaultValue);
        }

        public double GetDouble(string key, double defaultValue = 0)
        {
            return m_Config.GetDouble(key, defaultValue);
        }

        public string GetString(string key, string defaultValue = null)
        {
            return m_Config.GetString(key, defaultValue);
        }

        public string[] GetStringArray(string key)
        {
            return m_Config.GetStringArray(key);
        }

        public IList GetList(string key, IList defaultValue = null)
        {
            return m_Config.GetList(key, defaultValue);
        }
        #endregion
    }
}