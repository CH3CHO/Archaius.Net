using System;

namespace Archaius.Dynamic
{
    public class ConfigurationBackedDynamicPropertySupport : IDynamicPropertySupport
    {
        private readonly AbstractConfiguration m_Configuration;

        public ConfigurationBackedDynamicPropertySupport(AbstractConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }
            m_Configuration = configuration;
        }

        public AbstractConfiguration Configuration
        {
            get
            {
                return m_Configuration;
            }
        }

        /// <summary>
        /// Get the string value of a given property. The string value will be further 
        /// cached and parsed into specific type for <see cref="DynamicProperty"/>.
        /// </summary>
        /// <param name="propName">The name of the property</param>
        /// <returns>The string value of the property </returns>
        public string GetString(string propName)
        {
            try
            {
                var values = m_Configuration.GetStringArray(propName);
                if (values == null)
                {
                    return null;
                }
                switch (values.Length)
                {
                    case 0:
                        return m_Configuration.GetString(propName);
                    case 1:
                        return values[0];
                    default:
                        return string.Join(",", values);
                }
            }
            catch (Exception)
            {
                var v = m_Configuration.GetProperty(propName);
                return v != null ? v.ToString() : null;
            }
        }

        public event EventHandler<ConfigurationEventArgs> ConfigurationChanged
        {
            add
            {
                m_Configuration.ConfigurationChanged += value;
            }
            remove
            {
                m_Configuration.ConfigurationChanged -= value;
            }
        }
    }
}