using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Web.Script.Serialization;
using MvcMiniProfiler.Helpers;

namespace MvcMiniProfiler
{
    /// <summary>
    /// A single MiniProfiler can be used to represent any number of steps/levels in a call-graph, via Step()
    /// </summary>
    /// <remarks>Totally baller.</remarks>
    [DataContract]
    public partial class MiniProfiler
    {

        /// <summary>
        /// Identifies this Profiler so it may be stored/cached.
        /// </summary>
        [DataMember(Order = 1)]
        public Guid Id { get; set; }

        /// <summary>
        /// A display name for this profiling session.
        /// </summary>
        [DataMember(Order = 2)]
        public string Name { get; set; }

        /// <summary>
        /// When this profiler was instantiated.
        /// </summary>
        [DataMember(Order = 3)]
        public DateTime Started { get; set; }

        /// <summary>
        /// Where this profiler was run.
        /// </summary>
        [DataMember(Order = 4)]
        public string MachineName { get; set; }

        /// <summary>
        /// Allows filtering of <see cref="Timing"/> steps based on what <see cref="ProfileLevel"/> 
        /// the steps are created with.
        /// </summary>
        [DataMember(Order = 5)]
        public ProfileLevel Level { get; set; }

        /// <summary>
        /// A string identifying the user/client that is profiling this request.  Set <see cref="MiniProfiler.Settings.UserProvider"/>
        /// with an <see cref="IUserProvider"/>-implementing class to provide a custom value.
        /// </summary>
        /// <remarks>
        /// If this is not set manually at some point, 
        /// </remarks>
        [DataMember(Order = 6)]
        public string User { get; set; }


