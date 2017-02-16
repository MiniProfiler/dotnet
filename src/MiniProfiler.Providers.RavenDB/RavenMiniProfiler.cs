using System;
using System.Text;
using System.Text.RegularExpressions;

using Raven.Client;
using Raven.Client.Connection.Profiling;
using StackExchange.Profiling.Helpers;

namespace StackExchange.Profiling.RavenDb
{
    /// <summary>
    /// MiniProfiler RavenDB support
    /// </summary>
    public static class MiniProfilerRaven
    {
        private static readonly Regex IndexQueryPattern = new Regex(@"/indexes/([A-Za-z/]+)");

        /// <summary>
        /// Initialize MiniProfilerRaven for the given DocumentStore (only call once!)
        /// </summary>
        /// <param name="store">The <see cref="IDocumentStore"/> to attach to</param>
        public static IDocumentStore AddMiniProfiler(this IDocumentStore store)
        {
            if (store?.HasJsonRequestFactory ?? false)
            {
                // TODO: MiniProfiler 4.1 release
                // Note: this is a terrible approach - it logs the endpoint in relation to 
                // the tree position as-of the end of the request. This may be incorrect, and the
                // start position is at best calculated. We'll need to research if there's a better way
                // to hook into profiling here. The built-in profiling is all-or-nothing session based
                // but may still be the best (or only) available (correct) option.
                store.JsonRequestFactory.LogRequest += (sender, args) =>
                {
                    var profiler = MiniProfiler.Current;
                    var head = profiler?.Head;
                    if (head != null)
                    {
                        var formattedRequest = JsonFormatter.FormatRequest(args);
                        var duration = (decimal)formattedRequest.DurationMilliseconds;

                        head.AddCustomTiming("raven", new CustomTiming(profiler, BuildCommandString(formattedRequest))
                        {
                            Id = Guid.NewGuid(),
                            StartMilliseconds = Math.Max(head.StartMilliseconds - duration, 0),
                            DurationMilliseconds = duration,
                            FirstFetchDurationMilliseconds = duration,
                            ExecuteType = formattedRequest.Status.ToString()
                        });
                    }
                };
            }

            return store;
        }

        private static string BuildCommandString(RequestResultArgs request)
        {
            var uri = new Uri(request.Url);

            var sb = new StringBuilder();
            // Basic request information
            // HTTP GET - 200 (Cached)
            sb.AppendFormat("HTTP {0} - {1} ({2})\n",
                request.Method,
                request.HttpResult,
                request.Status);

            // Request URL
            sb.AppendFormat("{0}://{1}{2}\n\n", uri.Scheme, uri.Authority, uri.AbsolutePath);
            // Append query
            if (uri.Query.HasValue())
            {
                var match = IndexQueryPattern.Match(uri.AbsolutePath);
                if (match.Success)
                {
                    string index = match.Groups[1].Value;
                    if (index.HasValue())
                        sb.Append("index=").AppendLine(index);
                }
                if (uri.Query.Length > 1)
                {
                    var qsValues = Uri.UnescapeDataString(uri.Query.Substring(1).Replace("&", "\n").Trim());
                    sb.AppendLine(qsValues);
                }
            }

            // Append POSTed data, if any (multi-get, PATCH, etc.)
            if (request.PostedData.HasValue())
            {
                sb.Append(request.PostedData);
            }

            // Set the command string to a formatted string
            return sb.ToString();
        }
    }
}
