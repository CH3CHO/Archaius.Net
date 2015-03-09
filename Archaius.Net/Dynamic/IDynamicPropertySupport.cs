using System;

namespace Archaius.Dynamic
{
    /// <summary>
    /// The interface that defines the contract between DynamicProperty and its
    /// underlying support system.
    /// </summary>
    public interface IDynamicPropertySupport
    { 
        /// <summary>
        /// Get the string value of a given property. The string value will be further 
        /// cached and parsed into specific type for <see cref="DynamicProperty"/>.
        /// </summary>
        /// <param name="propName">The name of the property</param>
        /// <returns>The string value of the property </returns>
        string GetString(string propName);

        /// <summary>
        /// Add the property change listener. This is necessary for the <see cref="DynamicProperty"/> to
        /// receive callback once a property is updated in the underlying <see cref="IDynamicPropertySupport"/>
        /// </summary>
        event EventHandler<ConfigurationEventArgs> ConfigurationChanged;
    }
}