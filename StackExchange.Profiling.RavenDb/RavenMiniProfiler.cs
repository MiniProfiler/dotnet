using System.Text;

namespace StackExchange.Profiling.RavenDb
{
    using System;
    using Raven.Client.Document;
    using Raven.Client.Connection.Profiling;

    public class MiniProfilerRaven
    {

        public static void InitializeFor(DocumentStore store) {

            if (store != null && store.JsonRequestFactory != null)
                store.JsonRequestFactory.LogRequest += (sender, r) => IncludeTiming(JsonFormatter.FormatRequest(r));

        }

        private static void IncludeTiming(RequestResultArgs request)
        {
            if (MiniProfiler.Current == null)
                return;

            MiniProfiler.Current.Head.AddCustomTiming("raven", new RavenTiming(MiniProfiler.Current, request)
            {
                Id = Guid.NewGuid(),
                DurationMilliseconds = (decimal)request.DurationMilliseconds,
                FirstFetchDurationMilliseconds = (decimal)request.DurationMilliseconds,
                ExecuteType = request.Status.ToString()
            });
        }
        
        
    }
}
