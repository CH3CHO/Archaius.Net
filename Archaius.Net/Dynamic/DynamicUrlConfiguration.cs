using Archaius.Source;

namespace Archaius.Dynamic
{
    /// <summary>
    /// A <see cref="DynamicConfiguration"/> that uses a <see cref="UrlConfigurationSource"/> and <see cref="FixedDelayPollingScheduler"/>
    /// </summary>
    public class DynamicUrlConfiguration : DynamicConfiguration
    {
        /// <summary>
        /// Create an instance with default <see cref="UrlConfigurationSource"/> and <see cref="FixedDelayPollingScheduler"/> and start polling the source
        /// if there is any URLs available for polling.
        /// </summary>
        public DynamicUrlConfiguration()
        {
            var source = new UrlConfigurationSource();
            if (source.ConfigUrls != null && source.ConfigUrls.Count > 0)
            {
                StartPolling(source, new FixedDelayPollingScheduler());
            }
        }

        /// <summary>
        /// Create an instance and start polling the source.
        /// </summary>
        /// <param name="initialDelayMillis">initial delay in milliseconds used by <see cref="FixedDelayPollingScheduler"/></param>
        /// <param name="delayMillis">delay interval in milliseconds used by <see cref="FixedDelayPollingScheduler"/></param>
        /// <param name="ignoreDeletesFromSource">
        /// whether the scheduler should ignore deletes of properties from configuration source when applying the polling result to a configuration.
        /// </param>
        /// <param name="urls">the set of URLs to be polled by <see cref="UrlConfigurationSource"/></param>
        public DynamicUrlConfiguration(int initialDelayMillis, int delayMillis, bool ignoreDeletesFromSource, params string[] urls)
            : base(new UrlConfigurationSource(urls), new FixedDelayPollingScheduler(initialDelayMillis, delayMillis, ignoreDeletesFromSource))
        {
        }
    }
}