using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Archaius.Utils;
using log4net;

namespace Archaius.Dynamic
{
    /// <summary>
    ///  Apply the <see cref="WatchedUpdateResult"/> to the configuration.
    /// 
    /// If the result is a full result from source, each property in the result is added/set in the configuration. Any
    /// property that is in the configuration - but not in the result - is deleted if ignoreDeletesFromSource is false.
    /// 
    /// If the result is incremental, properties will be added and changed from the partial result in the configuration.
    /// Deleted properties are deleted from configuration iff ignoreDeletesFromSource is false.
    /// 
    /// This code is shared by both <see cref="AbstractPollingScheduler"/> and <see cref="DynamicWatchedConfiguration"/>
    /// </summary>
    public class DynamicPropertyUpdater
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Updates the properties in the config param given the contents of the result param.
        /// </summary>
        /// <param name="result">either an incremental or full set of data</param>
        /// <param name="config">underlying config dictionary</param>
        /// <param name="ignoreDeletesFromSource">if true, deletes will be skipped</param>
        public void UpdateProperties(WatchedUpdateResult result, IConfiguration config, bool ignoreDeletesFromSource)
        {
            if (result == null || !result.HasChanges)
            {
                return;
            }

            m_Log.DebugFormat("incremental result? [{0}]", result.Incremental);
            m_Log.DebugFormat("ignored deletes from source? [{0}]", ignoreDeletesFromSource);

            if (!result.Incremental)
            {
                var props = result.Complete;
                if (props == null)
                {
                    return;
                }
                foreach (var prop in props)
                {
                    AddOrChangeProperty(prop.Key, prop.Value, config);
                }
                var existingKeys = new HashSet<string>(config.Keys);
                if (!ignoreDeletesFromSource)
                {
                    foreach (var key in existingKeys.Where(k => !props.ContainsKey(k)))
                    {
                        DeleteProperty(key, config);
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
                        AddOrChangeProperty(prop.Key, prop.Value, config);
                    }
                }
                props = result.Changed;
                if (props != null)
                {
                    foreach (var prop in props)
                    {
                        AddOrChangeProperty(prop.Key, prop.Value, config);
                    }
                }
                if (!ignoreDeletesFromSource)
                {
                    props = result.Deleted;
                    if (props != null)
                    {
                        foreach (string name in props.Keys)
                        {
                            DeleteProperty(name, config);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add or update the property in the underlying config depending on if it exists
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newValue"></param>
        /// <param name="config"></param>
        public void AddOrChangeProperty(string name, object newValue, IConfiguration config)
        {
            // We do not want to abort the operation due to failed validation on one property
            try
            {
                if (!config.ContainsKey(name))
                {
                    m_Log.DebugFormat("Adding property key [{0}], value [{1}]", name, newValue);
                    config.AddProperty(name, newValue);
                    return;
                }
                var oldValue = config.GetProperty(name);
                if (newValue != null)
                {
                    object newValueArray;
                    if (oldValue is IList && config.ListDelimiter != '\0')
                    {
                        newValueArray = new ArrayList();
                        var values = ((string)newValue).Split(config.ListDelimiter).Select(v => v.Trim()).Where(v => v.Length != 0);
                        foreach (var value in values)
                        {
                            ((IList)newValueArray).Add(value);
                        }
                    }
                    else
                    {
                        newValueArray = newValue;
                    }
                    if (!ObjectUtils.AreEqual(newValueArray, oldValue))
                    {
                        m_Log.DebugFormat("Updating property key [{0}], value [{1}]", name, newValue);
                        config.SetProperty(name, newValue);
                    }
                }
                else if (oldValue != null)
                {
                    m_Log.DebugFormat("nulling out property key [{0}]", name);
                    config.SetProperty(name, null);
                }
            }
            catch (ValidationException e)
            {
                m_Log.Warn("Validation failed for property " + name, e);
            }
        }

        /// <summary>
        /// Delete a property in the underlying config
        /// </summary>
        /// <param name="key"></param>
        /// <param name="config"></param>
        public void DeleteProperty(string key, IConfiguration config)
        {
            if (!config.ContainsKey(key))
            {
                return;
            }
            m_Log.DebugFormat("Deleting property key [{0}]", key);
            config.ClearProperty(key);
        }
    }
}