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
            return Environment.GetEnvironmentVariables()
                    .Cast<DictionaryEntry>()
                    .ToDictionary(variable => (string)variable.Key, variable => variable.Value);
        }
    }
}