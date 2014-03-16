﻿namespace StackExchange.Profiling.RavenDb
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using Raven.Client.Document;
    using Raven.Client.Connection.Profiling;

    public class MiniProfilerRaven
    {
        private static readonly Regex IndexQueryPattern = new Regex(@"/indexes/[A-Za-z/]+");

        /// <summary>
        /// Initialize MiniProfilerRaven for the given DocumentStore (only call once!)
        /// </summary>
        /// <param name="store">The <see cref="DocumentStore"/> to attach to</param>
        public static void InitializeFor(DocumentStore store)
        {

            if (store != null && store.JsonRequestFactory != null)
                store.JsonRequestFactory.LogRequest += (sender, r) => IncludeTiming(JsonFormatter.FormatRequest(r));

        }

        private static void IncludeTiming(RequestResultArgs request)
        {
            if (MiniProfiler.Current == null || MiniProfiler.Current.Head == null)
                return;

            MiniProfiler.Current.Head.AddCustomTiming("raven", new CustomTiming(MiniProfiler.Current, BuildCommandString(request))
            {
                Id = Guid.NewGuid(),
                DurationMilliseconds = (decimal)request.DurationMilliseconds,
                FirstFetchDurationMilliseconds = (decimal)request.DurationMilliseconds,
                ExecuteType = request.Status.ToString()
            });
        }

        private static string BuildCommandString(RequestResultArgs request)
        {
            var url = request.Url;

            var commandTextBuilder = new StringBuilder();

            // Basic request information
            // HTTP GET - 200 (Cached)
            commandTextBuilder.AppendFormat("HTTP {0} - {1} ({2})\n",
                request.Method,
                request.HttpResult,
                request.Status);

            // Request URL
            commandTextBuilder.AppendFormat("{0}\n\n", FormatUrl(url));

            // Append query
            var query = FormatQuery(url);
            if (!String.IsNullOrWhiteSpace(query))
            {
                commandTextBuilder.AppendFormat("{0}\n\n", query);
            }

            // Append POSTed data, if any (multi-get, PATCH, etc.)
            if (!String.IsNullOrWhiteSpace(request.PostedData))
            {
                commandTextBuilder.Append(request.PostedData);
            }

            // Set the command string to a formatted string
            return commandTextBuilder.ToString();
        }

        private static string FormatUrl(string requestUrl)
        {
            var results = requestUrl.Split('?');

            if (results.Length > 0)
            {
                return results[0];
            }

            return String.Empty;
        }

        private static string FormatQuery(string url)
        {
            var results = url.Split('?');

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

    }
}