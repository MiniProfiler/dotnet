using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dapper;
using Oracle.ManagedDataAccess.Client;
using StackExchange.Profiling.Storage;

namespace Samples.Mvc5.Helpers
{
    /// <summary>
    /// The SQLITE mini profiler storage.
    /// </summary>
    public class OracleMiniProfilerStorage : OracleStorage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OracleMiniProfilerStorage"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public OracleMiniProfilerStorage(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Get the Connection.
        /// </summary>
        /// <returns>The Abstracted Connection</returns>
        protected override System.Data.Common.DbConnection GetConnection() =>
            new OracleConnection(ConnectionString);

        /// <summary>
        /// Used for testing purposes - create tables in Oracle database
        /// </summary>
        /// <param name="extraTablesToCreate">The Extra Tables To Create.</param>
        public OracleMiniProfilerStorage RecreateDatabase(params string[] extraTablesToCreate)
        {
            using (var cnn = GetConnection())
            {
                // We need some tiny mods to allow SQLite support 
                foreach (var sql in TableCreationScripts.Union(extraTablesToCreate))
                {
                    cnn.Execute(sql);
                }
            }
            return this;
        }
    }
}
