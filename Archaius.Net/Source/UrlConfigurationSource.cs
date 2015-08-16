using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Reflection;
using log4net;

namespace Archaius.Source
{
    public class UrlConfigurationSource : IPolledConfigurationSource
    {
        /// <summary>
        /// AppConfig property name to define a set of URLs to be used by the default constructor. 
        /// </summary>
        public static readonly string ConfigUrlPropertyName = "Archaius.ConfigUrls";

        public static readonly char UrlSeparator = ';';

        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string[] m_ConfigUrls;

        /// <summary>
        /// Create the instance for the default list of URLs, which is composed by the following order
        ///  <ul>
        /// <li>A configuration file (default name to be <code>config.properties</code>, see {@link #DEFAULT_CONFIG_FILE_NAME}) on the classpath</li>
        /// <li>A list of URLs defined by system property {@value #CONFIG_URL} with values separated by comma <code>","</code>.</li>
        /// </ul>
        /// </summary>
        public UrlConfigurationSource()
        {
            var fileNames = GetDefaultFileSources();
            m_ConfigUrls = fileNames;
            if (m_ConfigUrls.Length == 0)
            {
                m_ConfigUrls = new string[0];
                m_Log.Warn("No URLs will be polled as dynamic configuration sources.");
                m_Log.InfoFormat(
                    "To enable URLs as dynamic configuration sources, define {0} in AppConfig section of app.config or web.config file.",
                    ConfigUrlPropertyName);
            }
            else
            {
                m_Log.Info("URLs to be used as dynamic configuration source: " + string.Join(",", m_ConfigUrls));
            }
        }

        /// <summary>
        /// Create an instance with a list URLs to be used.
        /// </summary>
        /// <param name="urls"></param>
        public UrlConfigurationSource(params string[] urls)
        {
            m_ConfigUrls = urls;
        }

        public IList<string> ConfigUrls
        {
            get
            {
                return new ReadOnlyCollection<string>(m_ConfigUrls);
            }
        }

        private static string[] GetDefaultFileSources()
        {
            var name = System.Configuration.ConfigurationManager.AppSettings[ConfigUrlPropertyName];
            return !string.IsNullOrEmpty(name) ? name.Split(new[] {UrlSeparator}, StringSplitOptions.RemoveEmptyEntries) : new string[0];
        }

        /// <summary>
        /// Poll the configuration source to get the latest content.
        /// </summary>
        /// <param name="initial">true if this operation is the first poll.</param>
        /// <param name="checkPoint">
        /// Object that is used to determine the starting point if the result returned is incremental.
        /// Null if there is no check point or the caller wishes to get the full content.
        /// </param>
        /// <returns>The content of the configuration which may be full or incremental.</returns>
        public PollResult Poll(bool initial, object checkPoint)
        {
            if (m_ConfigUrls == null || m_ConfigUrls.Length == 0)
            {
                return PollResult.CreateFull(null);
            }
            var properties = new Dictionary<string, object>();
            foreach (var url in m_ConfigUrls)
            {
                var urlProperties = DownloadProperties(url);
                foreach (var urlProperty in urlProperties)
                {
                    properties[urlProperty.Key] = urlProperty.Value;
                }
            }
            return PollResult.CreateFull(properties);
        }

        private static IDictionary<string, object> DownloadProperties(string url)
        {
            var request = WebRequest.Create(url);
            var response = request.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return ReadProperties(reader);
            }
        }

        private static IDictionary<string, object> ReadProperties(TextReader reader)
        {
            var properties = new Dictionary<string, object>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                var parts = line.Split(new[] {'='}, 2);
                if (parts.Length < 2)
                {
                    continue;
                }
                properties[parts[0].Trim()] = parts[1].Trim();
            }
            return properties;
        }

        public override string ToString()
        {
            return "UrlConfigurationSource [Urls=" + string.Join(",", m_ConfigUrls) + "]";
        }
    }
}