using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using StackExchange.Profiling.Helpers;
#if NET46
using System.Web.Script.Serialization;
#endif

namespace StackExchange.Profiling
{
    /// <summary>
    /// An individual profiling step that can contain child steps.
    /// </summary>
    [DataContract]
    public class Timing : IDisposable
    {
        /// <summary>
        /// Offset from parent MiniProfiler's creation that this Timing was created.
        /// </summary>
        private readonly long _startTicks;
        private readonly decimal? _minSaveMs;
        private readonly bool _includeChildrenWithMinSave;

        /// <summary>
        /// Initialises a new instance of the <see cref="Timing"/> class. 
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public Timing() { /* serialization */ }

        /// <summary>
        /// Creates a new Timing named 'name' in the 'profiler's session, with 'parent' as this Timing's immediate ancestor.
        /// </summary>
        public Timing(MiniProfiler profiler, Timing parent, string name, decimal? minSaveMs = null, bool? includeChildrenWithMinSave = false)
        {
            Id = Guid.NewGuid();
            Profiler = profiler;
            Profiler.Head = this;

            // root will have no parent
            parent?.AddChild(this);

            Name = name;

            _startTicks = profiler.ElapsedTicks;
            _minSaveMs = minSaveMs;
            _includeChildrenWithMinSave = includeChildrenWithMinSave == true;
            StartMilliseconds = profiler.GetRoundedMilliseconds(_startTicks);
        }

