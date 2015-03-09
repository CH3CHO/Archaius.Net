namespace Archaius.Dynamic
{
    /// <summary>
    /// A dynamic property whose value is a double.
    /// <p>Use APIs in <see cref="DynamicPropertyFactory"/> to create instance of this class.
    /// </summary>
    public class DynamicDoubleProperty : PropertyWrapper<double>
    {
        public DynamicDoubleProperty(string propName, double defaultValue) : base(propName, defaultValue)
        {
        }

        /// <summary>
        /// Gets the latest value for the given property
        /// </summary>
        public override double Value
        {
            get
            {
                return m_Property.GetDouble(DefaultValue);
            }
        }
    }
}