using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;

namespace StackExchange.Profiling.Wcf
{
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;

    public class WcfMiniProfilerClientInspector : IClientMessageInspector
    {
        private bool _http;

        static WcfMiniProfilerClientInspector()
        {
            GetCurrentProfiler = () =>
            {
                return MiniProfiler.Current;
            };
        }

        public static Func<MiniProfiler> GetCurrentProfiler
        {
            get;
            set;
        }

        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, System.ServiceModel.IClientChannel channel)
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

                if (request.Headers.MessageVersion != MessageVersion.None)
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

        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
            var profilerStart = correlationState as MiniProfilerStart;
            // Check to see if there are any results here
            var profiler = GetCurrentProfiler();
            if (profiler != null)
            {
                // Check to see if we have a request as part of this message
                MiniProfilerResultsHeader resultsHeader = null;
                if (reply.Headers.MessageVersion != MessageVersion.None)
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
                    //if (resultsHeader.ProfilerResults.HasSqlTimings)
                        //profiler.HasSqlTimings = true;
                }
            }
        }

        private class MiniProfilerStart
        {
            public decimal StartTime { get; set; }
        }
    }
}
