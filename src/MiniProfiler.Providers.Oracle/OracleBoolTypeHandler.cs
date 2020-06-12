using System.Data;
using Dapper;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Handle Bool to Integer conversions for Oracle Provider
    /// </summary>
    public class OracleBoolTypeHandler : SqlMapper.TypeHandler<bool>
    {
        /// <inheritdoc/>
        public override bool Parse(object value) => ((int)value) == 1;

        /// <inheritdoc/>
        public override void SetValue(IDbDataParameter parameter, bool value)
        {
            parameter.Value = value ? 1 : 0;
        }
    }
}
