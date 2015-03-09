using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Archaius.Utils;

namespace Archaius
{
    /// <summary>
    /// Abstract configuration class. Provides basic functionality but does not store any data.
    /// </summary>
    public abstract class AbstractConfiguration : IConfiguration
    {
        #region [Constants]
        /// <summary>
        /// Constant for the disabled list delimiter. This character is passed to the
        /// list parsing methods if delimiter parsing is disabled. So this character
        /// should not occur in string property values.
        /// </summary>
        public const char DisabledDelimiter = '\0';

        /// <summary>
        /// The default value for listDelimiter
        /// </summary>
        public static char DefaultListDelimiter = ',';
        #endregion

        #region [Private Fields]
        private int m_EventSuspendingLevel;
        private char m_ListDelimiter = DefaultListDelimiter;
        #endregion

        #region [Events]
        public event EventHandler<ConfigurationEventArgs> ConfigurationChanged;

        protected internal EventHandler<ConfigurationEventArgs>[] ConfigurationChangedEventHandlers
        {
            get
            {
                return ConfigurationChanged != null
                           ? ConfigurationChanged.GetInvocationList().OfType<EventHandler<ConfigurationEventArgs>>().ToArray()
                           : new EventHandler<ConfigurationEventArgs>[0];
            }
        }
        #endregion

        #region [Properties]
        /// <summary>
        /// Delimiter used to convert single values to lists
        /// </summary>
        public virtual char ListDelimiter
        {
            get
            {
                return m_ListDelimiter;
            }
            set
            {
                m_ListDelimiter = value;
            }
        }

        /// <summary>
        /// When set to true the given configuration delimiter will not be used
        /// while parsing for this configuration.
        /// </summary>
        public virtual bool DelimiterParsingDisabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a property in the configuration.
        /// </summary>
        /// <param name="key">The key of the property to operate</param>
        /// <returns></returns>
        public object this[string key]
        {
            get
            {
                return GetProperty(key);
            }
            set
            {
                SetProperty(key, value);
            }
        }
        #endregion

