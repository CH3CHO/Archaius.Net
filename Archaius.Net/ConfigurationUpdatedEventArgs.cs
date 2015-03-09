using System;

namespace Archaius
{
    public class ConfigurationUpdatedEventArgs : EventArgs
    {
        private readonly WatchedUpdateResult m_Result;

        public ConfigurationUpdatedEventArgs(WatchedUpdateResult result)
        {
            m_Result = result;
        }

        public WatchedUpdateResult Result
        {
            get
            {
                return m_Result;
            }
        }
    }
}