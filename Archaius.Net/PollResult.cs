using System.Collections.Generic;

namespace Archaius
{
    /// <summary>
    /// This class represents result from a poll of configuration source. The result may be the complete 
    /// content of the configuration source, or an incremental one.
    /// </summary>
    public class PollResult : WatchedUpdateResult
    {
        protected readonly object m_CheckPoint;

        /// <summary>
        /// Create a full result that represents the complete content of the configuration source.
        /// </summary>
        /// <param name="complete">A complete dictionary that contains all the properties</param>
        /// <returns></returns>
        public new static PollResult CreateFull(IDictionary<string, object> complete)
        {
            return new PollResult(complete);
        }

        /// <summary>
        /// Create a result that represents incremental changes from the configuration source. 
        /// </summary>
        /// <param name="added">properties added</param>
        /// <param name="changed">properties changed</param>
        /// <param name="deleted">properties deleted, in which case the value in the map will be ignored</param>
        /// <param name="checkPoint">
        /// Object that served as a marker for this incremental change, for example, a timestamp of the last change.
        /// </param>
        /// <returns></returns>
        public static PollResult CreateIncremental(IDictionary<string, object> added,
            IDictionary<string, object> changed, IDictionary<string, object> deleted, object checkPoint)
        {
            return new PollResult(added, changed, deleted, checkPoint);
        }

        public PollResult(IDictionary<string, object> complete)
            : base(complete)
        {
            m_CheckPoint = null;
        }

        public PollResult(IDictionary<string, object> added, IDictionary<string, object> changed,
            IDictionary<string, object> deleted, object checkPoint)
            : base(added, changed, deleted)
        {
            m_CheckPoint = checkPoint;
        }

        /// <summary>
        /// Gets the check point (marker) for this poll result.
        /// </summary>
        public object CheckPoint
        {
            get
            {
                return m_CheckPoint;
            }
        }
    }
}