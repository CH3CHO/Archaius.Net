namespace Archaius.Dynamic
{
    /// <summary>
    /// A dynamic property whose value is a boolean.
    /// Use APIs in <see cref="DynamicPropertyFactory"/> to create instance of this class.
    /// </summary>
    public class DynamicBooleanProperty : PropertyWrapper<bool>
    {
        public DynamicBooleanProperty(string propName, bool defaultValue) : base(propName, defaultValue)
        {
        }

        /// <summary>
        /// Gets the latest value for the given property
        /// </summary>
        public override bool Value
        {
            get
            {
                return m_Property.GetBoolean(DefaultValue);
            }
        }
    }
}