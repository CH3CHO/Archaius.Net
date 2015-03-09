namespace Archaius.Source
{
    /// <summary>
    /// The definition of configuration source that brings dynamic changes to the configuration via polling.
    /// </summary>
    public interface IPolledConfigurationSource
    {
        /// <summary>
        /// Poll the configuration source to get the latest content.
        /// </summary>
        /// <param name="initial">true if this operation is the first poll.</param>
        /// <param name="checkPoint">
        /// Object that is used to determine the starting point if the result returned is incremental.
        /// Null if there is no check point or the caller wishes to get the full content.
        /// </param>
        /// <returns>The content of the configuration which may be full or incremental.</returns>
        PollResult Poll(bool initial, object checkPoint);
    }
}