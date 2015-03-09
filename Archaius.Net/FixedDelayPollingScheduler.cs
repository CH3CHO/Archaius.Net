using System;
using System.Threading;
using Archaius.Source;

namespace Archaius
{
    /// <summary>
    /// A polling scheduler that schedule the polling with fixed delay. This class relies  on <see cref="Timer"/> to do the scheduling.
    /// </summary>
    public class FixedDelayPollingScheduler : AbstractPollingScheduler
    {
        private readonly int m_InitialDelayMillis = 30000;
        private readonly int m_DelayMillis = 60000;
        private readonly object m_ObjectLock = new object();
        private Timer m_Timer;

        /// <summary>
        /// Create an instance with the default initial delay and delay values.
        /// The scheduler will delete the property in a configuration if it is absent from the configuration source.
        /// </summary>
        public FixedDelayPollingScheduler()
        {
        }

        /// <summary>
        /// Create an instance with the given settings.
        /// </summary>
        /// <param name="initialDelayMillis">initial delay in milliseconds</param>
        /// <param name="delayMillis">delay in milliseconds</param>
        /// <param name="ignoreDeletesFromSource">
        /// whether the scheduler should ignore deletes of properties from configuration source when applying the polling result to a configuration.
        /// </param>
        public FixedDelayPollingScheduler(int initialDelayMillis, int delayMillis, bool ignoreDeletesFromSource)
            : base(ignoreDeletesFromSource)
        {
            m_InitialDelayMillis = initialDelayMillis;
            m_DelayMillis = delayMillis;
        }

        #region Overrides of AbstractPollingScheduler
        /// <summary>
        /// Stop the scheduler.
        /// </summary>
        public override void StopPolling()
        {
            lock (m_ObjectLock)
            {
                if (m_Timer == null)
                {
                    return;
                }
                m_Timer.Dispose();
                m_Timer = null;
            }
        }

        /// <summary>
        /// Schedule the polling action of the configuration source
        /// </summary>
        protected override void SchedulePollingAction(IPolledConfigurationSource source, IConfiguration config)
        {
            lock (m_ObjectLock)
            {
                if (m_Timer != null)
                {
                    throw new InvalidOperationException("The polling thread is working now.");
                }
                m_Timer = new Timer(s => DoPoll(source, config), null, m_InitialDelayMillis, m_DelayMillis);
            }
        }
        #endregion
    }
}