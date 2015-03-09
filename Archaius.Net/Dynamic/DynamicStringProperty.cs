namespace Archaius.Dynamic
{
    /// <summary>
    /// A dynamic property whose value is a string.
    /// Use APIs in <see cref="DynamicPropertyFactory"/> to create instance of this class.
    /// </summary>
    public class DynamicStringProperty : PropertyWrapper<string>
    {
        public DynamicStringProperty(string propName, string defaultValue) : base(propName, defaultValue)
        {
        }

        /// <summary>
        /// Gets the latest value for the given property
        /// </summary>
        public override string Value
        {
            get
            {
                return m_Property.GetString(DefaultValue);
            }
        }
    }
}