namespace StackExchange.Profiling.RavenDb
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using Raven.Client.Document;
    using Raven.Client.Connection.Profiling;
    using System.Web;

    public class MiniProfilerRaven
    {
        private const string RavenHandledRequestMarker = "__MiniProfiler.Raven_handled";
        private const string RavenRequestPrefix = "__MiniProfiler.Raven.Request.";
        private const string RavenRequestPending = "Pending";
        private const string RavenRequestHandled = "Handled";

        private static readonly Regex IndexQueryPattern = new Regex(@"/indexes/[A-Za-z/]+");        

        /// <summary>
        /// Initialize MiniProfilerRaven for the given DocumentStore (only call once!)
        /// </summary>
        /// <param name="store">The <see cref="DocumentStore"/> to attach to</param>
        public static void InitializeFor(DocumentStore store)
        {
            if (store != null && store.JsonRequestFactory != null)
            {
                store.JsonRequestFactory.ConfigureRequest += (sender, args) =>
                {
                    EventHandler<RequestResultArgs> handler = null;
                    
                    var profiler = MiniProfiler.Current;
                    var httpContext = HttpContext.Current;

                    if (profiler != null && profiler.Head != null && httpContext != null)
                    {
                        var requestId = Guid.NewGuid();

                        // assign a unique request ID to this context since
                        // HttpContext may be shared across events, due to singleton-nature
                        // of DocumentStore
                        if (!httpContext.Items.Contains(RavenRequestPrefix + requestId))
                        {
                            httpContext.Items[RavenRequestPrefix + requestId] = RavenRequestPending;
                        }

                        handler = (s, r) =>
                        {
                            store.JsonRequestFactory.LogRequest -= handler;

                            // add a "handled" marker because not every ConfigureRequest event is for a single query
                            // so if we've already timed this request, ignore otherwise we will have dup timings                            
                            if (r.AdditionalInformation.ContainsKey(RavenHandledRequestMarker))
                                return;

                            // have we handled this request on this context?
                            if ((string)httpContext.Items[RavenRequestPrefix + requestId] == RavenRequestHandled)
                                return;

                            // add handled marker to request
                            r.AdditionalInformation.Add(RavenHandledRequestMarker, "");

                            // mark this request as handled on this context
                            httpContext.Items[RavenRequestPrefix + requestId] = RavenRequestHandled;

                            // add custom timing
                            IncludeTiming(r, profiler);                            
                        };

                        store.JsonRequestFactory.LogRequest += handler;
                    }
                };
            }

        }

        private static void IncludeTiming(RequestResultArgs request, MiniProfiler profiler)
        {
            if (profiler == null || profiler.Head == null)
            {
                return;
            }
            
            var formattedRequest = JsonFormatter.FormatRequest(request);

            profiler.Head.AddCustomTiming("raven", new CustomTiming(profiler, BuildCommandString(formattedRequest))
            {
                Id = Guid.NewGuid(),
                DurationMilliseconds = (decimal)formattedRequest.DurationMilliseconds,
                FirstFetchDurationMilliseconds = (decimal)formattedRequest.DurationMilliseconds,
                ExecuteType = formattedRequest.Status.ToString()
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
