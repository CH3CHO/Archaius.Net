using System;
using System.Collections.Generic;

namespace Archaius.Source
{
    /// <summary>
    /// The definition of configuration source that brings dynamic changes to the configuration via watchers.
    /// </summary>
    public interface IWatchedConfigurationSource
    {
        event EventHandler<ConfigurationUpdatedEventArgs> ConfigurationUpdated;

        /// <summary>
        /// Get a snapshot of the latest configuration data.
        /// 
        /// Note: The correctness of this data is only as good as the underlying config source's view of the data.
        /// </summary>
        /// <returns></returns>
        IDictionary<string, object> GetCurrentData();
    }
}