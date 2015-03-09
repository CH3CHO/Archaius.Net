using System.Collections.Generic;

namespace Archaius
{
    /// <summary>
    /// This class represents the result of a callback from the WatchedConfigurationSource. The result may be the complete
    /// content of the configuration source - or an incremental one.
    /// </summary>
    public class WatchedUpdateResult
    {
        protected readonly IDictionary<string, object> m_Complete, m_Added, m_Changed, m_Deleted;
        protected readonly bool m_Incremental;

        /// <summary>
        /// Create a full result that represents the complete content of the configuration source.
        /// </summary>
        /// <param name="complete">A dictionary that contains all the properties</param>
        /// <returns></returns>
        public static WatchedUpdateResult CreateFull(IDictionary<string, object> complete)
        {
            return new WatchedUpdateResult(complete);
        }

        /// <summary>
        /// Create a result that represents incremental changes from the configuration source.
        /// </summary>
        /// <param name="added">properties added</param>
        /// <param name="changed">properties changed</param>
        /// <param name="deleted">properties deleted, in which case the value in the map will be ignored</param>
        /// <returns></returns>
        public static WatchedUpdateResult CreateIncremental(IDictionary<string, object> added, IDictionary<string, object> changed,
                IDictionary<string, object> deleted)
        {
            return new WatchedUpdateResult(added, changed, deleted);
        }

        /// <summary>
        /// Get complete content from configuration source. null if the result is incremental.
        /// </summary>
        public IDictionary<string, object> Complete
        {
            get
            {
                return m_Complete;
            }
        }

        /// <summary>
        /// Gets the added properties in the configuration source as a map.
        /// </summary>
        public IDictionary<string, object> Added
        {
            get
            {
                return m_Added;
            }
        }

        /// <summary>
        /// Gets the changed properties in the configuration source as a map.
        /// </summary>
        public IDictionary<string, object> Changed
        {
            get
            {
                return m_Changed;
            }
        }

        /// <summary>
        /// Gets  the deleted properties in the configuration source as a map.
        /// </summary>
        public IDictionary<string, object> Deleted
        {
            get
            {
                return m_Deleted;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the result is incremental.
        /// false if it is the complete content of the configuration source.
        /// </summary>
        public bool Incremental
        {
            get
            {
                return m_Incremental;
            }
        }

        /// <summary>
        /// Indicate whether this result has any content. If the result is incremental, this is true if there is any any
        /// added, changed or deleted properties. If the result is complete, this is true if <see cref="Complete"/> is null.
        /// </summary>
        public bool HasChanges
        {
            get
            {
                if (!Incremental)
                {
                    return Complete != null;
                }
                return (Added != null && Added.Count > 0) || (Changed != null && Changed.Count > 0)
                       || (Deleted != null && Deleted.Count > 0);
            }
        }

        public WatchedUpdateResult(IDictionary<string, object> complete)
        {
            m_Complete = complete;
            m_Added = null;
            m_Changed = null;
            m_Deleted = null;
            m_Incremental = false;
        }

        public WatchedUpdateResult(IDictionary<string, object> added, IDictionary<string, object> changed,
            IDictionary<string, object> deleted)
        {
            m_Complete = null;
            m_Added = added;
            m_Changed = changed;
            m_Deleted = deleted;
            m_Incremental = true;
        }
    }
}