using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Profiling.Data;
using System.Runtime.Serialization;

namespace Profiling
{
    [DataContract]
    public class SqlTiming
    {
        [DataMember(Order = 0)]
        public ExecuteType ExecuteType { get; private set; }

        [DataMember(Order = 1)]
        public string CommandString { get; private set; }

        [DataMember(Order = 2)]
        public string StackTraceSnippet { get; private set; }

        [DataMember(Order = 3)]
        public double StartMilliseconds { get; private set; }

        [DataMember(Order = 4)]
        public double DurationMilliseconds { get; private set; }

        [DataMember(Order = 5)]
        public double FirstFetchDurationMilliseconds { get; private set; }

        private long _startTicks;
        private MiniProfiler _profiler;


        public SqlTiming(DbCommand command, ExecuteType type, MiniProfiler profiler)
        {
            CommandString = command.CommandText;
            ExecuteType = type;
            StackTraceSnippet = Helpers.StackTraceSnippet.Get();

            _profiler = profiler;
            _profiler.AddSqlTiming(this);

            _startTicks = _profiler.ElapsedTicks;
            StartMilliseconds = MiniProfiler.GetRoundedMilliseconds(_startTicks);
        }

        [Obsolete("Used for serialization")]
        public SqlTiming()
        {
        }


        public void ExecutionComplete(bool isReader)
        {
            if (isReader)
            {
                FirstFetchDurationMilliseconds = GetDurationMilliseconds();
            }
            else
            {
                DurationMilliseconds = GetDurationMilliseconds();
            }
        }

        public void ReaderFetchComplete()
        {
            DurationMilliseconds = GetDurationMilliseconds();
        }

        private double GetDurationMilliseconds()
        {
            return MiniProfiler.GetRoundedMilliseconds(_profiler.ElapsedTicks - _startTicks);
        }

    }
}