using System;
using System.Data;
using Dapper;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Handle Guid to String conversions for Oracle Provider
    /// </summary>
    //public class OracleGuidTypeHandler : SqlMapper.TypeHandler<Guid>
    //{
    //    /// <inheritdoc/>
    //    public override Guid Parse(object value) => new Guid(value.ToString());

    //    /// <inheritdoc/>
    //    public override void SetValue(IDbDataParameter parameter, Guid value)
    //    {
    //        parameter.Value = value.ToString();
    //    }
    //}

    /// <summary>
    /// Handle Guid to String conversions for Oracle Provider
    /// </summary>
    public class OracleNullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
    {
        /// <inheritdoc/>
        public override Guid? Parse(object value) => value == null ? (Guid?)null : new Guid(value.ToString());

        /// <inheritdoc/>
        public override void SetValue(IDbDataParameter parameter, Guid? value)
        {
            parameter.Value = value?.ToString();
        }
    }
}
