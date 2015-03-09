using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Archaius.Utils;

namespace Archaius
{
    /// <summary>
    ///  This class uses a ConcurrentHashMap for reading/writing a property to achieve high
    /// throughput and thread safety. The implementation is lock free for <see cref="IConfiguration.GetProperty"/>
    /// and <see cref="IConfiguration.AddProperty"/>, but has some synchronization cost for 
    /// <see cref="IConfiguration.AddProperty"/> if the object to add is not a String or the key already exists.
    /// The methods from AbstractConfiguration related to listeners and event generation are overridden
    /// so that adding/deleting listeners and firing events are no longer synchronized.
    /// Also, it catches Throwable when it invokes the listeners, making
    /// it more robust.
    /// This configuration does not allow null as key or value and will throw NullPointerException
    /// when trying to add or set properties with empty key or value.
    /// </summary>
    public class ConcurrentDictionaryConfiguration : AbstractConfiguration
    {
        #region [Constants]
        private const int LockCount = 32;
        #endregion

        #region [Private Fields]
        protected readonly ConcurrentDictionary<string, object> m_Properties = new ConcurrentDictionary<string, object>();

        private readonly object[] m_Locks = new object[LockCount];
        #endregion

        #region [Constructors]
        public ConcurrentDictionaryConfiguration()
        {
            for (var i = 0; i < m_Locks.Length; ++i)
            {
                m_Locks[i] = new object();
            }
        }

        public ConcurrentDictionaryConfiguration(IDictionary<string, object> properties)
            : this()
        {
            m_Properties = new ConcurrentDictionary<string, object>(properties);
        }

        /// <summary>
        /// Create an instance by copying the properties from an existing Configuration.
        /// Future changes to the Configuration passed in will not be reflected in this
        /// object.
        /// </summary>
        /// <param name="config">Configuration to be copied</param>
        public ConcurrentDictionaryConfiguration(IConfiguration config)
            : this()
        {
            foreach (var key in config.Keys)
            {
                m_Properties[key] = config.GetProperty(key);
            }
        }
        #endregion

        #region Overrides of AbstractConfiguration
        public override IEnumerable<string> Keys
        {
            get
            {
                return m_Properties.Keys;
            }
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        public override bool Empty
        {
            get
            {
                return m_Properties.Count == 0;
            }
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        /// <param name="key">The key whose presence in this configuration is to be tested</param>
        /// <returns><code>true</code> if the configuration contains a value for this key, <code>false</code> otherwise</returns>
        public override bool ContainsKey(string key)
        {
            return m_Properties.ContainsKey(key);
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
        public override object GetProperty(string key)
        {
            object value;
            m_Properties.TryGetValue(key, out value);
            return value;
        }

        public override void AddProperty(string key, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.AddProperty, key, value, true));
            DoAddProperty(key, value);
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.AddProperty, key, value, false));
        }

        protected void DoAddProperty(string key, object value)
        {
            if (DelimiterParsingDisabled || ((value is string) && ((string)value).IndexOf(ListDelimiter) < 0))
            {
                if (!m_Properties.TryAdd(key, value))
                {
                    AddPropertyValues(key, value, DelimiterParsingDisabled ? '\0' : ListDelimiter);
                }
            }
            else
            {
                AddPropertyValues(key, value, DelimiterParsingDisabled ? '\0' : ListDelimiter);
            }
        }

        protected override void AddPropertyDirect(string key, object value)
        {
            var lockObject = m_Locks[GetLockIndex(key)];
            lock (lockObject)
            {
                var currentValue = m_Properties.GetOrAdd(key, value);
                if (currentValue == value)
                {
                    return;
                }
                if (currentValue is IList)
                {
                    ((IList)currentValue).Add(value);
                }
                else
                {
                    var list = new ArrayList { currentValue, value };
                    m_Properties[key] = list;
                }
            }
        }

        private int GetLockIndex(string key)
        {
            return Math.Abs(key.GetHashCode()) % m_Locks.Length;
        }

        public override void SetProperty(string key, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.SetProperty, key, value, true));
            DoSetProperty(key, value);
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.SetProperty, key, value, false));
        }

        private void DoSetProperty(string key, object value)
        {
            if (DelimiterParsingDisabled)
            {
                m_Properties[key] = value;
            }
            else if (value is string && ((string)value).IndexOf(ListDelimiter) < 0)
            {
                m_Properties[key] = value;
            }
            else
            {
                var values = PropertyConverter.Flatten(value, ListDelimiter);
                m_Properties[key] = values.Count == 1 ? values.Cast<object>().First() : values;
            }
        }

        protected override void ClearPropertyDirect(string key)
        {
            object value;
            m_Properties.TryRemove(key, out value);
        }

        protected override void ClearDirect()
        {
            m_Properties.Clear();
        }
        #endregion
    }
}