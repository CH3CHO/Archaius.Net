using System;
using System.Collections;
using System.Collections.Generic;
using Archaius.Utils;

namespace Archaius
{
    /// <summary>
    /// <p>
    /// A dictionary based Configuration.
    /// </p>
    /// <p>
    /// This implementation of the <see cref="IConfiguration"/> interface is
    /// initialized with a <see cref="IDictionary{TKey,TValue}"/>. The methods of the
    /// <see cref="IConfiguration"/> interface are implemented on top of the content of
    /// this dictionary. The following storage scheme is used:
    /// </p>
    /// <p>
    /// Property keys are directly mapped to map keys, i.e. the
    /// <see cref="GetProperty"/> method directly performs a <see cref="IDictionary{TKey, TValue}.Item"/> on
    /// the map. Analogously, <see cref="IConfiguration.SetProperty"/> or
    /// <see cref="IConfiguration.AddProperty"/> operations write new data into the dictionary.
    /// If a value is added to an existing property, an <see cref="ArrayList"/> is created,
    /// which stores the values of this property.
    /// </p>
    /// <p>
    /// An important use case of this class is to treat a dictionary as a
    /// <see cref="IConfiguration"/> allowing access to its data through the richer
    /// interface. This can be a bit problematic in some cases because the dictionary may
    /// contain values that need not adhere to the default storage scheme used by
    /// typical configuration implementations, e.g. regarding lists. In such cases
    /// care must be taken when manipulating the data through the
    /// <see cref="IConfiguration"/> interface, e.g. by calling
    /// <see cref="IConfiguration.AddProperty"/>; results may be different than expected.
    /// </p>
    /// <p>
    /// An important point is the handling of list delimiters: If delimiter parsing
    /// is enabled (which it is per default), <see cref="GetProperty"/> checks
    /// whether the value of a property is a string and whether it contains the list
    /// delimiter character. If this is the case, the value is split at the delimiter
    /// resulting in a list. This split operation typically also involves trimming
    /// the single values as the list delimiter character may be surrounded by
    /// whitespace. Trimming can be disabled with the
    /// <see cref="TrimmingDisabled"/> property. The whole list splitting
    /// behavior can be disabled using the <see cref="AbstractConfiguration.DelimiterParsingDisabled"/> propertry.
    /// </p>
    /// <p>
    /// Notice that list splitting is only performed for single string values. If a
    /// property has multiple values, the single values are not split even if they
    /// contain the list delimiter character.
    /// </p>
    /// <p>
    /// As the underlying <see cref="IDictionary{TKey, TValue}"/> is directly used as store of the property
    /// values, the thread-safety of this <see cref="IConfiguration"/> implementation
    /// depends on the map passed to the constructor.
    /// </p>
    /// <p>
    /// Notes about type safety: For properties with multiple values this implementation
    /// creates lists of type <see cref="object"/> and stores them. If a property is assigned
    /// another value, the value is added to the list. This can cause problems if the
    /// map passed to the constructor already contains lists of other types. This
    /// should be avoided, otherwise it cannot be guaranteed that the application
    /// might throw <see cref="InvalidCastException"/> exceptions later.
    /// </p>
    /// </summary>
    public class DictionaryConfiguration : AbstractConfiguration
    {
        #region [Private Fields]
        /// <summary>
        /// The dictionary decorated by this configuration.
        /// </summary>
        private readonly IDictionary<string, object> m_Properties;
        #endregion

        #region [Constructor]
        /// <summary>
        /// Create a DictionaryConfiguration with an embedded dictionary.
        /// </summary>
        public DictionaryConfiguration()
            : this(new Dictionary<string, object>())
        {
        }

        /// <summary>
        /// Create a Configuration decorator around the specified Map. The map is
        /// used to store the configuration properties, any change will also affect
        /// the dictionary.
        /// </summary>
        /// <param name="properties"></param>
        public DictionaryConfiguration(IDictionary<string, object> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }
            m_Properties = properties;
        }
        #endregion

        #region [Properties]
        /// <summary>
        /// Gets or sets the flag whether trimming of property values is disabled.
        /// </summary>
        public bool TrimmingDisabled
        {
            get;
            set;
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
            if (value is string && !DelimiterParsingDisabled)
            {
                var list = PropertyConverter.Split((string)value, ListDelimiter, TrimmingDisabled);
                return list.Count > 1 ? (object)list : list[0];
            }
            return value;
        }

        protected override void AddPropertyDirect(string key, object value)
        {
            var previousValue = GetProperty(key);

            if (previousValue == null)
            {
                m_Properties.Add(key, value);
            }
            else if (previousValue is IList)
            {
                // The value is added to the existing list
                // Note: This is problematic. See header comment!
                ((IList)previousValue).Add(value);
            }
            else
            {
                // The previous value is replaced by a list containing the previous value and the new value
                var list = new ArrayList {previousValue, value};
                m_Properties[key] = list;
            }
        }

        protected override void ClearPropertyDirect(string key)
        {
            m_Properties.Remove(key);
        }

        protected override void ClearDirect()
        {
            m_Properties.Clear();
        }
        #endregion
    }
}