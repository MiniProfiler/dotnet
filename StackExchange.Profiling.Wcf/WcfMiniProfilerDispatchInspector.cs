namespace StackExchange.Profiling.Wcf
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;

    /// <summary>
    /// The WCF mini profiler dispatch inspector.
    /// </summary>
    public class WcfMiniProfilerDispatchInspector : IDispatchMessageInspector
    {
        /// <summary>
        /// true if the binding is using http.
        /// </summary>
        private bool _http;

        /// <summary>
        /// after the request is received.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="instanceContext">The instance context.</param>
        /// <returns>the mini profiler start.</returns>
        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1027:TabsMustNotBeUsed", Justification = "Reviewed. Suppression is OK here.")]
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            // ReSharper disable PossibleUnintendedReferenceComparison
            if (request.Headers.MessageVersion != MessageVersion.None)
            // ReSharper restore PossibleUnintendedReferenceComparison
            {
                // Check to see if we have a request as part of this message
                var headerIndex = request.Headers.FindHeader(MiniProfilerRequestHeader.HeaderName, MiniProfilerRequestHeader.HeaderNamespace);
                if (headerIndex >= 0)
                {
                    var requestHeader = request.Headers.GetHeader<MiniProfilerRequestHeader>(headerIndex);
                    if (requestHeader != null)
                    {
                    	MiniProfiler.Settings.ProfilerProvider = new WcfRequestProfilerProvider();
                        MiniProfiler.Start();
                        return requestHeader;
                    }
                }
            }
            else if (this._http || WebOperationContext.Current != null || channel.Via.Scheme == "http" | channel.Via.Scheme == "https")
            {
                this._http = true;

                if (request.Properties.ContainsKey(HttpRequestMessageProperty.Name))
                {
                    var property = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];

                    var text = property.Headers[MiniProfilerRequestHeader.HeaderName];
                    if (!string.IsNullOrEmpty(text))
                    {
                        var header = MiniProfilerRequestHeader.FromHeaderText(text);
                    	MiniProfiler.Settings.ProfilerProvider = new WcfRequestProfilerProvider();
                        MiniProfiler.Start();
                        return header;
                    }
                }
            }
            else
                throw new InvalidOperationException("MVC Mini Profiler does not support EnvelopeNone unless HTTP is the transport mechanism");

            return null;
        }

        /// <summary>
        /// before the reply is sent.
        /// </summary>
        /// <param name="reply">The reply.</param>
        /// <param name="correlationState">The correlation state.</param>
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var requestHeader = correlationState as MiniProfilerRequestHeader;
            MiniProfiler.Stop();
            var miniProfiler = MiniProfiler.Current;

            if (miniProfiler != null && requestHeader != null)
            {
                if (requestHeader.ExcludeTrivialMethods)
                {
                    miniProfiler.Root.RemoveTrivialTimings();
                }

                var header = new MiniProfilerResultsHeader
                {
                    ProfilerResults = miniProfiler
                };

                // ReSharper disable PossibleUnintendedReferenceComparison
                if (reply.Headers.MessageVersion != MessageVersion.None)
                // ReSharper restore PossibleUnintendedReferenceComparison
                {
                    var untypedHeader = new MessageHeader<MiniProfilerResultsHeader>(header)
                    .GetUntypedHeader(MiniProfilerResultsHeader.HeaderName, MiniProfilerResultsHeader.HeaderNamespace);

                    reply.Headers.Add(untypedHeader);
                }
                else if (this._http || reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
                {
                    this._http = true;

                    var property = (HttpResponseMessageProperty)reply.Properties[HttpResponseMessageProperty.Name];
                    string text = header.ToHeaderText();
                    property.Headers.Add(MiniProfilerResultsHeader.HeaderName, text);
                }
                else
                    throw new InvalidOperationException("MVC Mini Profiler does not support EnvelopeNone unless HTTP is the transport mechanism");
            }
        }
    }
}
