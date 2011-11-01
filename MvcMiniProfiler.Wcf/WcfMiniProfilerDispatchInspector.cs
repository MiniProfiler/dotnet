using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Dispatcher;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace MvcMiniProfiler.Wcf
{
    public class WcfMiniProfilerDispatchInspector : IDispatchMessageInspector
    {
        public object AfterReceiveRequest(ref System.ServiceModel.Channels.Message request, System.ServiceModel.IClientChannel channel, System.ServiceModel.InstanceContext instanceContext)
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

            return null;
        }

        public void BeforeSendReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
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

                var untypedHeader = new MessageHeader<MiniProfilerResultsHeader>(new MiniProfilerResultsHeader
                {
                    ProfilerResults = miniProfiler
                })
                .GetUntypedHeader(MiniProfilerResultsHeader.HeaderName, MiniProfilerResultsHeader.HeaderNamespace);

                reply.Headers.Add(untypedHeader);
            }

            //try
            //{
            //    var arrayOfIds = Settings.Storage.GetUnviewedIds(current.User).ToJson();
            //    // allow profiling of ajax requests
            //    response.AppendHeader("X-MiniProfiler-Ids", arrayOfIds);
            //}
            //catch { } // headers blew up


        }
    }
}
