using System;
using System.Collections.Generic;

namespace Archaius
{
    public interface IAggregatedConfiguration : IConfiguration
    {
        ISet<String> ConfigurationNames
        {
            get;
        }

        int NumberOfConfigurations
        {
            get;
        }

        void AddConfiguration(AbstractConfiguration config, string name = null);

        IConfiguration GetConfiguration(string name);

        IConfiguration GetConfiguration(int index);

        IList<AbstractConfiguration> GetConfigurations();

        IConfiguration RemoveConfiguration(string name);

        bool RemoveConfiguration(IConfiguration config);

        IConfiguration RemoveConfigurationAt(int index);
    }
}