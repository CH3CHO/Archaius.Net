using System;
using System.Collections;
using System.Collections.Generic;

namespace Archaius
{
    /// <summary>
    /// The main Configuration interface.
    /// 
    /// This interface allows accessing and manipulating a configuration object which contains multiple properties.
    /// </summary>
    public interface IConfiguration
    {
        event EventHandler<ConfigurationEventArgs> ConfigurationChanged;

        IEnumerable<string> Keys
        {
            get;
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        bool Empty
        {
            get;
        }

        /// <summary>
        /// Delimiter used to convert single values to lists
        /// </summary>
        char ListDelimiter
        {
            get;
            set;
        }

        /// <summary>
        /// When set to true the given configuration delimiter will not be used
        /// while parsing for this configuration.
        /// </summary>
        bool DelimiterParsingDisabled
        {
            get;
            set;
        }

        /// <summary>
        /// Check if the configuration is empty.
        /// </summary>
        /// <param name="key">The key whose presence in this configuration is to be tested</param>
        /// <returns><code>true</code> if the configuration contains a value for this key, <code>false</code> otherwise</returns>
        bool ContainsKey(string key);

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
        object GetProperty(string key);

        /// <summary>
        /// Add a property to the configuration.
        /// If it already exists then the value stated here will be added to the configuration entry.
        /// </summary>
        /// <param name="key">The key to add the property to.</param>
        /// <param name="value">The value to add.</param>
        void AddProperty(string key, object value);

        /// <summary>
        /// Remove a property from the configuration.
        /// </summary>
        /// <param name="key">The key to remove along with corresponding value.</param>
        void ClearProperty(string key);

        /// <summary>
        /// Set a property, this will replace any previously set values.
        ///  Set values is implicitly a call to ClearProperty(key), AddProperty(key, value).
        /// </summary>
        /// <param name="key">The key of the property to change</param>
        /// <param name="value">The new value</param>
        void SetProperty(string key, object value);

        /// <summary>
        /// Remove all properties from the configuration.
        /// </summary>
        void Clear();

        bool GetBoolean(string key, bool defaultValue = false);

        byte GetByte(string key, byte defaultValue = 0);

        short GetShort(string key, short defaultValue = 0);

        int GetInt(string key, int defaultValue = 0);

        long GetLong(string key, long defaultValue = 0L);

        decimal GetDecimal(string key, decimal defaultValue = 0m);

        float GetFloat(string key, float defaultValue = 0f);

        double GetDouble(string key, double defaultValue = 0.0);

        string GetString(string key, string defaultValue = null);

        string[] GetStringArray(string key);

        IList GetList(string key, IList defaultValue = null);
    }
}