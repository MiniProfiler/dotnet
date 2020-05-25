using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using StackExchange.Profiling.Internal;

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
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="Timing"/> class. 
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public Timing() { /* serialization */ }

        /// <summary>
        /// Creates a new Timing named 'name' in the 'profiler's session, with 'parent' as this Timing's immediate ancestor.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> this <see cref="Timing"/> belongs to.</param>
        /// <param name="parent">The <see cref="Timing"/> this <see cref="Timing"/> is a child of.</param>
        /// <param name="name">The name of this timing.</param>
        /// <param name="minSaveMs">(Optional) The minimum threshold (in milliseconds) for saving this timing.</param>
        /// <param name="includeChildrenWithMinSave">(Optional) Whether the children are included when comparing to the <paramref name="minSaveMs"/> threshold.</param>
        public Timing(MiniProfiler profiler, Timing parent, string name, decimal? minSaveMs = null, bool? includeChildrenWithMinSave = false) :
            this(profiler, parent, name, minSaveMs, includeChildrenWithMinSave, 0)
        { }

        /// <summary>
        /// Creates a new Timing named 'name' in the 'profiler's session, with 'parent' as this Timing's immediate ancestor.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> this <see cref="Timing"/> belongs to.</param>
        /// <param name="parent">The <see cref="Timing"/> this <see cref="Timing"/> is a child of.</param>
        /// <param name="name">The name of this timing.</param>
        /// <param name="minSaveMs">(Optional) The minimum threshold (in milliseconds) for saving this timing.</param>
        /// <param name="includeChildrenWithMinSave">(Optional) Whether the children are included when comparing to the <paramref name="minSaveMs"/> threshold.</param>
        /// <param name="debugStackShave">The number of frames to shave off the debug stack.</param>
        public Timing(MiniProfiler profiler, Timing parent, string name, decimal? minSaveMs, bool? includeChildrenWithMinSave, int debugStackShave)
        {
            Id = Guid.NewGuid();
            Profiler = profiler;
            Profiler.Head = this;

            // root will have no parent
            // Also, due to stack unwinding for minimal frame depth in MVC and such, we may need to traverse up when the
            // AsyncLocal<Timing> head is not reset properly in the context we expect (it was reset lower down)
            while (parent?.DurationMilliseconds.HasValue == true)
            {
                parent = parent.ParentTiming;
            }
            parent?.AddChild(this);

            Name = name;

            _startTicks = profiler.ElapsedTicks;
            _minSaveMs = minSaveMs;
            _includeChildrenWithMinSave = includeChildrenWithMinSave == true;
            StartMilliseconds = profiler.GetRoundedMilliseconds(_startTicks);

            if (profiler.Options.EnableDebugMode)
            {
                DebugInfo = new TimingDebugInfo(this, debugStackShave);
            }
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

        private List<Timing> _children;

        /// <summary>
        /// Gets or sets All sub-steps that occur within this Timing step. Add new children through <see cref="AddChild"/>
        /// </summary>
        [DataMember(Order = 5)]
        public List<Timing> Children
        {
            get => _children;
            set
            {
                if (value?.Count > 0)
                {
                    lock (value)
                    {
                        foreach (var t in value)
                        {
                            t.ParentTiming = this;
                        }
                    }
                }
                _children = value;
            }
        }

        /// <summary>
        /// <see cref="CustomTiming"/> lists keyed by their type, e.g. "sql", "memcache", "redis", "http".
        /// </summary>
        [DataMember(Order = 6)]
        public Dictionary<string, List<CustomTiming>> CustomTimings { get; set; }

        /// <summary>
        /// Present only when <c>EnableDebugMode</c> is <c>true</c>, additional step info in-memory only.
        /// </summary>
        [DataMember(Order = 7)]
        public TimingDebugInfo DebugInfo { get; set; }

        /// <summary>
        /// JSON representing the Custom Timings associated with this timing.
        /// </summary>
        public string CustomTimingsJson {
            get => CustomTimings?.ToJson();
            set => CustomTimings = value.FromJson<Dictionary<string, List<CustomTiming>>>();
        }

        /// <summary>
        /// Returns true when there exists any <see cref="CustomTiming"/> objects in this <see cref="CustomTimings"/>.
        /// </summary>
        public bool HasCustomTimings => CustomTimings?.Values.Any(v => v?.Count > 0) ?? false;

        /// <summary>
        /// Gets or sets Which Timing this Timing is under - the duration that this step takes will be added to its parent's duration.
        /// </summary>
        /// <remarks>This will be null for the root (initial) Timing.</remarks>
        [IgnoreDataMember]
        public Timing ParentTiming { get; set; }

        /// <summary>
        /// The Unique Identifier identifying the parent timing of this Timing. Used for sql server storage.
        /// </summary>
        [IgnoreDataMember]
        public Guid ParentTimingId { get; set; }

        /// <summary>
        /// Gets the elapsed milliseconds in this step without any children's durations.
        /// </summary>
        [IgnoreDataMember]
        public decimal DurationWithoutChildrenMilliseconds
        {
            get
            {
                var result = DurationMilliseconds.GetValueOrDefault();

                if (Children != null)
                {
                    lock (_syncRoot)
                    {
                        foreach (var child in Children)
                        {
                            result -= child.DurationMilliseconds.GetValueOrDefault();
                        }
                    }
                }

                return Math.Round(result, 1);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="DurationMilliseconds"/> is less than the configured
        /// <see cref="MiniProfilerBaseOptions.TrivialDurationThresholdMilliseconds"/>, by default 2.0 ms.
        /// </summary>
        [IgnoreDataMember]
        public bool IsTrivial => DurationMilliseconds <= Profiler.Options.TrivialDurationThresholdMilliseconds;

        /// <summary>
        /// Gets a value indicating whether this Timing has inner Timing steps.
        /// </summary>
        [IgnoreDataMember]
        public bool HasChildren => Children?.Count > 0;

        /// <summary>
        /// Gets a value indicating whether this Timing is the first one created in a MiniProfiler session.
        /// </summary>
        [IgnoreDataMember]
        public bool IsRoot => Equals(Profiler.Root);

        /// <summary>
        /// Gets a value indicating whether how far away this Timing is from the Profiler's Root.
        /// </summary>
        [IgnoreDataMember]
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
        [IgnoreDataMember]
        public Guid MiniProfilerId { get; set; }

        /// <summary>
        /// Returns this Timing's Name.
        /// </summary>
        public override string ToString() => Name;

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare to.</param>
        public override bool Equals(object obj) => obj is Timing timing && Id.Equals(timing.Id);

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
        /// <param name="timing">The child <see cref="Timing"/> to add.</param>
        /// <remarks>
        /// Used outside this assembly for custom deserialization when creating an <see cref="Storage.IAsyncStorage"/> implementation.
        /// </remarks>
        public void AddChild(Timing timing)
        {
            lock (_syncRoot)
            {
                Children ??= new List<Timing>();
                Children.Add(timing);
            }
            timing.Profiler ??= Profiler;
            timing.ParentTiming = this;
            timing.ParentTimingId = Id;
            if (Profiler != null)
                timing.MiniProfilerId = Profiler.Id;
        }

        internal void RemoveChild(Timing timing)
        {
            lock (Children)
            {
                Children?.Remove(timing);
            }
        }

        /// <summary>
        /// Adds <paramref name="customTiming"/> to this <see cref="Timing"/> step's dictionary of 
        /// custom timings, <see cref="CustomTimings"/>.  Ensures that <see cref="CustomTimings"/> is created, 
        /// as well as the <paramref name="category"/>'s list.
        /// </summary>
        /// <param name="category">The kind of custom timing, e.g. "http", "redis", "memcache"</param>
        /// <param name="customTiming">Duration and command information</param>
        public void AddCustomTiming(string category, CustomTiming customTiming)
        {
            var ctl = GetCustomTimingList(category);
            lock (ctl)
            {
                ctl.Add(customTiming);
            }
        }

        internal void RemoveCustomTiming(string category, CustomTiming customTiming)
        {
            var ctl = GetCustomTimingList(category);
            lock (ctl)
            {
                ctl.Remove(customTiming);
            }
        }

        /// <summary>
        /// Returns the <see cref="CustomTiming"/> list keyed to the <paramref name="category"/>, creating any collections when null.
        /// </summary>
        /// <param name="category">The kind of custom timings, e.g. "sql", "redis", "memcache"</param>
        private List<CustomTiming> GetCustomTimingList(string category)
        {
            List<CustomTiming> result;
            if (CustomTimings == null)
            {
                lock (_syncRoot)
                {
                    // If null, create it to the single entry...no need to go further
                    if (CustomTimings == null)
                    {
                        result = new List<CustomTiming>();
                        CustomTimings = new Dictionary<string, List<CustomTiming>> { [category] = result };
                        return result;
                    }
                }
            }

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
