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
            StackTraceSnippet = GetStackTraceSnippet();

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

        private string GetStackTraceSnippet()
        {
            var frames = new StackTrace(true).GetFrames();
            var methods = (from frame in frames
                           let method = frame.GetMethod()
                           where !ExcludeTypesFromTrace.Contains(method.DeclaringType)
                           && !string.IsNullOrEmpty(method.Name)
                           && !method.Name.Contains("InvokeActionResultWithFilters")
                           && !method.Name.Contains("InvokeActionMethodWithFilters")
                           && !method.Name.EndsWith(".GetEnumerator")
                           && !ExcludeMethodsTrace.Contains(method.Name)
                           select method).TakeWhile(method => !method.Name.Contains("BeginProcessRequest"));

            var result = string.Join(" ", methods.Select(m => m.Name));

            if (result.Length > 100)
                result = result.Remove(100);

            return result;
        }

        private double GetDurationMilliseconds()
        {
            return MiniProfiler.GetRoundedMilliseconds(_profiler.ElapsedTicks - _startTicks);
        }

        #region Excluded Types/Methods from GetStackTraceDisplay()

        private static readonly HashSet<Type> ExcludeTypesFromTrace = new HashSet<Type> 
            {
                //typeof(SqlProfiler),
                //typeof(SqlTiming),
                //typeof(ProfiledDbCommand),
                ////typeof(DataContext),
                //typeof(ControllerActionInvoker),
                //typeof(ViewResultBase),
                //typeof(RazorView),
                //typeof(BuildManagerCompiledView),
                ////typeof(SqlProvider),
                //typeof(WebViewPage),
                //typeof(HtmlHelper),
                ////typeof(StartPage)
            };

        private static readonly HashSet<string> ExcludeMethodsTrace = new HashSet<string>
            {
                "Execute",
                "System.Web.Mvc.IController.Execute",
                "ExecuteCore",
                "Partial", 
                "System.Data.IDbCommand.ExecuteReader",
                "RenderControl",
                "RenderControlInternal",
                "RenderChildrenInternal",
                "RenderChildren",
                "System.Linq.IQueryProvider.Execute",
                "ExecuteReader",
                "Query",
                "lambda_method",
                "Get",
                "Set"
            };

        #endregion
    }
}