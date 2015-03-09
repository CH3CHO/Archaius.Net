using Archaius.Source;

namespace Archaius.Dynamic
{
    /// <summary>
    ///  A configuration that polls a <see cref="IPolledConfigurationSource"/> according to the schedule set by a
    /// scheduler. The property values in this configuration will be changed dynamically at runtime if the
    /// value changes in the configuration source.
    /// This configuration does not allow null as key or value and will throw NullPointerException
    /// when trying to add or set properties with empty key or value.
    /// </summary>
    public class DynamicConfiguration : ConcurrentDictionaryConfiguration
    {
        private AbstractPollingScheduler m_Scheduler;
        private IPolledConfigurationSource m_Source;
        private readonly object m_ObjectLock = new object();

        /// <summary>
        /// Create an instance and start polling the configuration source.
        /// </summary>
        /// <param name="source">PolledConfigurationSource to poll</param>
        /// <param name="scheduler">
        /// AbstractPollingScheduler whose <see cref="AbstractPollingScheduler.SchedulePollingAction"/> will be used to determine the polling schedule
        /// </param>
        public DynamicConfiguration(IPolledConfigurationSource source, AbstractPollingScheduler scheduler) : this()
        {
            StartPolling(source, scheduler);
        }

        public DynamicConfiguration()
        {
        }

        public IPolledConfigurationSource Source
        {
            get
            {
                return m_Source;
            }
        }

        /// <summary>
        /// Start polling the configuration source with the specified scheduler.
        /// </summary>
        /// <param name="source">PolledConfigurationSource to poll</param>
        /// <param name="scheduler">
        /// AbstractPollingScheduler whose <see cref="AbstractPollingScheduler.SchedulePollingAction"/> will be used to determine the polling schedule
        /// </param>
        public void StartPolling(IPolledConfigurationSource source, AbstractPollingScheduler scheduler)
        {
            lock (m_ObjectLock)
            {
                m_Scheduler = scheduler;
                m_Source = source;
                Init(source, scheduler);
                scheduler.StartPolling(source, this);
            }
        }

        /// <summary>
        /// Initialize the configuration. This method is called in <see cref="DynamicConfiguration"/> and <see cref="StartPolling"/>
        /// before the initial polling. The default implementation does nothing.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="scheduler"></param>
        protected virtual void Init(IPolledConfigurationSource source, AbstractPollingScheduler scheduler)
        {
        }

        /// <summary>
        ///  Stops the scheduler
        /// </summary>
        public void StopLoading()
        {
            lock (m_ObjectLock)
            {
                if (m_Scheduler != null)
                {
                    m_Scheduler.StopPolling();
                }
            }
        }
    }
}