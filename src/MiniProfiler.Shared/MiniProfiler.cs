using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.Internal;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling
{
    /// <summary>
    /// A single MiniProfiler can be used to represent any number of steps/levels in a call-graph, via Step()
    /// </summary>
    /// <remarks>Totally baller.</remarks>
    [DataContract]
    public partial class MiniProfiler
    {
        /// <summary>
        /// The options this profiler uses, assigned at creation time.
        /// </summary>
        [IgnoreDataMember]
        public MiniProfilerBaseOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniProfiler"/> class. 
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public MiniProfiler() { /* serialization only */ }

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniProfiler"/> class.  Creates and starts a new MiniProfiler 
        /// for the root <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of this <see cref="MiniProfiler"/>, typically a URL.</param>
        /// <param name="options">The options to use for this MiniProfiler.</param>
        public MiniProfiler(string name, MiniProfilerBaseOptions options)
        {
            Name = name;
            Id = Guid.NewGuid();
            MachineName = Environment.MachineName;
            Started = DateTime.UtcNow;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Storage = options.Storage;

            // stopwatch must start before any child Timings are instantiated
            Stopwatch = options.StopwatchProvider();
            Root = new Timing(this, null, name);
            IsActive = true;
        }

        /// <summary>
        /// Whether the profiler is currently profiling
        /// </summary>
        internal bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the profiler id.
        /// Identifies this Profiler so it may be stored/cached.
        /// </summary>
        [DataMember(Order = 1)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets a display name for this profiling session.
        /// </summary>
        [DataMember(Order = 2)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets when this profiler was instantiated, in UTC time.
        /// </summary>
        [DataMember(Order = 3)]
        public DateTime Started { get; set; }

        /// <summary>
        /// Gets the milliseconds, to one decimal place, that this MiniProfiler ran.
        /// </summary>
        [DataMember(Order = 4)]
        public decimal DurationMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets where this profiler was run.
        /// </summary>
        [DataMember(Order = 5)]
        public string MachineName { get; set; }

        /// <summary>
        /// Keys are names, values are URLs, allowing additional links to be added to a profiler result, e.g. perhaps a deeper
        /// diagnostic page for the current request.
        /// </summary>
        /// <remarks>
        /// Use <see cref="MiniProfilerExtensions.AddCustomLink"/> to easily add a name/url pair to this dictionary.
        /// </remarks>
        [DataMember(Order = 6)]
        public Dictionary<string, string> CustomLinks { get; set; }

        /// <summary>
        /// JSON used to store Custom Links. Do not touch.
        /// </summary>
        public string CustomLinksJson
        {
            get => CustomLinks?.ToJson();
            set
            {
                if (value.HasValue())
                {
                    CustomLinks = value.FromJson<Dictionary<string, string>>();
                }
            }
        }

        private Timing _root;
        /// <summary>
        /// Gets or sets the root timing.
        /// The first <see cref="Timing"/> that is created and started when this profiler is instantiated.
        /// All other <see cref="Timing"/>s will be children of this one.
        /// </summary>
        [DataMember(Order = 7)]
        public Timing Root
        {
            get => _root;
            set
            {
                _root = value;
                RootTimingId = value.Id;
            }
        }

        /// <summary>
        /// Id of Root Timing. Used for Sql Storage purposes.
        /// </summary>
        public Guid? RootTimingId { get; set; }

        /// <summary>
        /// Gets or sets timings collected from the client
        /// </summary>
        [DataMember(Order = 8)]
        public ClientTimings ClientTimings { get; set; }

        /// <summary>
        /// RedirectCount in ClientTimings. Used for sql storage.
        /// </summary>
        public int? ClientTimingsRedirectCount { get; set; }

        /// <summary>
        /// Gets or sets a string identifying the user/client that is profiling this request.
        /// </summary>
        /// <remarks>
        /// If this is not set manually at some point, the UserIdProvider implementation will be used;
        /// by default, this will be the current request's IP address.
        /// </remarks>
        [DataMember(Order = 9)]
        public string User { get; set; }

        /// <summary>
        /// Returns true when this MiniProfiler has been viewed by the <see cref="User"/> that recorded it.
        /// </summary>
        /// <remarks>
        /// Allows POSTs that result in a redirect to be profiled. <see cref="MiniProfilerBaseOptions.Storage"/> implementation
        /// will keep a list of all profilers that haven't been fetched down.
        /// </remarks>
        [DataMember(Order = 10)]
        public bool HasUserViewed { get; set; }

        // Allows async to properly track the attachment point
        private readonly FlowData<Timing> _head = new FlowData<Timing>();

        // When async context flows aren't preserved, fallback to enable correct profiling in most cases
        private Timing _lastSetHead;

        /// <summary>
        /// Gets or sets points to the currently executing Timing. 
        /// </summary>
        public Timing Head
        {
            get => _head.Value ?? _lastSetHead;
            set => _head.Value = _lastSetHead = value;
        }

        /// <summary>
        /// Gets the ticks since this MiniProfiler was started.
        /// </summary>
        internal long ElapsedTicks => Stopwatch.ElapsedTicks;

        /// <summary>
        /// Gets the timer, for unit testing, returns the timer.
        /// </summary>
        internal IStopwatch Stopwatch { get; set; }

        /// <summary>
        /// Gets the internal timer for this MiniProfile, primarily for unit testing.
        /// </summary>
        public IStopwatch GetStopwatch() => Stopwatch;

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was started.
        /// </summary>
        public static MiniProfiler Current => MiniProfilerBaseOptions.CurrentProfilerProvider?.CurrentProfiler;

        /// <summary>
        /// A <see cref="IAsyncStorage"/> strategy to use for the current profiler.
        /// </summary>
        /// <remarks>Used to set custom storage for an individual request</remarks>
        [IgnoreDataMember]
        public IAsyncStorage Storage { get; set; }

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public bool Stop(bool discardResults = false)
        {
            if (!InnerStop())
            {
                return false;
            }
            Options.ProfilerProvider.Stopped(this, discardResults);
            return true;
        }

        /// <summary>
        /// Asynchronously ends the current profiling session, if one exists. 
        /// This invokes async saving all the way down if th providers support it.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="Current"/>, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public async Task<bool> StopAsync(bool discardResults = false)
        {
            if (!InnerStop())
            {
                return false;
            }
            await Options.ProfilerProvider.StoppedAsync(this, discardResults).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// Shared stop bits for <see cref="Stop(bool)"/> and <see cref="StopAsync(bool)"/>
        /// </summary>
        /// <returns></returns>
        private bool InnerStop()
        {
            if (!Stopwatch.IsRunning)
            {
                return false;
            }

            Stopwatch.Stop();
            DurationMilliseconds = GetRoundedMilliseconds(ElapsedTicks);

            foreach (var timing in GetTimingHierarchy())
            {
                timing.Stop();
            }

            IsActive = false;

            return true;
        }

        /// <summary>
        /// Deserializes the JSON string parameter to a <see cref="MiniProfiler"/>.
        /// </summary>
        /// <param name="json">The string to deserialize int a <see cref="MiniProfiler"/>.</param>
        public static MiniProfiler FromJson(string json) => json.FromJson<MiniProfiler>();

        /// <summary>
        /// Returns the <see cref="Root"/>'s <see cref="Timing.Name"/> and <see cref="DurationMilliseconds"/> this profiler recorded.
        /// </summary>
        /// <returns>a string containing the recording information</returns>
        public override string ToString() => Root != null ? Root.Name + " (" + DurationMilliseconds + " ms)" : string.Empty;

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare to.</param>
        public override bool Equals(object obj) => obj is MiniProfiler && Id.Equals(((MiniProfiler)obj).Id);

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Walks the <see cref="Timing"/> hierarchy contained in this profiler, starting with <see cref="Root"/>, and returns each Timing found.
        /// </summary>
        public IEnumerable<Timing> GetTimingHierarchy()
        {
            var timings = new Stack<Timing>();

            timings.Push(_root);

            while (timings.Count > 0)
            {
                var timing = timings.Pop();

                yield return timing;

                if (timing.HasChildren)
                {
                    var children = timing.Children;
                    for (int i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                }
            }
        }

#if !NETSTANDARD1_5
        /// <summary>
        /// Create a DEEP clone of this MiniProfiler.
        /// </summary>
        public MiniProfiler Clone()
        {
            var serializer = new DataContractSerializer(typeof(MiniProfiler), new DataContractSerializerSettings
            {
                MaxItemsInObjectGraph = int.MaxValue,
                IgnoreExtensionDataObject = false,
                PreserveObjectReferences = true
            });
            using (var ms = new System.IO.MemoryStream())
            {
                serializer.WriteObject(ms, this);
                ms.Position = 0;
                return (MiniProfiler)serializer.ReadObject(ms);
            }
        }
#endif

        internal Timing StepImpl(string name, decimal? minSaveMs = null, bool? includeChildrenWithMinSave = false)
        {
            return new Timing(this, Head, name, minSaveMs, includeChildrenWithMinSave);
        }

        /// <summary>
        /// Returns milliseconds based on Stopwatch's Frequency, rounded to one decimal place.
        /// </summary>
        /// <param name="ticks">The tick count to round.</param>
        internal decimal GetRoundedMilliseconds(long ticks)
        {
            long z = 10000 * ticks;
            decimal timesTen = (int)(z / Stopwatch.Frequency);
            return timesTen / 10;
        }

        /// <summary>
        /// Returns how many milliseconds have elapsed since <paramref name="startTicks"/> was recorded.
        /// </summary>
        /// <param name="startTicks">The start tick count.</param>
        internal decimal GetDurationMilliseconds(long startTicks) =>
            GetRoundedMilliseconds(ElapsedTicks - startTicks);
    }
}