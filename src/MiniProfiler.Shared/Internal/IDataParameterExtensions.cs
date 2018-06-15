using System;
using System.Data;
#if NETSTANDARD1_5
using System.Reflection;
#endif

namespace StackExchange.Profiling.Internal
{
    /// <summary>
    /// Internal MiniProfiler extensions, not meant for consumption.
    /// This can and probably will break without warning. Don't use the .Internal namespace directly.
    /// </summary>
    public static class IDataParameterExtensions
    {
        /// <summary>
        /// Returns the value of <paramref name="parameter"/> suitable for storage/display.
        /// </summary>
        /// <param name="parameter">The parameter to get a value for.</param>
        public static string GetStringValue(this IDataParameter parameter)
        {
            object rawValue = parameter.Value;
            if (rawValue == null || rawValue == DBNull.Value)
            {
                return null;
            }

            // This assumes that all SQL variants use the same parameter format, it works for T-SQL
            if (parameter.DbType == DbType.Binary)
            {
                if (rawValue is byte[] bytes && bytes.Length <= SqlTimingParameter.MaxByteParameterSize)
                {
                    return "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
                }

                // Parameter is too long, so blank it instead
                return null;
            }

            if (parameter.DbType == DbType.Date && rawValue is DateTime)
            {
                return ((DateTime) rawValue).ToString("d", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (parameter.DbType == DbType.Time && rawValue is TimeSpan)
            {
                return ((TimeSpan) rawValue).ToString("hh\\:mm\\:ss");
            }

            if (rawValue is DateTime)
            {
                return ((DateTime) rawValue).ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            }

            if (rawValue is DateTimeOffset)
            {
                var time = (DateTimeOffset) rawValue;
                var offset = time.Offset;
                return time.ToString("s", System.Globalization.CultureInfo.InvariantCulture) 
                       + (offset < TimeSpan.Zero ? "-" : "+")
                       + offset.ToString("hh\\:mm");
            }

            // we want the integral value of an enum, not its string representation
            var rawType = rawValue.GetType();
#if NETSTANDARD1_5
            if (rawType.GetTypeInfo().IsEnum)
#else
            if (rawType.IsEnum)
#endif
            {
                // use ChangeType, as we can't cast - http://msdn.microsoft.com/en-us/library/exx3b86w(v=vs.80).aspx
                return Convert.ChangeType(rawValue, Enum.GetUnderlyingType(rawType)).ToString();
            }

            return rawValue.ToString();
        }

        /// <summary>
        /// Gets the size of a <see cref="IDbDataParameter"/> (e.g. nvarchar(20) would be 20).
        /// </summary>
        /// <param name="parameter">The parameter to get the size of.</param>
        /// <returns>The size of the parameter, or 0 if nullable or unlimited.</returns>
        public static int GetSize(this IDbDataParameter parameter) =>
            parameter.IsNullable && parameter.Value == null ? 0 : parameter.Size;
    }
}
