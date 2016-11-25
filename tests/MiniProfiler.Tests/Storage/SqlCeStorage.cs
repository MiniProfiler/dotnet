namespace StackExchange.Profiling.Tests.Storage
{
    using System.Data.Common;
    using System.Data.SqlServerCe;

    using StackExchange.Profiling.Storage;

    /// <summary>
    /// SQL CE Storage.
    /// </summary>
    internal class SqlCeStorage : SqlServerStorage
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="SqlCeStorage"/> class.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string.
        /// </param>
        public SqlCeStorage(string connectionString) : base(connectionString) { }

        /// <summary>
        /// The get connection.
        /// </summary>
        /// <returns>
        /// The <see cref="DbConnection"/>.
        /// </returns>
        protected override DbConnection GetConnection() => new SqlCeConnection(ConnectionString);
    }
}