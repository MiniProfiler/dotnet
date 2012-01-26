using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;

namespace StackExchange.Profiling.Wcf
{
    public class WcfMiniProfilerClientInspector : IClientMessageInspector
    {
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
                var untypedHeader = new MessageHeader<MiniProfilerRequestHeader>(new MiniProfilerRequestHeader
                {
                    User = miniProfiler.User,
                    ParentProfilerId = miniProfiler.Id
                })
                .GetUntypedHeader(MiniProfilerRequestHeader.HeaderName, MiniProfilerRequestHeader.HeaderNamespace);

                request.Headers.Add(untypedHeader);

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
                var headerIndex = reply.Headers.FindHeader(MiniProfilerResultsHeader.HeaderName, MiniProfilerResultsHeader.HeaderNamespace);
                if (headerIndex >= 0)
                {
                    var resultsHeader = reply.Headers.GetHeader<MiniProfilerResultsHeader>(headerIndex);
                    if (resultsHeader != null && resultsHeader.ProfilerResults != null)
                    {
                        // Update timings of profiler results
                        if (profilerStart != null)
                            resultsHeader.ProfilerResults.Root.UpdateStartMillisecondTimingsToAbsolute(profilerStart.StartTime);

                        profiler.AddProfilerResults(resultsHeader.ProfilerResults);
                    }
                }
            }
        }

        private class MiniProfilerStart
        {
            public decimal StartTime { get; set; }
        }
    }
}
