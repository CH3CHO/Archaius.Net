using System;
using System.Collections.Generic;
using System.Linq;

namespace Archaius
{
    public class AppSettingsConfiguration : DictionaryConfiguration
    {
        public AppSettingsConfiguration() : base(GetAppSettings())
        {
        }

        public override void AddProperty(string key, object value)
        {
            throw new NotSupportedException("AppSettingsConfiguration is readonly.");
        }

        public override void SetProperty(string key, object value)
        {
            throw new NotSupportedException("AppSettingsConfiguration is readonly.");
        }

        public override void ClearProperty(string key)
        {
            throw new NotSupportedException("AppSettingsConfiguration is readonly.");
        }

        public override void Clear()
        {
            throw new NotSupportedException("AppSettingsConfiguration is readonly.");
        }

        private static IDictionary<string, object> GetAppSettings()
        {
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            return appSettings.Cast<string>().ToDictionary(p => p, p => (object)appSettings[p]);
        }
    }
}