namespace StackExchange.Profiling.RavenDb
{
    using System;
    using Raven.Client.Document;
    using Raven.Client.Connection.Profiling;

    public class MiniProfilerRaven
    {
        /// <summary>
        /// Initialize MiniProfilerRaven for the given DocumentStore (only call once!)
        /// </summary>
        /// <param name="store">The <see cref="DocumentStore"/> to attach to</param>
        public static void InitializeFor(DocumentStore store) {

            if (store != null && store.JsonRequestFactory != null)
                store.JsonRequestFactory.LogRequest += (sender, r) => IncludeTiming(JsonFormatter.FormatRequest(r));

        }
       
        private static void IncludeTiming(RequestResultArgs request)
        {
            if (MiniProfiler.Current == null || MiniProfiler.Current.Head == null)
                return;

            MiniProfiler.Current.Head.AddCustomTiming("raven", new RavenTiming(request, MiniProfiler.Current)
            {
                Id = Guid.NewGuid(),
                DurationMilliseconds = (decimal)request.DurationMilliseconds,
                FirstFetchDurationMilliseconds = (decimal)request.DurationMilliseconds,
                ExecuteType = request.Status.ToString()
            });
        }
        
        
    }
}
