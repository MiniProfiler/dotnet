using System;
using System.Collections.Generic;

namespace StackExchange.Profiling.Storage.Internal
{
    internal class MiniProfilerDoc
    {
        public string Id { get; set; }
        public Guid ProfilerId { get; set; }
        public string Name { get; set; }
        public DateTime Started { get; set; }
        public decimal DurationMilliseconds { get; set; }
        public string MachineName { get; set; }
        public Dictionary<string, string> CustomLinks { get; set; }
        public string CustomLinksJson { get; set; }
        public Timing Root { get; set; }
        public ClientTimings ClientTimings { get; set; }
        public string User { get; set; }
        public bool HasUserViewed { get; set; }

        public MiniProfilerDoc() { }

        public MiniProfilerDoc(MiniProfiler profiler)
        {
            ProfilerId = profiler.Id;
            Name = profiler.Name;
            Started = profiler.Started;
            DurationMilliseconds = profiler.DurationMilliseconds;
            MachineName = profiler.MachineName;
            CustomLinks = profiler.CustomLinks;
            CustomLinksJson = profiler.CustomLinksJson;
            Root = profiler.Root;
            ClientTimings = profiler.ClientTimings;
            User = profiler.User;
            HasUserViewed = profiler.HasUserViewed;
        }

#pragma warning disable CS0618 // Type or member is obsolete (MiniProfiler serialization constructor)
        public MiniProfiler ToMiniProfiler() => new()
        {
            Id = ProfilerId,
            Name = Name,
            Started = Started,
            DurationMilliseconds = DurationMilliseconds,
            MachineName = MachineName,
            CustomLinks = CustomLinks,
            CustomLinksJson = CustomLinksJson,
            Root = Root,
            ClientTimings = ClientTimings,
            User = User,
            HasUserViewed = HasUserViewed
        };
#pragma warning restore CS0618 // Type or member is obsolete (MiniProfiler serialization constructor)
    }
}
