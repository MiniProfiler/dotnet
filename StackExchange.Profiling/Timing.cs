namespace StackExchange.Profiling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Web.Script.Serialization;

    using StackExchange.Profiling.Data;

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

        /// <summary>
        /// Gets or sets the parent timing.
        /// </summary>
        private Timing _parentTiming;

        /// <summary>
        /// Initialises a new instance of the <see cref="Timing"/> class. 
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public Timing()
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="Timing"/> class. 
        /// Creates a new Timing named 'name' in the 'profiler's session, with 'parent' as this Timing's immediate ancestor.
        /// </summary>
        /// <param name="profiler">
        /// The profiler.
        /// </param>
        /// <param name="parent">
        /// The parent.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        public Timing(MiniProfiler profiler, Timing parent, string name)
        {
            this.Id = Guid.NewGuid();
            this.Profiler = profiler;
            this.Profiler.Head = this;

            if (parent != null)
            {
                // root will have no parent
                parent.AddChild(this);
            }

            this.Name = name;

            this._startTicks = profiler.ElapsedTicks;
            this.StartMilliseconds = profiler.GetRoundedMilliseconds(this._startTicks);
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
        /// Gets or sets Stores arbitrary key/value strings on this Timing step. Add new tuples through <see cref="AddKeyValue"/>.
        /// </summary>
        [DataMember(Order = 6)]
        public Dictionary<string, string> KeyValues { get; set; }

        /// <summary>
        /// Gets or sets Any queries that occurred during this Timing step.
        /// </summary>
        [DataMember(Order = 7)]
        public List<SqlTiming> SqlTimings { get; set; }

        /// <summary>
        /// Gets or sets Needed for database deserialization and JSON serialization.
        /// </summary>
        public Guid? ParentTimingId { get; set; }

        /// <summary>
        /// Gets or sets Which Timing this Timing is under - the duration that this step takes will be added to its parent's duration.
        /// </summary>
        /// <remarks>This will be null for the root (initial) Timing.</remarks>
        [ScriptIgnore]
        public Timing ParentTiming
        {
            get
            {
                return this._parentTiming;
            }
            
            set
            {
                this._parentTiming = value;

                if (value != null && this.ParentTimingId != value.Id)
                    this.ParentTimingId = value.Id;
            }
        }

        /// <summary>
        /// Gets the elapsed milliseconds in this step without any children's durations.
        /// </summary>
        public decimal DurationWithoutChildrenMilliseconds
        {
            get
            {
                var result = this.DurationMilliseconds.GetValueOrDefault();

                if (this.HasChildren)
                {
                    foreach (var child in this.Children)
                    {
                        result -= child.DurationMilliseconds.GetValueOrDefault();
                    }
                }

                return Math.Round(result, 1);
            }
        }

        /// <summary>
        /// Gets the aggregate elapsed milliseconds of all <c>SqlTimings</c> executed in this Timing, excluding Children Timings.
        /// </summary>
        public decimal SqlTimingsDurationMilliseconds
        {
            get { return this.HasSqlTimings ? Math.Round(this.SqlTimings.Sum(s => s.DurationMilliseconds), 1) : 0; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="DurationWithoutChildrenMilliseconds"/> is less than the configured
        /// <see cref="MiniProfiler.Settings.TrivialDurationThresholdMilliseconds"/>, by default 2.0 ms.
        /// </summary>
        public bool IsTrivial
        {
            get { return this.DurationWithoutChildrenMilliseconds <= MiniProfiler.Settings.TrivialDurationThresholdMilliseconds; }
        }

        /// <summary>
        /// Gets a value indicating whether this Timing has inner Timing steps.
        /// </summary>
        public bool HasChildren
        {
            get { return this.Children != null && this.Children.Count > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether this Timing step collected SQL execution timings.
        /// </summary>
        public bool HasSqlTimings
        {
            get { return this.SqlTimings != null && this.SqlTimings.Count > 0; }
        }

        /// <summary>
        /// Gets a value indicating whether this has duplicate SQL timings.
        /// Returns true if any <see cref="SqlTiming"/>s executed in this step are detected as duplicate statements.
        /// </summary>
        public bool HasDuplicateSqlTimings
        {
            get { return this.HasSqlTimings && this.SqlTimings.Any(s => s.IsDuplicate); }
        }

        /// <summary>
        /// Gets a value indicating whether this Timing is the first one created in a MiniProfiler session.
        /// </summary>
        public bool IsRoot
        {
            get { return this.ParentTiming == null; }
        }

        /// <summary>
        /// Gets a value indicating whether how far away this Timing is from the Profiler's Root.
        /// </summary>
        public short Depth
        {
            get
            {
                short result = 0;
                var parent = this.ParentTiming;

                while (parent != null)
                {
                    parent = parent.ParentTiming;
                    result++;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets how many SQL data readers were executed in this Timing step. Does not include queries in any child Timings.
        /// </summary>
        public int ExecutedReaders
        {
            get { return this.GetExecutedCount(ExecuteType.Reader); }
        }

        /// <summary>
        /// Gets how many SQL scalar queries were executed in this Timing step. Does not include queries in any child Timings.
        /// </summary>
        public int ExecutedScalars
        {
            get { return this.GetExecutedCount(ExecuteType.Scalar); }
        }

        /// <summary>
        /// Gets how many SQL non-query statements were executed in this Timing step. Does not include queries in any child Timings.
        /// </summary>
        public int ExecutedNonQueries
        {
            get { return this.GetExecutedCount(ExecuteType.NonQuery); }
        }

        /// <summary>
        /// Gets a reference to the containing profiler, allowing this Timing to affect the Head and get Stopwatch readings.
        /// </summary>
        internal MiniProfiler Profiler { get; private set; }

        /// <summary>
        /// Rebuilds all the parent timings on deserialization calls
        /// </summary>
        public void RebuildParentTimings()
        {
            if (this.SqlTimings != null)
            {
                foreach (var timing in this.SqlTimings)
                {
                    timing.ParentTiming = this;
                }
            }

            if (this.Children != null)
            {
                foreach (var child in this.Children)
                {
                    child.ParentTiming = this;
                    child.RebuildParentTimings();
                }
            }
        }

        /// <summary>
        /// Returns this Timing's Name.
        /// </summary>
        /// <returns>a string containing the name.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        /// <param name="rValue">The rValue.</param>
        /// <returns>true if the supplied value is the same as this.</returns>
        public override bool Equals(object rValue)
        {
            return rValue is Timing && this.Id.Equals(((Timing)rValue).Id);
        }

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        /// <returns>the hash code value.</returns>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        /// <summary>
        /// Adds arbitrary string 'value' under 'key', allowing custom properties to be stored in this Timing step.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddKeyValue(string key, string value)
        {
            if (this.KeyValues == null)
                this.KeyValues = new Dictionary<string, string>();

            this.KeyValues[key] = value;
        }

        /// <summary>
        /// Completes this Timing's duration and sets the MiniProfiler's Head up one level.
        /// </summary>
        public void Stop()
        {
            if (this.DurationMilliseconds == null)
            {
                this.DurationMilliseconds = this.Profiler.GetRoundedMilliseconds(this.Profiler.ElapsedTicks - this._startTicks);
                this.Profiler.Head = this.ParentTiming;
            }
        }

        /// <summary>
        /// dispose the profiler.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.Stop();
        }

        /// <summary>
        /// Add the parameter 'timing' to this Timing's Children collection.
        /// </summary>
        /// <param name="timing">The timing.</param>
        /// <remarks>Used outside this assembly for custom deserialization when creating an <see cref="Storage.IStorage"/> implementation.</remarks>
        public void AddChild(Timing timing)
        {
            if (this.Children == null)
                this.Children = new List<Timing>();

            this.Children.Add(timing);
            timing.ParentTiming = this;
        }

        /// <summary>
        /// Adds the parameter <c>sqlTiming</c> to this Timing's <c>SqlTimings</c> collection.
        /// </summary>
        /// <param name="sqlTiming">A SQL statement profiling that was executed in this Timing step.</param>
        /// <remarks>Used outside this assembly for custom deserialization when creating an <see cref="Storage.IStorage"/> implementation.</remarks>
        public void AddSqlTiming(SqlTiming sqlTiming)
        {
            if (this.SqlTimings == null)
                this.SqlTimings = new List<SqlTiming>();

            this.SqlTimings.Add(sqlTiming);
            sqlTiming.ParentTiming = this;
        }

        /// <summary>
        /// Returns the number of SQL statements of <paramref name="type"/> that were executed in this <see cref="Timing"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>the execution count.</returns>
        internal int GetExecutedCount(ExecuteType type)
        {
            return this.HasSqlTimings ? this.SqlTimings.Count(s => s.ExecuteType == type) : 0;
        }
    }
}