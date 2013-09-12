namespace StackExchange.Profiling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Web;
    using System.Web.Script.Serialization;

    using StackExchange.Profiling.Helpers;

    /// <summary>
    /// A single MiniProfiler can be used to represent any number of steps/levels in a call-graph, via Step()
    /// </summary>
    /// <remarks>Totally baller.</remarks>
    [DataContract]
    public partial class MiniProfiler
    {
        /// <summary>
        /// Starts when this profiler is instantiated. Each <see cref="Timing"/> step will use this Stopwatch's current ticks as
        /// their starting time.
        /// </summary>
        private readonly IStopwatch _sw;

        /// <summary>
        /// The root.
        /// </summary>
        private Timing _root;

        /// <summary>
        /// Initialises a new instance of the <see cref="MiniProfiler"/> class. 
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public MiniProfiler()
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MiniProfiler"/> class. 
        /// Creates and starts a new MiniProfiler for the root <paramref name="url"/>, filtering <see cref="Timing"/> steps to <paramref name="level"/>.
        /// </summary>
        /// <param name="url">
        /// The URL.
        /// </param>
        /// <param name="level">
        /// The level.
        /// </param>
        public MiniProfiler(string url, ProfileLevel level = ProfileLevel.Info)
        {
            Id = Guid.NewGuid();
            Level = level;
            SqlProfiler = new SqlProfiler(this);
            MachineName = Environment.MachineName;
            Started = DateTime.UtcNow;

            // stopwatch must start before any child Timings are instantiated
            _sw = Settings.StopwatchProvider();
            Root = new Timing(this, null, url);
        }

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start"/>ed.
        /// </summary>
        public static MiniProfiler Current
        {
            get
            {
                Settings.EnsureProfilerProvider();
                return Settings.ProfilerProvider.GetCurrentProfiler();
            }
        }

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
        /// Gets or sets when this profiler was instantiated.
        /// </summary>
        [DataMember(Order = 3)]
        public DateTime Started { get; set; }

        /// <summary>
        /// Gets or sets where this profiler was run.
        /// </summary>
        [DataMember(Order = 4)]
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets whether or now filtering is allowed of <see cref="Timing"/> steps based on what <see cref="ProfileLevel"/> 
        /// the steps are created with.
        /// </summary>
        [DataMember(Order = 5)]
        public ProfileLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the root timing.
        /// The first <see cref="Timing"/> that is created and started when this profiler is instantiated.
        /// All other <see cref="Timing"/>s will be children of this one.
        /// </summary>
        [DataMember(Order = 6)]
        public Timing Root
        {
            get
            {
                return _root;
            }

            set
            {
                _root = value;

                // when being deserialized, we need to go through and set all child timings' parents
                if (_root.HasChildren)
                {
                    var timings = new Stack<Timing>();

                    timings.Push(_root);

                    while (timings.Count > 0)
                    {
                        var timing = timings.Pop();

                        if (timing.HasChildren)
                        {
                            var children = timing.Children;

                            for (int i = children.Count - 1; i >= 0; i--)
                            {
                                children[i].ParentTiming = timing;
                                timings.Push(children[i]); // FLORIDA!  TODO: refactor this and other stack creation methods into one 
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a string identifying the user/client that is profiling this request. Set <c>UserProvider</c>"/>
        /// with an <see cref="IUserProvider"/>-implementing class to provide a custom value.
        /// </summary>
        /// <remarks>
        /// If this is not set manually at some point, the <c>UserProvider</c>"/> implementation will be used;
        /// by default, this will be the current request's IP address.
        /// </remarks>
        [DataMember(Order = 7)]
        public string User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the profile has been viewed.
        /// Returns true when this MiniProfiler has been viewed by the <see cref="User"/> that recorded it.
        /// </summary>
        /// <remarks>
        /// Allows POSTs that result in a redirect to be profiled. <see cref="MiniProfiler.Settings.Storage"/> implementation
        /// will keep a list of all profilers that haven't been fetched down.
        /// </remarks>
        [DataMember(Order = 8)]
        public bool HasUserViewed { get; set; }

        /// <summary>
        /// Gets or sets timings collected from the client
        /// </summary>
        [DataMember(Order = 9)]
        public ClientTimings ClientTimings { get; set; }

        /// <summary>
        /// Gets the milliseconds, to one decimal place, that this MiniProfiler ran.
        /// </summary>
        public decimal DurationMilliseconds
        {
            get { return _root.DurationMilliseconds ?? GetRoundedMilliseconds(ElapsedTicks); }
        }

        /// <summary>
        /// Returns true if any child timing has a duration less than <see cref="TrivialDurationThresholdMilliseconds"/>.
        /// </summary>
        public bool HasTrivialTimings
        {
            get { return GetTimingHierarchy().Any(t => t.IsTrivial); }
        }

        /// <summary>
        /// Returns true when all child <see cref="Timing"/>s are <see cref="Timing.IsTrivial"/>.
        /// </summary>
        public bool HasAllTrivialTimings
        {
            get { return GetTimingHierarchy().All(t => t.IsTrivial); }
        }

        /// <summary>
        /// Returns total
        /// </summary>
        public decimal TrivialMilliseconds
        {
            get { return GetTimingHierarchy().Where(t => t.IsTrivial).Sum(t => t.DurationMilliseconds ?? 0); }
        }

        /// <summary>
        /// Gets any Timing step with a duration less than or equal to this will be hidden by default in the UI; defaults to 2.0 ms.
        /// </summary>
        public decimal TrivialDurationThresholdMilliseconds
        {
            get { return Settings.TrivialDurationThresholdMilliseconds; }
        }

        /// <summary>
        /// Gets or sets points to the currently executing Timing. 
        /// </summary>
        public Timing Head { get; set; }

        /// <summary>
        /// Gets the ticks since this MiniProfiler was started.
        /// </summary>
        internal long ElapsedTicks
        {
            get { return _sw.ElapsedTicks; }
        }

        /// <summary>
        /// Gets the timer, for unit testing, returns the timer.
        /// </summary>
        internal IStopwatch Stopwatch
        {
            get { return _sw; }
        }

        /// <summary>
        /// Starts a new MiniProfiler based on the current <see cref="IProfilerProvider"/>. This new profiler can be accessed by
        /// <see cref="MiniProfiler.Current"/>
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>the mini profiler.</returns>
        public static MiniProfiler Start(ProfileLevel level = ProfileLevel.Info)
        {
            Settings.EnsureProfilerProvider();
            return Settings.ProfilerProvider.Start(level);
        }

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public static void Stop(bool discardResults = false)
        {
            Settings.EnsureProfilerProvider();
            Settings.ProfilerProvider.Stop(discardResults);
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal. Use this method when you
        /// do not wish to include the StackExchange.Profiling namespace for the <see cref="MiniProfilerExtensions.Step"/> extension method.
        /// </summary>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start"/> is called.</param>
        /// <returns>the static step.</returns>
        public static IDisposable StepStatic(string name, ProfileLevel level = ProfileLevel.Info)
        {
            return Current.Step(name, level);
        }

        /// <summary>
        /// Renders the current <see cref="MiniProfiler"/> to JSON.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public static string ToJson()
        {
            return ToJson(Current);
        }

        /// <summary>
        /// Renders the parameter <see cref="MiniProfiler"/> to JSON.
        /// </summary>
        /// <param name="profiler">The profiler.</param>
        /// <returns>a string containing the JSON result.</returns>
        public static string ToJson(MiniProfiler profiler)
        {
            if (profiler == null) return null;

            var result = new JavaScriptSerializer { MaxJsonLength = Settings.MaxJsonResponseSize }.Serialize(profiler);
            return result;
        }

        /// <summary>
        /// <c>Deserializes</c> the JSON string parameter to a <see cref="MiniProfiler"/>.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>the mini profiler</returns>
        public static MiniProfiler FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var result = new JavaScriptSerializer { MaxJsonLength = Settings.MaxJsonResponseSize }.Deserialize<MiniProfiler>(json);
            return result;
        }

        /// <summary>
        /// Returns the <c>css</c> and <c>javascript</c> includes needed to display the MiniProfiler results UI.
        /// </summary>
        /// <param name="position">Which side of the page the profiler popup button should be displayed on (defaults to left)</param>
        /// <param name="showTrivial">Whether to show trivial timings by default (defaults to false)</param>
        /// <param name="showTimeWithChildren">Whether to show time the time with children column by default (defaults to false)</param>
        /// <param name="maxTracesToShow">The maximum number of trace popups to show before removing the oldest (defaults to 15)</param>
        /// <param name="showControls">when true, shows buttons to minimize and clear MiniProfiler results</param>
        /// <param name="useExistingjQuery">Whether MiniProfiler should attempt to load its own version of jQuery, or rely on a version previously loaded on the page</param>
        /// <param name="samplingOnly">The sampling Only.</param>
        /// <returns>Script and link elements normally; an empty string when there is no active profiling session.</returns>
        public static IHtmlString RenderIncludes(
            RenderPosition? position = null, 
            bool? showTrivial = null, 
            bool? showTimeWithChildren = null, 
            int? maxTracesToShow = null, 
            bool? showControls = null,
            bool? useExistingjQuery = null, // TODO: we need to deprecate this
            bool samplingOnly = false,      // TODO: can we remove this?
            bool? startHidden = null)
        {
            return MiniProfilerHandler.RenderIncludes(Current, position, showTrivial, showTimeWithChildren, maxTracesToShow, showControls, startHidden);
        }

        /// <summary>
        /// Returns the <see cref="Root"/>'s <see cref="Timing.Name"/> and <see cref="DurationMilliseconds"/> this profiler recorded.
        /// </summary>
        /// <returns>a string containing the recording information</returns>
        public override string ToString()
        {
            return Root != null ? Root.Name + " (" + DurationMilliseconds + " ms)" : string.Empty;
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        /// <param name="rValue">The rValue.</param>
        /// <returns>true if the profilers are equal.</returns>
        public override bool Equals(object rValue)
        {
            return rValue is MiniProfiler && Id.Equals(((MiniProfiler)rValue).Id);
        }

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        /// <returns>an integer containing the hash code.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Walks the <see cref="Timing"/> hierarchy contained in this profiler, starting with <see cref="Root"/>, and returns each Timing found.
        /// </summary>
        /// <returns>the set of timings.</returns>
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

        /// <summary>
        /// Create a DEEP clone of this object
        /// </summary>
        /// <returns>the mini profiler.</returns>
        public MiniProfiler Clone()
        {
            var serializer = new DataContractSerializer(typeof(MiniProfiler), null, int.MaxValue, false, true, null);
            using (var ms = new System.IO.MemoryStream())
            {
                serializer.WriteObject(ms, this);
                ms.Position = 0;
                return (MiniProfiler)serializer.ReadObject(ms);
            }
        }

        /// <summary>
        /// The step implementation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="level">The level.</param>
        /// <returns>the step.</returns>
        internal IDisposable StepImpl(string name, ProfileLevel level = ProfileLevel.Info)
        {
            if (level > Level) return null;
            return new Timing(this, Head, name);
        }

        /// <summary>
        /// The ignore implementation.
        /// </summary>
        /// <returns>the step.</returns>
        internal IDisposable IgnoreImpl()
        {
            return new Suppression(this);
        }

        /// <summary>
        /// The stop implementation
        /// </summary>
        /// <returns>true if the profile is stopped.</returns>
        internal bool StopImpl()
        {
            if (!_sw.IsRunning)
                return false;

            _sw.Stop();
            foreach (var timing in GetTimingHierarchy()) timing.Stop();

            return true;
        }

        /// <summary>
        /// add the data implementation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        internal void AddDataImpl(string key, string value)
        {
            if (Head == null)
                return;

            Head.AddKeyValue(key, value);
        }

        /// <summary>
        /// Returns milliseconds based on Stopwatch's Frequency.
        /// </summary>
        /// <param name="stopwatchElapsedTicks">The stopwatch Elapsed Ticks.</param>
        /// <returns>a decimal containing the milliseconds</returns>
        internal decimal GetRoundedMilliseconds(long stopwatchElapsedTicks)
        {
            long z = 10000 * stopwatchElapsedTicks;
            decimal timesTen = (int)(z / _sw.Frequency);
            return timesTen / 10;
        }

        /// <summary>
        /// The on <c>deserialized</c> event.
        /// </summary>
        /// <param name="context">The context.</param>
        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            HasSqlTimings = GetTimingHierarchy().Any(t => t.HasSqlTimings);
            HasDuplicateSqlTimings = GetTimingHierarchy().Any(t => t.HasDuplicateSqlTimings);
            if (_root != null)
            {
                _root.RebuildParentTimings();
            }
        }
    }
}