using System;
using System.Reflection;
using Archaius.Source;
using log4net;

namespace Archaius.Dynamic
{
    /// <summary>
    /// A configuration that waits for a watcher event from the specified config source.
    /// 
    /// The property values in this configuration will be changed dynamically at runtime if the value changes in the
    /// underlying configuration source.
    /// 
    /// This configuration does not allow null as key or value and will throw NullPointerException when trying to add or set
    /// properties with empty key or value.
    /// </summary>
    public class DynamicWatchedConfiguration : ConcurrentDictionaryConfiguration
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IWatchedConfigurationSource m_Source;
        private readonly DynamicPropertyUpdater m_Updater;

        /// <summary>
        ///  Simplified constructor with the following defaults:
        ///  ignoreDeletesFromSource = false
        ///  dynamicPropertyUpdater = new <see cref="DynamicPropertyUpdater"/>
        /// </summary>
        /// <param name="source"></param>
        public DynamicWatchedConfiguration(IWatchedConfigurationSource source)
            : this(source, false, new DynamicPropertyUpdater())
        {
        }

        /// <summary>
        /// Create an instance of the WatchedConfigurationSource, add listeners, and wait for the update callbacks.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ignoreDeletesFromSource"></param>
        /// <param name="updater"></param>
        public DynamicWatchedConfiguration(IWatchedConfigurationSource source, bool ignoreDeletesFromSource,
            DynamicPropertyUpdater updater)
        {
            m_Source = source;
            IgnoreDeletesFromSource = ignoreDeletesFromSource;
            m_Updater = updater;

            // Get a current snapshot of the config source data
            try
            {
                var currentData = source.GetCurrentData();
                var result = WatchedUpdateResult.CreateFull(currentData);
                UpdateConfiguration(result);
            }
            catch (Exception ex)
            {
                m_Log.Error("Could not GetCurrentData() from the WatchedConfigurationSource", ex);
            }

            // Add a listener for subsequent config updates
            m_Source.ConfigurationUpdated += Source_ConfigurationUpdated;
        }

        /// <summary>
        /// Gets or sets the flag indicating if the this configuration will ignore deletes from source.
        /// </summary>
        public bool IgnoreDeletesFromSource
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the underlying <see cref="IWatchedConfigurationSource"/>.
        /// </summary>
        public IWatchedConfigurationSource Source
        {
            get
            {
                return m_Source;
            }
        }

        private void Source_ConfigurationUpdated(object sender, ConfigurationUpdatedEventArgs e)
        {
            UpdateConfiguration(e.Result);
        }

        public void UpdateConfiguration(WatchedUpdateResult result)
        {
            m_Updater.UpdateProperties(result, this, IgnoreDeletesFromSource);
        }
    }
}