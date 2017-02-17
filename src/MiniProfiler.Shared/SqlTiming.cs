using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.SqlFormatters;
#if !NET46
using System.Reflection;
#endif

namespace StackExchange.Profiling
{
    /// <summary>
    /// Profiles a single SQL execution.
    /// </summary>
    public class SqlTiming
    {
        /// <summary>
        /// Holds the maximum size that will be stored for byte[] parameters
        /// </summary>
        private const int MaxByteParameterSize = 512;
        private readonly MiniProfiler _profiler;
        private readonly CustomTiming _customTiming;

        /// <summary>
        /// Initialises a new instance of the <see cref="SqlTiming"/> class. 
        /// Creates a new <c>SqlTiming</c> to profile 'command'.
        /// </summary>
        public SqlTiming(IDbCommand command, SqlExecuteType type, MiniProfiler profiler)
        {
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));

            var commandText = AddSpacesToParameters(command.CommandText);
            var parameters = GetCommandParameters(command);

            if (MiniProfiler.Settings.SqlFormatter != null)
            {
                commandText = MiniProfiler.Settings.SqlFormatter.GetFormattedSql(commandText, parameters, command);
            }

            _customTiming = profiler.CustomTiming("sql", commandText, type.ToString());
            if (_customTiming == null) throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets or sets the offset from main <c>MiniProfiler</c> start that this custom command began.
        /// </summary>
        public decimal StartMilliseconds => _customTiming.StartMilliseconds;

        /// <summary>
        /// Returns a snippet of the SQL command and the duration.
        /// </summary>
        public override string ToString() =>
            _customTiming.CommandString.Truncate(30) + " (" + _customTiming.DurationMilliseconds + " ms)";

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object other) =>
            other is SqlTiming && _customTiming.Id.Equals(((SqlTiming)other)._customTiming.Id);

        /// <summary>
        /// Returns hash code of Id.
        /// </summary>
        public override int GetHashCode() => _customTiming.Id.GetHashCode();

        /// <summary>
        /// Called when command execution is finished to determine this <c>SqlTiming's</c> duration.
        /// </summary>
        public void ExecutionComplete(bool isReader)
        {
            if (isReader)
            {
                _customTiming.FirstFetchCompleted();
            }
            else
            {
                _customTiming.Stop();
            }
        }

        /// <summary>
        /// Called when database reader is closed, ending profiling for 
        /// <see cref="SqlExecuteType.Reader"/> <c>SqlTimings</c>.
        /// </summary>
        public void ReaderFetchComplete() => _customTiming.Stop();

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
                if (rawValue is byte[] bytes && bytes.Length <= MaxByteParameterSize)
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
#if NET46
            if (rawType.IsEnum)
#else
            if (rawType.GetTypeInfo().IsEnum)
#endif
            {
                // use ChangeType, as we can't cast - http://msdn.microsoft.com/en-us/library/exx3b86w(v=vs.80).aspx
                return Convert.ChangeType(rawValue, Enum.GetUnderlyingType(rawType)).ToString();
            }

            return rawValue.ToString();
        }

        private static int GetParameterSize(IDbDataParameter parameter) =>
            parameter.IsNullable && parameter.Value == null ? 0 : parameter.Size;

        /// <summary>
        /// To help with display, put some space around crowded commas.
        /// </summary>
        private string AddSpacesToParameters(string commandString) =>
            Regex.Replace(commandString, @",([^\s])", ", $1");

        /// <summary>
        /// Returns better parameter information for <paramref name="command"/>.  Returns null if no parameters are present.
        /// </summary>
        public static List<SqlTimingParameter> GetCommandParameters(IDbCommand command)
        {
            if (command.Parameters == null || command.Parameters.Count == 0) return null;

            var result = new List<SqlTimingParameter>();

            foreach (DbParameter parameter in command.Parameters)
            {
                if (parameter.ParameterName.HasValue())
                {
                    result.Add(new SqlTimingParameter
                    {
                        Name = parameter.ParameterName.Trim(),
                        Value = GetValue(parameter),
                        DbType = parameter.DbType.ToString(),
                        Size = GetParameterSize(parameter),
                        Direction = parameter.Direction.ToString(),
                        IsNullable = parameter.IsNullable
                    });
                }
            }

            return result;
        }
    }
}