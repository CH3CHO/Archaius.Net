namespace Archaius.Dynamic
{
    /// <summary>
    /// A dynamic property whose value is a float.
    /// Use APIs in <see cref="DynamicPropertyFactory"/> to create instance of this class.
    /// </summary>
    public class DynamicFloatProperty : PropertyWrapper<float>
    {
        public DynamicFloatProperty(string propName, float defaultValue) : base(propName, defaultValue)
        {
        }

        /// <summary>
        /// Gets the latest value for the given property
        /// </summary>
        public override float Value
        {
            get
            {
                return m_Property.GetFloat(DefaultValue);
            }
        }
    }
}