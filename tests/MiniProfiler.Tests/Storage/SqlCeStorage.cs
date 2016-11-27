using System.Data.Common;
using System.Data.SqlServerCe;

using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling.Tests.Storage
{
    /// <summary>
    /// SQL CE Storage.
    /// </summary>
    internal class SqlCeStorage : SqlServerStorage
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SqlCeStorage"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SqlCeStorage(string connectionString) : base(connectionString) { }
        
        protected override DbConnection GetConnection() => new SqlCeConnection(ConnectionString);
    }
}