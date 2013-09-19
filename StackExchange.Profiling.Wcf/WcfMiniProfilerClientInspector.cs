namespace StackExchange.Profiling.Wcf
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;

    /// <summary>
    /// The WCF mini profiler client inspector.
    /// </summary>
    public class WcfMiniProfilerClientInspector : IClientMessageInspector
    {
        /// <summary>
        /// true if the binding is using http.
        /// </summary>
        private bool _http;

        /// <summary>
        /// Initialises static members of the <see cref="WcfMiniProfilerClientInspector"/> class.
        /// </summary>
        static WcfMiniProfilerClientInspector()
        {
            GetCurrentProfiler = () => MiniProfiler.Current;
        }

        /// <summary>
        /// Gets or sets the get current profiler.
        /// </summary>
        public static Func<MiniProfiler> GetCurrentProfiler { get; set; }

        /// <summary>
        /// before the send request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>the mini profiler start</returns>
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            // If we currently are running inside a MiniProfiler context, then add a request onto this request
            var miniProfiler = GetCurrentProfiler();
            if (miniProfiler != null)
            {
                var header = new MiniProfilerRequestHeader
                {
                    User = miniProfiler.User,
                    ParentProfilerId = miniProfiler.Id
                };

                // ReSharper disable PossibleUnintendedReferenceComparison
                if (request.Headers.MessageVersion != MessageVersion.None)
                // ReSharper restore PossibleUnintendedReferenceComparison
                {
                    var untypedHeader = new MessageHeader<MiniProfilerRequestHeader>(header)
                    .GetUntypedHeader(MiniProfilerRequestHeader.HeaderName, MiniProfilerRequestHeader.HeaderNamespace);
                    request.Headers.Add(untypedHeader);
                }
                else if (_http || WebOperationContext.Current != null || channel.Via.Scheme == "http" | channel.Via.Scheme == "https")
                {
                    _http = true;

                    HttpRequestMessageProperty property = null;
                    if (!request.Properties.ContainsKey(HttpRequestMessageProperty.Name))
                        request.Properties.Add(HttpRequestMessageProperty.Name, new HttpRequestMessageProperty());
                    property = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];

                    property.Headers.Add(MiniProfilerRequestHeader.HeaderName, header.ToHeaderText());
                }
                else
                    throw new InvalidOperationException("MVC Mini Profiler does not support EnvelopeNone unless HTTP is the transport mechanism");

                return new MiniProfilerStart { StartTime = miniProfiler.DurationMilliseconds };
            }

            return null;
        }

        /// <summary>
        /// after the reply is received.
        /// </summary>
        /// <param name="reply">The reply.</param>
        /// <param name="correlationState">The correlation state.</param>
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            var profilerStart = correlationState as MiniProfilerStart;

            // Check to see if there are any results here
            var profiler = GetCurrentProfiler();
            if (profiler != null)
            {
                // Check to see if we have a request as part of this message
                MiniProfilerResultsHeader resultsHeader = null;
                // ReSharper disable PossibleUnintendedReferenceComparison
                if (reply.Headers.MessageVersion != MessageVersion.None)
                // ReSharper restore PossibleUnintendedReferenceComparison
                {
                    var headerIndex = reply.Headers.FindHeader(MiniProfilerResultsHeader.HeaderName, MiniProfilerResultsHeader.HeaderNamespace);
                    if (headerIndex >= 0)
                        resultsHeader = reply.Headers.GetHeader<MiniProfilerResultsHeader>(headerIndex);
                }
                else if (_http || reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
                {
                    _http = true;

                    var property = (HttpResponseMessageProperty)reply.Properties[HttpResponseMessageProperty.Name];

                    var text = property.Headers[MiniProfilerResultsHeader.HeaderName];
                    if (!string.IsNullOrEmpty(text))
                        resultsHeader = MiniProfilerResultsHeader.FromHeaderText(text);
                }
                else
                    throw new InvalidOperationException("MVC Mini Profiler does not support EnvelopeNone unless HTTP is the transport mechanism");

                if (resultsHeader != null && resultsHeader.ProfilerResults != null)
                {
                    // Update timings of profiler results
                    if (profilerStart != null)
                        resultsHeader.ProfilerResults.Root.UpdateStartMillisecondTimingsToAbsolute(profilerStart.StartTime);

                    profiler.AddProfilerResults(resultsHeader.ProfilerResults);
                }
            }
        }

        /// <summary>
        /// The mini profiler start.
        /// </summary>
        private class MiniProfilerStart
        {
            /// <summary>
            /// Gets or sets the start time.
            /// </summary>
            public decimal StartTime { get; set; }
        }
    }
}