        /// <summary>
        /// Gets or sets Unique identifier for this timing; set during construction.
        /// </summary>
        [DataMember(Order = 1)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets Text displayed when this Timing is rendered.
        /// </summary>
        [DataMember(Order = 2)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets How long this Timing step took in ms; includes any <see cref="Children"/> Timings' durations.
        /// </summary>
        [DataMember(Order = 3)]
        public decimal? DurationMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets The offset from the start of profiling.
        /// </summary>
        [DataMember(Order = 4)]
        public decimal StartMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets All sub-steps that occur within this Timing step. Add new children through <see cref="AddChild"/>
        /// </summary>
        [DataMember(Order = 5)]
        public List<Timing> Children { get; set; }

        /// <summary>
        /// <see cref="CustomTiming"/> lists keyed by their type, e.g. "sql", "memcache", "redis", "http".
        /// </summary>
        [DataMember(Order = 6)]
        public Dictionary<string, List<CustomTiming>> CustomTimings { get; set; }

        /// <summary>
        /// JSON representing the Custom Timings associated with this timing.
        /// </summary>
        public string CustomTimingsJson {
            get { return CustomTimings?.ToJson(); }
            set { CustomTimings = value.FromJson<Dictionary<string, List<CustomTiming>>>(); }
        }

        /// <summary>
        /// Returns true when there exists any <see cref="CustomTiming"/> objects in this <see cref="CustomTimings"/>.
        /// </summary>
        public bool HasCustomTimings
        {
            get { return CustomTimings != null && CustomTimings.Any(pair => pair.Value?.Count > 0); }
        }

        /// <summary>
        /// Gets or sets Which Timing this Timing is under - the duration that this step takes will be added to its parent's duration.
        /// </summary>
        /// <remarks>This will be null for the root (initial) Timing.</remarks>
#if NET46
        [ScriptIgnore]
#endif
        public Timing ParentTiming { get; set; }

        /// <summary>
        /// The Unique Identifier identifying the parent timing of this Timing. Used for sql server storage.
        /// </summary>
#if NET46
        [ScriptIgnore]
#endif
        public Guid ParentTimingId { get; set; }

        /// <summary>
        /// Gets the elapsed milliseconds in this step without any children's durations.
        /// </summary>
#if NET46
        [ScriptIgnore]
#endif
        public decimal DurationWithoutChildrenMilliseconds
        {
            get
            {
                var result = DurationMilliseconds.GetValueOrDefault();

                if (HasChildren)
                {
                    foreach (var child in Children)
                    {
                        result -= child.DurationMilliseconds.GetValueOrDefault();
                    }
                }

                return Math.Round(result, 1);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="DurationMilliseconds"/> is less than the configured
        /// <see cref="MiniProfiler.Settings.TrivialDurationThresholdMilliseconds"/>, by default 2.0 ms.
        /// </summary>
#if NET46
        [ScriptIgnore]
#endif
        public bool IsTrivial => DurationMilliseconds <= MiniProfiler.Settings.TrivialDurationThresholdMilliseconds;

        /// <summary>
        /// Gets a value indicating whether this Timing has inner Timing steps.
        /// </summary>
#if NET46
        [ScriptIgnore]
#endif
        public bool HasChildren => Children?.Count > 0;

        /// <summary>
        /// Gets a value indicating whether this Timing is the first one created in a MiniProfiler session.
        /// </summary>
#if NET46
        [ScriptIgnore]
#endif
        public bool IsRoot => Equals(Profiler.Root);

        /// <summary>
        /// Gets a value indicating whether how far away this Timing is from the Profiler's Root.
        /// </summary>
#if NET46
        [ScriptIgnore]
#endif
        public short Depth
        {
            get
            {
                short result = 0;
                var parent = ParentTiming;

                while (parent != null)
                {
                    parent = parent.ParentTiming;
                    result++;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets a reference to the containing profiler, allowing this Timing to affect the Head and get Stopwatch readings.
        /// </summary>
        internal MiniProfiler Profiler { get; set; }

        /// <summary>
        /// The unique identifier used to identify the Profiler with which this Timing is associated. Used for sql storage.
        /// </summary>
#if NET46
        [ScriptIgnore]
#endif
        public Guid MiniProfilerId { get; set; }

        /// <summary>
        /// Returns this Timing's Name.
        /// </summary>
        public override string ToString() => Name;

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object other)
        {
            return other is Timing && Id.Equals(((Timing)other).Id);
        }

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>
        /// Completes this Timing's duration and sets the MiniProfiler's Head up one level.
        /// </summary>
        public void Stop()
        {
            if (DurationMilliseconds != null) return;
            DurationMilliseconds = Profiler.GetDurationMilliseconds(_startTicks);
            Profiler.Head = ParentTiming;

            if (_minSaveMs.HasValue && _minSaveMs.Value > 0 && ParentTiming != null)
            {
                var compareMs = _includeChildrenWithMinSave ? DurationMilliseconds : DurationWithoutChildrenMilliseconds;
                if (compareMs < _minSaveMs.Value)
                {
                    ParentTiming.RemoveChild(this);
                }
            }
        }

        /// <summary>
        /// Stops profiling, allowing the <c>using</c> construct to neatly encapsulate a region to be profiled.
        /// </summary>
        void IDisposable.Dispose() => Stop();

        /// <summary>
        /// Add the parameter 'timing' to this Timing's Children collection.
        /// </summary>
        /// <remarks>
        /// Used outside this assembly for custom deserialization when creating an <see cref="Storage.IAsyncStorage"/> implementation.
        /// </remarks>
        public void AddChild(Timing timing)
        {
            if (Children == null)
                Children = new List<Timing>();

            Children.Add(timing);
            if(timing.Profiler == null)
                timing.Profiler = Profiler;
            timing.ParentTiming = this;
            timing.ParentTimingId = Id;
            if (Profiler != null)
                timing.MiniProfilerId = Profiler.Id;
        }

        internal void RemoveChild(Timing timing) => Children?.Remove(timing);

        /// <summary>
        /// Adds <paramref name="customTiming"/> to this <see cref="Timing"/> step's dictionary of 
        /// custom timings, <see cref="CustomTimings"/>.  Ensures that <see cref="CustomTimings"/> is created, 
        /// as well as the <paramref name="category"/>'s list.
        /// </summary>
        /// <param name="category">The kind of custom timing, e.g. "http", "redis", "memcache"</param>
        /// <param name="customTiming">Duration and command information</param>
        public void AddCustomTiming(string category, CustomTiming customTiming)
        {
            GetCustomTimingList(category).Add(customTiming);
        }

        internal void RemoveCustomTiming(string category, CustomTiming customTiming)
        {
            GetCustomTimingList(category).Remove(customTiming);
        }

        private readonly object _lockObject = new object();

        /// <summary>
        /// Returns the <see cref="CustomTiming"/> list keyed to the <paramref name="category"/>, creating any collections when null.
        /// </summary>
        /// <param name="category">The kind of custom timings, e.g. "sql", "redis", "memcache"</param>
        private List<CustomTiming> GetCustomTimingList(string category)
        {
            lock (_lockObject)
            {
                if (CustomTimings == null)
                    CustomTimings = new Dictionary<string, List<CustomTiming>>();
            }

            List<CustomTiming> result;
            lock (CustomTimings)
            {
                if (!CustomTimings.TryGetValue(category, out result))
                {
                    result = new List<CustomTiming>();
                    CustomTimings[category] = result;
                }
            }
            return result;
        }
    }
}