using System;

namespace Archaius
{
    public class ConfigurationEventArgs : EventArgs
    {
        public ConfigurationEventArgs()
        {
        }

        public ConfigurationEventArgs(ConfigurationEventType type, bool beforeOperation)
            : this(type, null, null, beforeOperation)
        {
        }

        public ConfigurationEventArgs(ConfigurationEventType type, string name, bool beforeOperation)
            : this(type, name, null, beforeOperation)
        {
        }

        public ConfigurationEventArgs(ConfigurationEventType type, string name, object value, bool beforeOperation)
        {
            Type = type;
            Name = name;
            Value = value;
            BeforeOperation = beforeOperation;
        }

        public ConfigurationEventType Type
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public object Value
        {
            get;
            private set;
        }

        public bool BeforeOperation
        {
            get;
            private set;
        }
    }
}