        #region [Abstract Properties & Methods]
        public abstract IEnumerable<string> Keys
        {
            get;
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        public abstract bool Empty
        {
            get;
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        /// <param name="key">The key whose presence in this configuration is to be tested</param>
        /// <returns><code>true</code> if the configuration contains a value for this key, <code>false</code> otherwise</returns>
        public abstract bool ContainsKey(string key);

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
        public abstract object GetProperty(string key);

        /// <summary>
        /// Add a property to the configuration.
        /// If it already exists then the value stated here will be added to the configuration entry.
        /// </summary>
        /// <param name="key">The key to add the property to.</param>
        /// <param name="value">The value to add.</param>
        public virtual void AddProperty(string key, object value)
        {
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.AddProperty, key, value, true));
            AddPropertyValues(key, value, DelimiterParsingDisabled ? DisabledDelimiter : ListDelimiter);
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.AddProperty, key, value, false));
        }

        protected void AddPropertyValues(string key, object value, char delimiter)
        {
            var values = PropertyConverter.Flatten(value, delimiter);
            foreach (var elem in values)
            {
                AddPropertyDirect(key, elem);
            }
        }

        protected abstract void AddPropertyDirect(string key, object value);

        /// <summary>
        /// Remove a property from the configuration.
        /// </summary>
        /// <param name="key">The key to remove along with corresponding value.</param>
        public virtual void ClearProperty(string key)
        {
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.ClearProperty, key, true));
            ClearPropertyDirect(key);
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.ClearProperty, key, false));
        }

        protected abstract void ClearPropertyDirect(string key);

        /// <summary>
        /// Set a property, this will replace any previously set values.
        /// Set values is implicitly a call to ClearProperty(key), AddProperty(key, value).
        /// </summary>
        /// <param name="key">The key of the property to change</param>
        /// <param name="value">The new value</param>
        public virtual void SetProperty(string key, object value)
        {
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.SetProperty, key, value, true));
            SuspendEvents();
            try
            {
                ClearProperty(key);
                AddProperty(key, value);
            }
            finally
            {
                ResumeEvents();
            }
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.SetProperty, key, value, false));
        }

        /// <summary>
        /// Remove all properties from the configuration.
        /// </summary>
        public virtual void Clear()
        {
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.Clear, true));
            ClearDirect();
            RaiseConfigurationChangedEvent(new ConfigurationEventArgs(ConfigurationEventType.Clear, false));
        }

        protected abstract void ClearDirect();
        #endregion

        #region [Property Getters]
        public bool GetBoolean(string key, bool defaultValue = false)
        {
            var raw = ResolveContainerStore(key);
            if (raw == null)
            {
                return defaultValue;
            }
            bool value;
            return PropertyConverter.ToBoolean(Interpolate(raw), out value) ? value : defaultValue;
        }

        public byte GetByte(string key, byte defaultValue = 0)
        {
            var raw = ResolveContainerStore(key);
            if (raw == null)
            {
                return defaultValue;
            }
            byte value;
            return PropertyConverter.ToByte(Interpolate(raw), out value) ? value : defaultValue;
        }

        public short GetShort(string key, short defaultValue = 0)
        {
            var raw = ResolveContainerStore(key);
            if (raw == null)
            {
                return defaultValue;
            }
            short value;
            return PropertyConverter.ToShort(Interpolate(raw), out value) ? value : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            var raw = ResolveContainerStore(key);
            if (raw == null)
            {
                return defaultValue;
            }
            int value;
            return PropertyConverter.ToInt(Interpolate(raw), out value) ? value : defaultValue;
        }

        public long GetLong(string key, long defaultValue = 0)
        {
            var raw = ResolveContainerStore(key);
            if (raw == null)
            {
                return defaultValue;
            }
            long value;
            return PropertyConverter.ToLong(Interpolate(raw), out value) ? value : defaultValue;
        }

        public decimal GetDecimal(string key, decimal defaultValue = 0)
        {
            var raw = ResolveContainerStore(key);
            if (raw == null)
            {
                return defaultValue;
            }
            decimal value;
            return PropertyConverter.ToDecimal(Interpolate(raw), out value) ? value : defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0)
        {
            var raw = ResolveContainerStore(key);
            if (raw == null)
            {
                return defaultValue;
            }
            float value;
            return PropertyConverter.ToFloat(Interpolate(raw), out value) ? value : defaultValue;
        }

        public double GetDouble(string key, double defaultValue = 0)
        {
            var raw = ResolveContainerStore(key);
            if (raw == null)
            {
                return defaultValue;
            }
            double value;
            return PropertyConverter.ToDouble(Interpolate(raw), out value) ? value : defaultValue;
        }

        public string GetString(string key, string defaultValue = null)
        {
            var raw = ResolveContainerStore(key);
            return Interpolate(PropertyConverter.ToString(raw) ?? defaultValue);
        }

        public virtual string[] GetStringArray(string key)
        {
            string[] array;
            var raw = GetProperty(key);
            if (raw is IList)
            {
                var rawList = (IList)raw;
                array = new string[rawList.Count];
                for (var i = 0; i < rawList.Count; ++i)
                {
                    array[i] = Interpolate(PropertyConverter.ToString(raw));
                }
            }
            else if (raw is string)
            {
                array = new[] {Interpolate((string)raw)};
            }
            else if (raw != null)
            {
                array = new[] {Interpolate(raw.ToString())};
            }
            else
            {
                array = new string[0];
            }
            return array;
        }

        public virtual IList GetList(string key, IList defaultValue = null)
        {
            var list = defaultValue;
            var raw = GetProperty(key);
            if (raw is string)
            {
                list = new ArrayList(1) {Interpolate(raw)};
            }
            else if (raw is IList)
            {
                var rawList = (IList)raw;
                list = new ArrayList(rawList.Count);
                foreach (var rawItem in rawList)
                {
                    list.Add(Interpolate(rawItem));
                }
            }
            return list;
        }
        #endregion

        #region [Protected Methods]
        /// <summary>
        /// Returns an object from the store described by the key.
        /// If the value is an IList object, replace it with the first object in it.
        /// </summary>
        /// <param name="key">The property key.</param>
        /// <returns>Value, transparently resolving a possible collection dependency.</returns>
        protected object ResolveContainerStore(string key)
        {
            var value = GetProperty(key);
            if (value != null)
            {
                if (value is IList)
                {
                    var collection = (IList)value;
                    value = collection.Count == 0 ? null : collection[0];
                }
            }
            return value;
        }

        protected string Interpolate(string value)
        {
            var result = Interpolate((object)value);
            return result != null ? result.ToString() : null;
        }

        protected object Interpolate(object value)
        {
            // TODO: Support interpolate
            return value;
        }

        protected void SuspendEvents()
        {
            Interlocked.Increment(ref m_EventSuspendingLevel);
        }

        protected void ResumeEvents()
        {
            Interlocked.Decrement(ref m_EventSuspendingLevel);
        }

        protected void RaiseConfigurationChangedEvent(ConfigurationEventArgs args)
        {
            var configurationChanged = ConfigurationChanged;
            if (configurationChanged == null)
            {
                return;
            }
            if (m_EventSuspendingLevel != 0)
            {
                return;
            }
            configurationChanged(this, args);
        }
        #endregion
    }
}