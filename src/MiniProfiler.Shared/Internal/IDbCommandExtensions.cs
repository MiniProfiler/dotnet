using StackExchange.Profiling.Helpers;
using StackExchange.Profiling.SqlFormatters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace StackExchange.Profiling.Internal
{
    public static class IDbCommandExtensions
    {
        private static readonly Regex commandSpacing = new Regex(@",([^\s])", RegexOptions.Compiled);

        /// <summary>
        /// Gets a <see cref=" CustomTiming"/> for the relational parameters passed.
        /// </summary>
        /// <param name="command">The <see cref="DbCommand"/> to time.</param>
        /// <param name="commandType">The command execution type (e.g. ExecuteNonQuery).</param>
        /// <param name="profiler">The miniprofiler to attach the timing to.</param>
        /// <param name="customType">The type for this command to show up as (which custom column).</param>
        /// <returns>A custom timing (which should be disposed or stopped!) for <paramref name="command"/>.</returns>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="profiler"/> is <c>null</c>.</exception>
        public static CustomTiming GetTiming(this IDbCommand command, string commandType, MiniProfiler profiler, string customType = "sql")
        {
            profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));

            var commandText = command.GetReadableCommand();
            var parameters = command.GetParameters();

            if (MiniProfiler.Settings.SqlFormatter != null)
            {
                commandText = MiniProfiler.Settings.SqlFormatter.GetFormattedSql(commandText, parameters, command);
            }

            return profiler.CustomTiming(customType, commandText, commandType);
        }

        /// <summary>
        /// Gets a command's text, adding space around crowded commas for readability.
        /// </summary>
        /// <param name="command">The command to space out.</param>
        public static string GetReadableCommand(this IDbCommand command)
        {
            if (command == null) return string.Empty;
            return commandSpacing.Replace(command.CommandText, ", $1");
        }

        /// <summary>
        /// Returns better parameter information for <paramref name="command"/>.
        /// Returns <c>null</c> if no parameters are present.
        /// </summary>
        /// <param name="command">The cmmand to get parameters for.</param>
        public static List<SqlTimingParameter> GetParameters(this IDbCommand command)
        {
            if ((command?.Parameters?.Count ?? 0) == 0) return null;

            var result = new List<SqlTimingParameter>();

            foreach (DbParameter parameter in command.Parameters)
            {
                if (parameter.ParameterName.HasValue())
                {
                    result.Add(new SqlTimingParameter
                    {
                        Name = parameter.ParameterName.Trim(),
                        Value = parameter.GetStringValue(),
                        DbType = parameter.DbType.ToString(),
                        Size = parameter.GetSize(),
                        Direction = parameter.Direction.ToString(),
                        IsNullable = parameter.IsNullable
                    });
                }
            }

            return result;
        }
    }
}
