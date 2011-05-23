using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ProtoBuf;

namespace Profiling
{
    [ProtoContract]
    public class Timing : IDisposable
    {
        /// <summary>
        /// Text displayed when this Timing is rendered.
        /// </summary>
        [ProtoMember(1)]
        public string Name { get; private set; }

        /// <summary>
        /// How long this Timing step took in ms; includes any <see cref="Children"/> Timings' durations.
        /// </summary>
        [ProtoMember(2)]
        public double? DurationMilliseconds { get; private set; }

        /// <summary>
        /// All sub-steps that occur within this Timing step. Add new children through <see cref="AddChild"/>
        /// </summary>
        [ProtoMember(3)]
        public List<Timing> Children { get; private set; }

        /// <summary>
        /// Stores arbitrary key/value strings on this Timing step. Add new tuples through <see cref="AddKeyValue"/>.
        /// </summary>
        [ProtoMember(4)]
        public Dictionary<string, string> KeyValues { get; private set; }

        /// <summary>
        /// Any queries that occurred during this Timing step.
        /// </summary>
        [ProtoMember(5)]
        public List<SqlTiming> SqlTimings { get; set; }

        /// <summary>
        /// Which Timing this Timing is under - the duration that this step takes will be added to its parent's duration.
        /// </summary>
        /// <remarks>This will be null for the root (initial) Timing.</remarks>
        public Timing Parent { get; internal set; }

        /// <summary>
        /// Reference to the containing profiler, allowing this Timing to affect the Head and get Stopwatch readings.
        /// </summary>
        private readonly MiniProfiler _profiler;

        /// <summary>
        /// Offset from parent MiniProfiler's creation that this Timing was created.
        /// </summary>
        private readonly long _startTicks;

        /// <summary>
        /// Returns true when this Timing has inner Timing steps.
        /// </summary>
        public bool HasChildren
        {
            get { return Children != null && Children.Count > 0; }
        }

        public bool HasSqlTimings
        {
            get { return SqlTimings != null && SqlTimings.Count > 0; }
        }

        public bool IsRoot
        {
            get { return Parent == null; }
        }

        /// <summary>
        /// How far away this Timing is from the Profiler's Root.
        /// </summary>
        public int Depth
        {
            get
            {
                int result = 0;
                var parent = Parent;

                while (parent != null)
                {
                    parent = parent.Parent;
                    result++;
                }

                return result;
            }
        }

        public Timing(MiniProfiler profiler, Timing parent, string name)
        {
            _startTicks = profiler.ElapsedTicks;

            _profiler = profiler;
            _profiler.Head = this;

            if (parent != null) // root will have no parent
            {
                Parent = parent;
                Parent.AddChild(this);
            }

            Name = name;
        }

        [Obsolete("Used for serialization")]
        public Timing()
        {
        }


        public void AddKeyValue(string key, string value)
        {
            if (KeyValues == null)
                KeyValues = new Dictionary<string, string>();

            KeyValues[key] = value;
        }

        public void Stop()
        {
            if (DurationMilliseconds == null)
            {
                DurationMilliseconds = MiniProfiler.GetRoundedMilliseconds(_profiler.ElapsedTicks - _startTicks);
                _profiler.Head = Parent;
            }
        }

        void IDisposable.Dispose()
        {
            Stop();
        }

        internal void AddChild(Timing timing)
        {
            if (Children == null)
                Children = new List<Timing>();

            Children.Add(timing);
        }

        internal void AddSqlTiming(SqlTiming stat)
        {
            if (SqlTimings == null)
                SqlTimings = new List<SqlTiming>();

            SqlTimings.Add(stat);
        }
    }
}