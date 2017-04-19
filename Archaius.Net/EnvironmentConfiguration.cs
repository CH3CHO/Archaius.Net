using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Archaius
{
    public class EnvironmentConfiguration : DictionaryConfiguration
    {
        public EnvironmentConfiguration()
            : base(GetEnvironmentVariables())
        {
        }

        public override void AddProperty(string key, object value)
        {
            throw new NotSupportedException("EnvironmentConfiguration is readonly.");
        }

        public override void SetProperty(string key, object value)
        {
            throw new NotSupportedException("EnvironmentConfiguration is readonly.");
        }

        public override void ClearProperty(string key)
        {
            throw new NotSupportedException("EnvironmentConfiguration is readonly.");
        }

        public override void Clear()
        {
            throw new NotSupportedException("EnvironmentConfiguration is readonly.");
        }

        private static IDictionary<string, object> GetEnvironmentVariables()
        {
            var variables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)
                                       .Cast<DictionaryEntry>()
                                       .ToDictionary(variable => (string)variable.Key, variable => variable.Value);
            foreach (DictionaryEntry userVariable in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User))
            {
                if (!variables.ContainsKey((string)userVariable.Key))
                {
                    variables.Add((string)userVariable.Key, userVariable.Value);
                }
            }
            foreach (DictionaryEntry machineVariable in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine))
            {
                if (!variables.ContainsKey((string)machineVariable.Key))
                {
                    variables.Add((string)machineVariable.Key, machineVariable.Value);
                }
            }
            return variables;
        }
    }
}