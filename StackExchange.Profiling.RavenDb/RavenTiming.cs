using System;
using System.Text;
using System.Text.RegularExpressions;
using Raven.Client.Connection.Profiling;

namespace StackExchange.Profiling.RavenDb
{
    /// <summary>
    /// Profiles a single SQL execution.
    /// </summary>
    public class RavenTiming : CustomTiming
    {

        public RavenTiming(MiniProfiler profiler, RequestResultArgs request)
            : base(profiler, null)
        {

            var commandTextBuilder = new StringBuilder();

            commandTextBuilder.AppendFormat("{0} HTTP status\n\n", request.HttpResult);
            commandTextBuilder.AppendFormat("Request:\n{0}\n\n", FormatQuery(request.Url));

            if (!String.IsNullOrWhiteSpace(request.PostedData))
            {
                commandTextBuilder.AppendFormat("POST:\n{0}", request.PostedData);
            }

            this.CommandString = commandTextBuilder.ToString();
        }

        private static string FormatQuery(string url)
        {
            var results = url.Split('?');

            if (results.Length > 1)
            {
                string[] items = results[1].Split('&');
                string query = String.Join("\r\n", items).Trim();

                var match = Regex.Match(results[0], @"/indexes/[A-Za-z/]+");
                if (match.Success)
                {
                    string index = match.Value.Replace("/indexes/", "");

                    if (!String.IsNullOrEmpty(index))
                        query = String.Format("index={0}\r\n", index) + query;
                }

                return Uri.UnescapeDataString(Uri.UnescapeDataString(query));
            }

            return String.Empty;
        }

        /// <summary>
        /// Returns a snippet of the SQL command and the duration.
        /// </summary>
        public override string ToString()
        {
            return this.CommandString.Truncate(30) + " (" + this.DurationMilliseconds + " ms)";
        }
    }
}
