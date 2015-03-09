using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Archaius.Dynamic;
using Archaius.Source;
using log4net;

namespace Archaius
{
    /// <summary>
    /// This class is responsible for scheduling the periodical polling of a configuration source and applying the 
    /// polling result to a Configuration.
    /// </summary>
    public abstract class AbstractPollingScheduler
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private volatile bool m_IgnoreDeletesFromSource;
        private volatile object m_CheckPoint;
        private readonly DynamicPropertyUpdater m_PropertyUpdater = new DynamicPropertyUpdater();

        /// <summary>
        /// Initialize a new instance of <see cref="AbstractPollingScheduler"/>.
        /// </summary>
        /// <param name="ignoreDeletesFromSource">
        /// true if deletes happened in the configuration source should be ignored 
        /// by the Configuration.
        /// <b>Warning: </b>If both <see cref="PollResult.Incremental"/> and this parameter are false,
        /// any property in the configuration that is missing in the polled result will be deleted once the PollResult is applied.
        /// </param>
        protected AbstractPollingScheduler(bool ignoreDeletesFromSource = false)
        {
            m_IgnoreDeletesFromSource = ignoreDeletesFromSource;
        }

        /// <summary>
        /// Gets a flag indicating if the scheduler should ignore deletes from source when applying property changes.
        /// </summary>
        public bool IgnoreDeletesFromSource
        {
            get
            {
                return m_IgnoreDeletesFromSource;
            }
            set
            {
                m_IgnoreDeletesFromSource = value;
            }
        }

        public event EventHandler<PollingEventArgs> PollingCompleted;

        /// <summary>
        /// Initiate the first poll of the configuration source and schedule the action.
        /// </summary>
        /// <param name="source">Configuration source being polled</param>
        /// <param name="config">Configuration where the properties will be updated</param>
        public void StartPolling(IPolledConfigurationSource source, IConfiguration config)
        {
            InitialLoad(source, config);
            SchedulePollingAction(source,config);
        }

        /// <summary>
        /// Stop the scheduler.
        /// </summary>
        public abstract void StopPolling();

        /// <summary>
        /// Schedule the polling action of the configuration source
        /// </summary>
        protected abstract void SchedulePollingAction(IPolledConfigurationSource source, IConfiguration config);

        /// <summary>
        /// Do an initial poll from the source and apply the result to the configuration.
        /// </summary>
        /// <param name="source">source of the configuration</param>
        /// <param name="config">Configuration to apply the polling result</param>
        /// <exception cref="Exception">if any error occurs in polling the configuration source</exception>
        protected void InitialLoad(IPolledConfigurationSource source, IConfiguration config)
        {
            PollResult result;
            try
            {
                result = source.Poll(true, null);
                m_CheckPoint = result.CheckPoint;
                RaisePollingCompletedEvent(new PollingEventArgs(PollingEventArgs.EventType.Success, result, null));
            }
            catch (Exception e)
            {
                throw new Exception("Unable to load Properties source from " + source, e);
            }
            try
            {
                PopulateProperties(result, config);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to load Properties", e);
            }
        }

        /// <summary>
        /// Apply the polled result to the configuration.
        /// If the polled result is full result from source, each property in the result is either added to set 
        /// to the configuration, and any property that is in the configuration but not in the result is deleted if IgnoreDeletesFromSource
        /// is false. If the polled result is incremental, properties added and changed in the partial result 
        /// are set with the configuration, and deleted properties are deleted form configuration if ignoreDeletesFromSource
        /// is false.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="config"></param>
        protected void PopulateProperties(PollResult result, IConfiguration config)
        {
            if (result == null || !result.HasChanges)
            {
                return;
            }
            if (!result.Incremental)
            {
                var props = result.Complete;
                if (props == null)
                {
                    return;
                }
                foreach (var prop in props)
                {
                    m_PropertyUpdater.AddOrChangeProperty(prop.Key, prop.Value, config);
                }
                var existingKeys = new HashSet<string>(config.Keys);
                if (!IgnoreDeletesFromSource)
                {
                    foreach (string key in existingKeys.Where(k => !props.ContainsKey(k)))
                    {
                        m_PropertyUpdater.DeleteProperty(key, config);
                    }
                }
            }
            else
            {
                var props = result.Added;
                if (props != null)
                {
                    foreach (var prop in props)
                    {
                        m_PropertyUpdater.AddOrChangeProperty(prop.Key, prop.Value, config);
                    }
                }
                props = result.Changed;
                if (props != null)
                {
                    foreach (var prop in props)
                    {
                        m_PropertyUpdater.AddOrChangeProperty(prop.Key, prop.Value, config);
                    }
                }
                if (!IgnoreDeletesFromSource)
                {
                    props = result.Deleted;
                    if (props != null)
                    {
                        foreach (var key in props.Keys)
                        {
                            m_PropertyUpdater.DeleteProperty(key, config);
                        }
                    }
                }
            }
        }

        protected void DoPoll(IPolledConfigurationSource source, IConfiguration config)
        {
            m_Log.Debug("Polling started");
            PollResult result;
            try
            {
                result = source.Poll(false, GetNextCheckPoint(m_CheckPoint));
                m_CheckPoint = result.CheckPoint;
                RaisePollingCompletedEvent(new PollingEventArgs(PollingEventArgs.EventType.Success, result, null));
            }
            catch (Exception e)
            {
                m_Log.Error("Error getting result from polling source", e);
                RaisePollingCompletedEvent(new PollingEventArgs(PollingEventArgs.EventType.Failure, null, e));
                return;
            }
            try
            {
                PopulateProperties(result, config);
            }
            catch (Exception e)
            {
                m_Log.Error("Error occured applying properties", e);
            }
        }

        /// <summary>
        /// Get the check point used in next <see cref="IPolledConfigurationSource.Poll"/>.
        /// The check point can be used by the <see cref="IPolledConfigurationSource"/> to determine 
        /// the set of records to return. For example, a check point can be a time stamp and 
        /// the <see cref="IPolledConfigurationSource"/> can return the records modified since the time stamp.
        /// This method is called before the poll. The 
        /// default implementation returns the check point received from last poll.
        /// </summary>
        /// <param name="lastCheckPoint">CheckPoint from last <see cref="PollResult.CheckPoint"/></param>
        /// <returns>The check point to be used for the next poll</returns>
        protected object GetNextCheckPoint(object lastCheckPoint)
        {
            return lastCheckPoint;
        }

        protected void RaisePollingCompletedEvent(PollingEventArgs args)
        {
            var invoker = PollingCompleted;
            if (invoker != null)
            {
                invoker(this, args);
            }
        }
    }
}