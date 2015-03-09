namespace Archaius.Dynamic
{
    /// <summary>
    /// A dynamic property whose value is a long.
    /// Use APIs in <see cref="DynamicPropertyFactory"/> to create instance of this class.
    /// </summary>
    public class DynamicLongProperty : PropertyWrapper<long>
    {
        public DynamicLongProperty(string propName, long defaultValue) : base(propName, defaultValue)
        {
        }

        /// <summary>
        /// Gets the latest value for the given property
        /// </summary>
        public override long Value
        {
            get
            {
                return m_Property.GetLong(DefaultValue);
            }
        }
    }
}