        private Timing _root;
        /// <summary>
        /// The first <see cref="Timing"/> that is created and started when this profiler is instantiated.
        /// All other <see cref="Timing"/>s will be children of this one.
        /// </summary>
        [DataMember(Order = 6)]
        public Timing Root
        {
            get { return _root; }
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
        /// Contains information about queries executed during this profiling session.
        /// </summary>
        internal SqlProfiler SqlProfiler { get; private set; }

        /// <summary>
        /// Starts when this profiler is instantiated. Each <see cref="Timing"/> step will use this Stopwatch's current ticks as
        /// their starting time.
        /// </summary>
        private readonly Stopwatch _watch;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, int> _sqlCounts;

        /// <summary>
        /// Milliseconds, to one decimal place, that this MiniProfiler ran.
        /// </summary>
        public decimal DurationMilliseconds
        {
            get { return _root.DurationMilliseconds ?? GetRoundedMilliseconds(ElapsedTicks); }
        }

        /// <summary>
        /// Milliseconds, to one decimal place, that this MiniProfiler was executing sql.
        /// </summary>
        public decimal DurationMillisecondsInSql
        {
            get { return GetTimingHierarchy().Sum(t => t.HasSqlTimings ? t.SqlTimings.Sum(s => s.DurationMilliseconds) : 0); }
        }


        /// <summary>
        /// Returns true when we have profiled queries.
        /// </summary>
        public bool HasSqlTimings { get; set; }

        /// <summary>
        /// Returns true when any child Timings have duplicate queries.
        /// </summary>
        public bool HasDuplicateSqlTimings { get; set; }

        /// <summary>
        /// Returns true when <see cref="Root"/> or any of its <see cref="Timing.Children"/> are <see cref="Timing.IsTrivial"/>.
        /// </summary>
        public bool HasTrivialTimings
        {
            get
            {
                foreach (var t in GetTimingHierarchy())
                {
                    if (t.IsTrivial)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true when all child <see cref="Timing"/>s are <see cref="Timing.IsTrivial"/>.
        /// </summary>
        public bool HasAllTrivialTimings
        {
            get
            {
                foreach (var t in GetTimingHierarchy())
                {
                    if (!t.IsTrivial)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Any Timing step with a duration less than or equal to this will be hidden by default in the UI; defaults to 2.0 ms.
        /// </summary>
        public decimal TrivialDurationThresholdMilliseconds
        {
            get { return Settings.TrivialDurationThresholdMilliseconds; }
        }

        /// <summary>
        /// Ticks since this MiniProfiler was started.
        /// </summary>
        internal long ElapsedTicks { get { return _watch.ElapsedTicks; } }

        /// <summary>
        /// Points to the currently executing Timing.
        /// </summary>
        internal Timing Head { get; set; }


        /// <summary>
        /// Creates and starts a new MiniProfiler for the root <paramref name="url"/>, filtering <see cref="Timing"/> steps to <paramref name="level"/>.
        /// </summary>
        public MiniProfiler(string url, ProfileLevel level = ProfileLevel.Info)
        {
            Id = Guid.NewGuid();
            Level = level;
            SqlProfiler = new SqlProfiler(this);
            MachineName = Environment.MachineName;
            _sqlCounts = new Dictionary<string, int>();

            Started = DateTime.UtcNow;

            // stopwatch must start before any child Timings are instantiated
            _watch = Stopwatch.StartNew();
            Root = new Timing(this, parent: null, name: url);
        }

        /// <summary>
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public MiniProfiler()
        {
        }

        static MiniProfiler()
        {
            UI.MiniProfilerHandler.RegisterRoutes();
        }


        internal IDisposable StepImpl(string name, ProfileLevel level = ProfileLevel.Info)
        {
            if (level > this.Level) return null;
            return new Timing(this, Head, name);
        }

        internal bool StopImpl()
        {
            if (!_watch.IsRunning)
                return false;

            _watch.Stop();
            foreach (var timing in GetTimingHierarchy()) timing.Stop();

            return true;
        }

        internal void AddDataImpl(string key, string value)
        {
            Head.AddKeyValue(key, value);
        }

        internal void AddSqlTiming(SqlTiming stats)
        {
            int count;

            stats.IsDuplicate = _sqlCounts.TryGetValue(stats.RawCommandString, out count);
            _sqlCounts[stats.RawCommandString] = count + 1;

            HasSqlTimings = true;
            if (stats.IsDuplicate)
            {
                HasDuplicateSqlTimings = true;
            }

            Head.AddSqlTiming(stats);
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
        /// Returns all <see cref="SqlTiming"/> results contained in all child <see cref="Timing"/> steps.
        /// </summary>
        public List<SqlTiming> GetSqlTimings()
        {
            return GetTimingHierarchy().Where(t => t.HasSqlTimings).SelectMany(t => t.SqlTimings).ToList();
        }

        /// <summary>
        /// Returns milliseconds based on Stopwatch's Frequency.
        /// </summary>
        internal static decimal GetRoundedMilliseconds(long stopwatchElapsedTicks)
        {
            long z = 10000 * stopwatchElapsedTicks;
            decimal msTimesTen = (int)(z / Stopwatch.Frequency);
            return msTimesTen / 10;
        }

        /// <summary>
        /// Starts a new MiniProfiler for the current Request. This new profiler can be accessed by
        /// <see cref="MiniProfiler.Current"/>
        /// </summary>
        public static MiniProfiler Start(ProfileLevel level = ProfileLevel.Info)
        {
            var context = HttpContext.Current;
            if (context == null) return null;

            var url = context.Request.Url;
            var path = context.Request.AppRelativeCurrentExecutionFilePath.Substring(1);

            // don't profile /content or /scripts, either - happens in web.dev
            foreach (var ignored in Settings.IgnoredRootPaths ?? new string[0])
            {
                if (path.StartsWith(ignored, StringComparison.OrdinalIgnoreCase))
                    return null;

                var routePath = (MiniProfiler.Settings.RouteBasePath ?? "").Replace("~", "").RemoveTrailingSlash();
                if (path.StartsWith(routePath + ignored, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            var result = new MiniProfiler(url.OriginalString, level);
            Current = result;

            return result;
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
            var context = HttpContext.Current;
            if (context == null)
                return;

            var current = Current;
            if (current == null)
                return;

            // stop our timings - when this is false, we've already called .Stop before on this session
            if (!current.StopImpl())
                return;

            if (discardResults)
            {
                Current = null;
                return;
            }

            var request = context.Request;
            var response = context.Response;

            // set the profiler name to Controller/Action or /url
            EnsureName(current, request);

            // set the user identity of who is profiling this request
            EnsureUser(current, request);

            try
            {
                // allow profiling of ajax requests
                response.AppendHeader("X-MiniProfiler-Id", current.Id.ToString());
            }
            catch { } // headers blew up

            // because we fetch profiler results after the page loads, we have to put them somewhere in the meantime
            Settings.EnsureStorageStrategy();
            Settings.Storage.SaveMiniProfiler(current);
        }

        /// <summary>
        /// Makes sure 'profiler' has a Name, pulling it from route data or url.
        /// </summary>
        private static void EnsureName(MiniProfiler profiler, HttpRequest request)
        {
            // also set the profiler name to Controller/Action or /url
            if (string.IsNullOrWhiteSpace(profiler.Name))
            {
                var rc = request.RequestContext;
                RouteValueDictionary values;

                if (rc != null && rc.RouteData != null && (values = rc.RouteData.Values).Count > 0)
                {
                    var controller = values["Controller"];
                    var action = values["Action"];

                    if (controller != null && action != null)
                        profiler.Name = controller.ToString() + "/" + action.ToString();
                }

                if (string.IsNullOrWhiteSpace(profiler.Name))
                {
                    profiler.Name = request.Url.AbsolutePath ?? "";
                    if (profiler.Name.Length > 50)
                        profiler.Name = profiler.Name.Remove(50);
                }
            }
        }

        /// <summary>
        /// Ensures that there's a <see cref="MiniProfiler.User"/> identity set on the parameter profiler.
        /// </summary>
        private static void EnsureUser(MiniProfiler profiler, HttpRequest request)
        {
            if (profiler.User.HasValue()) return;

            profiler.User = (Settings.UserProvider ?? new IpAddressIdentity()).GetUser(request);
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal. Use this method when you
        /// do not wish to include the MvcMiniProfiler namespace for the <see cref="MiniProfilerExtensions.Step"/> extension method.
        /// </summary>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start"/> is called.</param>
        public static IDisposable StepStatic(string name, ProfileLevel level = ProfileLevel.Info)
        {
            return MiniProfilerExtensions.Step(Current, name, level);
        }

        /// <summary>
        /// Returns the css and javascript includes needed to display the MiniProfiler results UI.
        /// </summary>
        /// <param name="position">Which side of the page the profiler popup button should be displayed on (defaults to left)</param>
        /// <param name="showTrivial">Whether to show trivial timings by default (defaults to false)</param>
        /// <param name="showTimeWithChildren">Whether to show time the time with children column by default (defaults to false)</param>
        /// <param name="maxTracesToShow">The maximum number of trace popups to show before removing the oldest (defaults to 15)</param>
        /// <returns>Script and link elements normally; an empty string when there is no active profiling session.</returns>
        public static IHtmlString RenderIncludes(RenderPosition? position = null, bool? showTrivial = null, bool? showTimeWithChildren = null, int? maxTracesToShow = null)
        {
            return UI.MiniProfilerHandler.RenderIncludes(Current, position, showTrivial, showTimeWithChildren, maxTracesToShow);
        }

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start"/>ed.
        /// </summary>
        public static MiniProfiler Current
        {
            get
            {
                var context = HttpContext.Current;
                if (context == null) return null;

                return context.Items[CacheKey] as MiniProfiler;
            }
            private set
            {
                var context = HttpContext.Current;
                if (context == null) return;

                context.Items[CacheKey] = value;
            }
        }

        private const string CacheKey = ":mini-profiler:";

        /// <summary>
        /// Renders the current <see cref="MiniProfiler"/> to json.
        /// </summary>
        public static string ToJson()
        {
            return ToJson(MiniProfiler.Current);
        }

        /// <summary>
        /// Renders the parameter <see cref="MiniProfiler"/> to json.
        /// </summary>
        public static string ToJson(MiniProfiler profiler)
        {
            if (profiler == null) return null;

            var result = new JavaScriptSerializer().Serialize(profiler);
            return result;
        }

        /// <summary>
        /// Deserializes the json string parameter to a <see cref="MiniProfiler"/>.
        /// </summary>
        public static MiniProfiler FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var result = new JavaScriptSerializer().Deserialize<MiniProfiler>(json);
            return result;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext ctx)
        {
            HasSqlTimings = GetTimingHierarchy().Any(t => t.HasSqlTimings);
            HasDuplicateSqlTimings = GetTimingHierarchy().Any(t => t.HasDuplicateSqlTimings);
            if (_root != null)
            {
                _root.RebuildParentTimings();
            }
        }

        /// <summary>
        /// Create a DEEP clone of this object
        /// </summary>
        /// <returns></returns>
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
    }

    /// <summary>
    /// Categorizes individual <see cref="Timing"/> steps to allow filtering.
    /// </summary>
    public enum ProfileLevel : byte
    {
        /// <summary>
        /// Default level given to Timings.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Useful when profiling many items in a loop, but you don't wish to always see this detail.
        /// </summary>
        Verbose = 1
    }

    /// <summary>
    /// Dictates on which side of the page the profiler popup button is displayed; defaults to left.
    /// </summary>
    public enum RenderPosition
    {
        /// <summary>
        /// Profiler popup button is displayed on the left.
        /// </summary>
        Left = 0,

        /// <summary>
        /// Profiler popup button is displayed on the right.
        /// </summary>
        Right = 1
    }

    /// <summary>
    /// Contains helper methods that ease working with null <see cref="MiniProfiler"/>s.
    /// </summary>
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Wraps <paramref name="selector"/> in a <see cref="Step"/> call and executes it, returning its result.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="selector">Method to execute and profile.</param>
        /// <param name="name">The <see cref="Timing"/> step name used to label the profiler results.</param>
        /// <returns></returns>
        public static T Inline<T>(this MiniProfiler profiler, Func<T> selector, string name)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            if (profiler == null) return selector();
            using (profiler.StepImpl(name))
            {
                return selector();
            }
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start"/> is called.</param>
        public static IDisposable Step(this MiniProfiler profiler, string name, ProfileLevel level = ProfileLevel.Info)
        {
            return profiler == null ? null : profiler.StepImpl(name, level);
        }

        // TODO: get this working in the UI
        //public static void AddData(this MiniProfiler profiler, string key, string value)
        //{
        //    if (profiler != null) profiler.AddDataImpl(key, value);
        //}

        /// <summary>
        /// Adds <paramref name="externalProfiler"/>'s <see cref="Timing"/> hierarchy to this profiler's current Timing step,
        /// allowing other threads, remote calls, etc. to be profiled and joined into this profiling session.
        /// </summary>
        public static void AddProfilerResults(this MiniProfiler profiler, MiniProfiler externalProfiler)
        {
            if (profiler == null || externalProfiler == null) return;
            profiler.Head.AddChild(externalProfiler.Root);
        }

        /// <summary>
        /// Returns an html-encoded string with a text-representation of <paramref name="profiler"/>; returns "" when profiler is null.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        public static IHtmlString Render(this MiniProfiler profiler)
        {
            if (profiler == null) return new HtmlString("");

            var text = new StringBuilder()
                .Append(HttpUtility.HtmlEncode(Environment.MachineName)).Append(" at ").Append(DateTime.UtcNow).AppendLine();

            Stack<Timing> timings = new Stack<Timing>();
            timings.Push(profiler.Root);
            while (timings.Count > 0)
            {
                var timing = timings.Pop();
                string name = HttpUtility.HtmlEncode(timing.Name);
                text.AppendFormat("{2} {0} = {1:###,##0.##}ms", name, timing.DurationMilliseconds, new string('>', timing.Depth)).AppendLine();
                if (timing.HasChildren)
                {
                    IList<Timing> children = timing.Children;
                    for (int i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                }
            }
            return new HtmlString(text.ToString());
        }

    }
}