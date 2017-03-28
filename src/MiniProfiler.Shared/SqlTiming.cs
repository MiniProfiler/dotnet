using System;
using System.Data;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.SqlFormatters;
using StackExchange.Profiling.Internal;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Profiles a single SQL execution.
    /// </summary>
    public class SqlTiming
    {
        private readonly CustomTiming _customTiming;

        /// <summary>
        /// Initialises a new instance of the <see cref="SqlTiming"/> class. 
        /// Creates a new <c>SqlTiming</c> to profile 'command'.
        /// </summary>
        /// <param name="command">The <see cref="DbCommand"/> to time.</param>
        /// <param name="type">The execution type.</param>
        /// <param name="profiler">The miniprofiler to attach the timing to.</param>
        /// <param name="customType">The type for this command to show up as (which custom column).</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="profiler"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Throw if the custom timing can't be created.</exception>
        public SqlTiming(IDbCommand command, SqlExecuteType type, MiniProfiler profiler, string customType = "sql")
        {
            profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));

            var commandText = command.GetReadableCommand();
            var parameters = command.GetParameters();

            if (MiniProfiler.Settings.SqlFormatter != null)
            {
                commandText = MiniProfiler.Settings.SqlFormatter.GetFormattedSql(commandText, parameters, command);
            }

            _customTiming = profiler.CustomTiming(customType, commandText, type.ToString());
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
        /// Returns true if IDs match.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare.</param>
        public override bool Equals(object obj) =>
            obj is SqlTiming && _customTiming.Id.Equals(((SqlTiming)obj)._customTiming.Id);

        /// <summary>
        /// Returns hash code of ID.
        /// </summary>
        public override int GetHashCode() => _customTiming.Id.GetHashCode();

        /// <summary>
        /// Called when command execution is finished to determine this <c>SqlTiming's</c> duration.
        /// </summary>
        /// <param name="isReader">Whether the item completing is a <see cref="IDataReader"/>.</param>
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
    }
}