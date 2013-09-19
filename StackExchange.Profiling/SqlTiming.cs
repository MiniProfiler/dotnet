using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Helpers;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Profiles a single SQL execution.
    /// </summary>
    [DataContract]
    public class SqlTiming : CustomTiming // TODO: remove this inheritance 
    {
        /// <summary>
        /// Holds the maximum size that will be stored for byte[] parameters
        /// </summary>
        private const int MaxByteParameterSize = 512;
        private readonly MiniProfiler _profiler;
        private readonly long _startTicks;

        /// <summary>
        /// Initialises a new instance of the <see cref="SqlTiming"/> class. 
        /// Creates a new <c>SqlTiming</c> to profile 'command'.
        /// </summary>
        public SqlTiming(IDbCommand command, SqlExecuteType type, MiniProfiler profiler)
        {
            Id = Guid.NewGuid();

            CommandString = AddSpacesToParameters(command.CommandText);
            Parameters = GetCommandParameters(command);
            ExecuteType = type.ToString();

            if (!MiniProfiler.Settings.ExcludeStackTraceSnippetFromCustomTimings)
                StackTraceSnippet = Helpers.StackTraceSnippet.Get();

            _profiler = profiler;
            if (_profiler != null)
            {
                // THREADING: revisit
                _profiler.Head.AddSqlTiming(this);

                _startTicks = _profiler.ElapsedTicks;
                StartMilliseconds = _profiler.GetRoundedMilliseconds(_startTicks);
            }
        }

        // TODO: remove this

        /// <summary>
        /// Gets the command string with special formatting applied based on <c>MiniProfiler.Settings.SqlFormatter</c>
        /// </summary>
        internal string FormattedCommandString
        {
            get
            {
                if (MiniProfiler.Settings.SqlFormatter == null)
                    return CommandString;

                return MiniProfiler.Settings.SqlFormatter.FormatSql(this);
            }
        }

        /// <summary>
        /// Gets or sets any parameter names and values used by the profiled <c>DbCommand.</c>
        /// </summary>
        [ScriptIgnore]
        public List<SqlTimingParameter> Parameters { get; set; }

        /// <summary>
        /// Returns a snippet of the SQL command and the duration.
        /// </summary>
        public override string ToString()
        {
            return CommandString.Truncate(30) + " (" + DurationMilliseconds + " ms)";
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object rValue)
        {
            return rValue is SqlTiming && Id.Equals(((SqlTiming)rValue).Id);
        }

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Called when command execution is finished to determine this <c>SqlTiming's</c> duration.
        /// </summary>
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

        /// <summary>
        /// Called when database reader is closed, ending profiling for 
        /// <see cref="StackExchange.Profiling.Data.SqlExecuteType.Reader"/> <c>SqlTimings</c>.
        /// </summary>
        public void ReaderFetchComplete()
        {
            DurationMilliseconds = GetDurationMilliseconds();
        }

        /// <summary>
        /// Returns the value of <paramref name="parameter"/> suitable for storage/display.
        /// </summary>
        private static string GetValue(IDataParameter parameter)
        {
            object rawValue = parameter.Value;
            if (rawValue == null || rawValue == DBNull.Value)
            {
                return null;
            }

            // This assumes that all SQL variants use the same parameter format, it works for T-SQL
            if (parameter.DbType == DbType.Binary)
            {
                var bytes = rawValue as byte[];
                if (bytes != null && bytes.Length <= MaxByteParameterSize)
                {
                    return "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                }

                // Parameter is too long, so blank it instead
                return null;
            }

            if (rawValue is DateTime)
            {
                return ((DateTime)rawValue).ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            }

            // we want the integral value of an enum, not its string representation
            var rawType = rawValue.GetType();
            if (rawType.IsEnum)
            {
                // use ChangeType, as we can't cast - http://msdn.microsoft.com/en-us/library/exx3b86w(v=vs.80).aspx
                return Convert.ChangeType(rawValue, Enum.GetUnderlyingType(rawType)).ToString();
            }

            return rawValue.ToString();
        }

        private static int GetParameterSize(IDbDataParameter parameter)
        {
            var value = parameter.Value as INullable;
            if (value != null)
            {
                var nullable = value;
                if (nullable.IsNull)
                {
                    return 0;
                }
            }

            return parameter.Size;
        }

        private decimal GetDurationMilliseconds()
        {
            return _profiler.GetRoundedMilliseconds(_profiler.ElapsedTicks - _startTicks);
        }

        /// <summary>
        /// To help with display, put some space around crowded commas.
        /// </summary>
        private string AddSpacesToParameters(string commandString)
        {
            return Regex.Replace(commandString, @",([^\s])", ", $1");
        }

        private List<SqlTimingParameter> GetCommandParameters(IDbCommand command)
        {
            if (command.Parameters == null || command.Parameters.Count == 0) return null;

            var result = new List<SqlTimingParameter>();

            foreach (DbParameter parameter in command.Parameters)
            {
                if (!string.IsNullOrWhiteSpace(parameter.ParameterName))
                {
                    result.Add(new SqlTimingParameter
                    {
                        ParentSqlTimingId = Id,
                        Name = parameter.ParameterName.Trim(),
                        Value = GetValue(parameter),
                        DbType = parameter.DbType.ToString(),
                        Size = GetParameterSize(parameter)
                    });
                }
            }

            return result;
        }
    }
}