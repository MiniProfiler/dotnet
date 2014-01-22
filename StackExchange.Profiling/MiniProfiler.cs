using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Script.Serialization;
using StackExchange.Profiling.Helpers;

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
        /// Initialises a new instance of the <see cref="MiniProfiler"/> class. 
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public MiniProfiler()
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MiniProfiler"/> class.  Creates and starts a new MiniProfiler 
        /// for the root <paramref name="url"/>, filtering <see cref="Timing"/> steps to <paramref name="level"/>.
        /// </summary>
        public MiniProfiler(string url, ProfileLevel level = ProfileLevel.Info)
        {
            Id = Guid.NewGuid();
#pragma warning disable 612,618
            Level = level;
#pragma warning restore 612,618
            SqlProfiler = new SqlProfiler(this);
            MachineName = Environment.MachineName;
            Started = DateTime.UtcNow;

            // stopwatch must start before any child Timings are instantiated
            _sw = Settings.StopwatchProvider();
            Root = new Timing(this, null, url);
        }

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
        /// Json used to store Custom Links
        /// </summary>
        [ScriptIgnore]
        public string CustomLinksJson {
            get { return CustomLinks != null ? CustomLinks.ToJson() : null; } 
            set {
                if (value.HasValue())
                {
                    CustomLinks = value.FromJson<Dictionary<string, string>>();
                }
            } 
        }
        
            /// <summary>
        /// Gets or sets the root timing.
        /// The first <see cref="Timing"/> that is created and started when this profiler is instantiated.
        /// All other <see cref="Timing"/>s will be children of this one.
        /// </summary>
        [DataMember(Order = 7)]
        public Timing Root
        {
            get
            {
                return _root;
            }
            set
            {
                _root = value;
                RootTimingId = value.Id;

                // TODO: remove this shit

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
        /// Id of Root Timing. Used for Sql Storage purposes.
        /// </summary>
        [ScriptIgnore]
        public Guid? RootTimingId { get; set; }

        /// <summary>
        /// Gets or sets timings collected from the client
        /// </summary>
        [DataMember(Order = 8)]
        public ClientTimings ClientTimings { get; set; }

        /// <summary>
        /// RedirectCount in ClientTimings. Used for sql storage.
        /// </summary>
        [ScriptIgnore]
        public int? ClientTimingsRedirectCount { get; set; }

        /// <summary>
        /// Gets or sets a string identifying the user/client that is profiling this request.
        /// </summary>
        /// <remarks>
        /// If this is not set manually at some point, the <see cref="IUserProvider"/> implementation will be used;
        /// by default, this will be the current request's IP address.
        /// </remarks>
        [DataMember(Order = 9)]
        public string User { get; set; }

        /// <summary>
        /// Returns true when this MiniProfiler has been viewed by the <see cref="User"/> that recorded it.
        /// </summary>
        /// <remarks>
        /// Allows POSTs that result in a redirect to be profiled. <see cref="MiniProfiler.Settings.Storage"/> implementation
        /// will keep a list of all profilers that haven't been fetched down.
        /// </remarks>
        [DataMember(Order = 10)]
        [ScriptIgnore]
        public bool HasUserViewed { get; set; }

        /// <summary>
        /// Gets or sets whether or not filtering is allowed of <see cref="Timing"/> steps based on what <see cref="ProfileLevel"/> 
        /// the steps are created with.
        /// </summary>
        [Obsolete("If you don't want this removed, speak up at https://github.com/MiniProfiler/dotnet")]
        [ScriptIgnore]
        public ProfileLevel Level { get; set; }

        /// <summary>
        /// Gets or sets points to the currently executing Timing. 
        /// </summary>
        [ScriptIgnore]
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
        /// Starts a new MiniProfiler based on the current <see cref="IProfilerProvider"/>. This new profiler can be accessed by
        /// <see cref="MiniProfiler.Current"/>.
        /// </summary>
        /// <param name="level">Profiling level. Default to Info.</param>
        /// <param name="sessionName">
        /// Allows explicit naming of the new profiling session; when null, an appropriate default will be used, e.g. for
        /// a web request, the url will be used for the overall session name.
        /// </param>
        public static MiniProfiler Start(ProfileLevel level = ProfileLevel.Info, string sessionName = null)
        {
            Settings.EnsureProfilerProvider();
            return Settings.ProfilerProvider.Start(level, sessionName);
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
        public static string ToJson()
        {
            return ToJson(Current);
        }

        /// <summary>
        /// Renders the parameter <see cref="MiniProfiler"/> to JSON.
        /// </summary>
        public static string ToJson(MiniProfiler profiler)
        {
            return profiler == null ? null : GetJsonSerializer().Serialize(profiler);
        }

        /// <summary>
        /// Deserializes the JSON string parameter to a <see cref="MiniProfiler"/>.
        /// </summary>
        public static MiniProfiler FromJson(string json)
        {
            return json.HasValue() ? GetJsonSerializer().Deserialize<MiniProfiler>(json) : null;
        }

        private static JavaScriptSerializer GetJsonSerializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = Settings.MaxJsonResponseSize };   
        }

        /// <summary>
        /// Returns the <c>css</c> and <c>javascript</c> includes needed to display the MiniProfiler results UI.
        /// </summary>
        /// <param name="position">Which side of the page the profiler popup button should be displayed on (defaults to left)</param>
        /// <param name="showTrivial">Whether to show trivial timings by default (defaults to false)</param>
        /// <param name="showTimeWithChildren">Whether to show time the time with children column by default (defaults to false)</param>
        /// <param name="maxTracesToShow">The maximum number of trace popups to show before removing the oldest (defaults to 15)</param>
        /// <param name="showControls">when true, shows buttons to minimize and clear MiniProfiler results</param>
        /// <param name="useExistingjQuery">
        /// Should MiniProfiler attempt to load its own version of jQuery, or rely on a version previously loaded on the page?
        /// </param>
        /// <param name="samplingOnly">The sampling Only.</param>
        /// <param name="startHidden">Should the profiler start as hidden. Default to null.</param>
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
            return MiniProfilerHandler.RenderIncludes(
                Current, 
                position, 
                showTrivial, 
                showTimeWithChildren, 
                maxTracesToShow, 
                showControls, 
                startHidden);
        }

        /// <summary>
        /// Returns the <see cref="Root"/>'s <see cref="Timing.Name"/> and <see cref="DurationMilliseconds"/> this profiler recorded.
        /// </summary>
        /// <returns>a string containing the recording information</returns>
        public override string ToString()
        {
            return Root != null ? Root.Name + " (" + DurationMilliseconds + " ms)" : "";
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object other)
        {
            return other is MiniProfiler && Id.Equals(((MiniProfiler)other).Id);
        }

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

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

        /// <summary>
        /// Create a DEEP clone of this MiniProfiler.
        /// </summary>
        public MiniProfiler Clone()
        {
            var serializer = new DataContractSerializer(typeof(MiniProfiler), null, int.MaxValue, false, true, null);
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                ms.Position = 0;
                return (MiniProfiler)serializer.ReadObject(ms);
            }
        }

        internal IDisposable StepImpl(string name, ProfileLevel level = ProfileLevel.Info)
        {
            return level > Level ? null : new Timing(this, Head, name);
        }

        internal IDisposable IgnoreImpl()
        {
            return new Suppression(this);
        }

        internal bool StopImpl()
        {
            if (!_sw.IsRunning)
                return false;

            _sw.Stop();
            DurationMilliseconds = GetRoundedMilliseconds(ElapsedTicks);

            foreach (var timing in GetTimingHierarchy())
            {
                timing.Stop();
            }

            return true;
        }

        /// <summary>
        /// Returns milliseconds based on Stopwatch's Frequency, rounded to one decimal place.
        /// </summary>
        internal decimal GetRoundedMilliseconds(long ticks)
        {
            long z = 10000 * ticks;
            decimal timesTen = (int)(z / _sw.Frequency);
            return timesTen / 10;
        }

        /// <summary>
        /// Returns how many milliseconds have elapsed since <paramref name="startTicks"/> was recorded.
        /// </summary>
        internal decimal GetDurationMilliseconds(long startTicks)
        {
            return GetRoundedMilliseconds(ElapsedTicks - startTicks);
        }

        /// <summary>
        /// Called immediately after deserialization.
        /// </summary>
        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            if (_root != null)
            {
                //_root.RebuildParentTimings();
            }
        }
    }
}