using System;
using System.Collections.Generic;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    internal class MiniProfilerWrapper    
    {
        public MiniProfilerWrapper() { }
        
        public MiniProfilerWrapper(MiniProfiler profiler)
        {
            ProfileId = profiler.Id;
            Name =  profiler.Name;
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

        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the profiler id.
        /// Identifies this Profiler so it may be stored/cached.
        /// </summary>
        public Guid ProfileId { get; set; }
        
        /// <summary>
        /// Gets or sets a display name for this profiling session.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets when this profiler was instantiated, in UTC time.
        /// </summary>
        public DateTime Started { get; set; }

        /// <summary>
        /// Gets the milliseconds, to one decimal place, that this MiniProfiler ran.
        /// </summary>
        public decimal DurationMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets where this profiler was run.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Keys are names, values are URLs, allowing additional links to be added to a profiler result, e.g. perhaps a deeper
        /// diagnostic page for the current request.
        /// </summary>
        /// <remarks>
        /// Use <see cref="MiniProfilerExtensions.AddCustomLink"/> to easily add a name/url pair to this dictionary.
        /// </remarks>
        public Dictionary<string, string> CustomLinks { get; set; }

        /// <summary>
        /// JSON used to store Custom Links. Do not touch.
        /// </summary>
        public string CustomLinksJson { get; set; }
        
        /// <summary>
        /// Gets or sets the root timing.
        /// The first <see cref="Timing"/> that is created and started when this profiler is instantiated.
        /// All other <see cref="Timing"/>s will be children of this one.
        /// </summary>
        public Timing Root { get; set; }

        /// <summary>
        /// Gets or sets timings collected from the client
        /// </summary>
        public ClientTimings ClientTimings { get; set; }

        /// <summary>
        /// Gets or sets a string identifying the user/client that is profiling this request.
        /// </summary>
        /// <remarks>
        /// If this is not set manually at some point, the UserIdProvider implementation will be used;
        /// by default, this will be the current request's IP address.
        /// </remarks>
        public string User { get; set; }

        /// <summary>
        /// Returns true when this MiniProfiler has been viewed by the <see cref="User"/> that recorded it.
        /// </summary>
        /// <remarks>
        /// Allows POSTs that result in a redirect to be profiled. <see cref="MiniProfilerBaseOptions.Storage"/> implementation
        /// will keep a list of all profilers that haven't been fetched down.
        /// </remarks>
        public bool HasUserViewed { get; set; }

#pragma warning disable CS0618 // Type or member is obsolete
        public MiniProfiler ToMiniProfiler() => new MiniProfiler
        {
            Id = ProfileId,
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
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
