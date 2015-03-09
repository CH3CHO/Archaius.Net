namespace Archaius.Dynamic
{
    /// <summary>
    /// A dynamic property whose value is an integer.
    /// Use APIs in <see cref="DynamicPropertyFactory"/> to create instance of this class.
    /// </summary>
    public class DynamicIntProperty : PropertyWrapper<int>
    {
        public DynamicIntProperty(string propName, int defaultValue) : base(propName, defaultValue)
        {
        }

        /// <summary>
        /// Gets the latest value for the given property
        /// </summary>
        public override int Value
        {
            get
            {
                return m_Property.GetInteger(DefaultValue);
            }
        }
    }
}