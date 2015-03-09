using System;

namespace Archaius
{
    public class PollingEventArgs : EventArgs
    {
        public enum EventType
        {
            Success,
            Failure
        }

        public PollingEventArgs(EventType type, PollResult result, Exception exception)
        {
            Type = type;
            Result = result;
            Exception = exception;
        }

        public EventType Type
        {
            get;
            private set;
        }

        public PollResult Result
        {
            get;
            private set;
        }

        public Exception Exception
        {
            get;
            private set;
        }
    }
}