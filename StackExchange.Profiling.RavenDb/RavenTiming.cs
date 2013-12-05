using System;
using System.Text;
using System.Text.RegularExpressions;
using Raven.Client.Connection.Profiling;
using StackExchange.Profiling.Helpers;

namespace StackExchange.Profiling.RavenDb
{
    /// <summary>
    /// Profiles a single Raven DB request execution.
    /// </summary>
    public class RavenTiming : CustomTiming
    {
        private readonly string _requestUrl;

        private static readonly Regex IndexQueryPattern = new Regex(@"/indexes/[A-Za-z/]+");

        public RavenTiming(RequestResultArgs request, MiniProfiler profiler)
            : base(profiler, null)
        {
            if (profiler == null) throw new ArgumentNullException("profiler");

            _requestUrl = request.Url;

            var commandTextBuilder = new StringBuilder();

            // Basic request information
            // HTTP GET - 200 (Cached)
            commandTextBuilder.AppendFormat("HTTP {0} - {1} ({2})\n",
                request.Method,
                request.HttpResult,
                request.Status);

            // Request URL
            commandTextBuilder.AppendFormat("{0}\n\n", FormatUrl());

            // Append query
            var query = FormatQuery();
            if (!String.IsNullOrWhiteSpace(query)) {
                commandTextBuilder.AppendFormat("{0}\n\n", query);
            }

            // Append POSTed data, if any (multi-get, PATCH, etc.)
            if (!String.IsNullOrWhiteSpace(request.PostedData))
            {
                commandTextBuilder.Append(request.PostedData);
            }

            // Set the command string to a formatted string
            CommandString = commandTextBuilder.ToString();
        }

        /// <summary>
        /// Returns the base URL of the request
        /// </summary>
        /// <returns></returns>
        private string FormatUrl()
        {
            var results = _requestUrl.Split('?');

            if (results.Length > 0)
            {
                return results[0];
            }

            return String.Empty;
        }

        /// <summary>
        /// Returns the Raven query parameters for a request
        /// </summary>
        /// <returns></returns>
        private string FormatQuery()
        {
            var results = _requestUrl.Split('?');

            if (results.Length > 1)
            {
                string[] items = results[1].Split('&');
                string query = String.Join("\r\n", items).Trim();

                var match = IndexQueryPattern.Match(results[0]);
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
        /// Returns a snippet of the Raven command and the duration.
        /// </summary>
        public override string ToString()
        {
            var results = _requestUrl.Split('?');

            if (results.Length > 0)
            {
                return results[0].Truncate(30) + " (" + this.DurationMilliseconds + " ms)";
            }

            return base.ToString();
        }
    }
}
