namespace StackExchange.Profiling.Data
{
    using System;
    using System.Data.Common;
    using System.Reflection;

    /// <summary>
    /// The entity framework profiled database connection.
    /// </summary>
    public class EFProfiledDbConnection : ProfiledDbConnection
    {
        /// <summary>
        /// The rip inner provider.
        /// </summary>
        private static readonly Func<DbConnection, DbProviderFactory> RipInnerProvider =
            (Func<DbConnection, DbProviderFactory>)
            Delegate.CreateDelegate(
                typeof(Func<DbConnection, DbProviderFactory>),
                typeof(DbConnection).GetProperty("DbProviderFactory", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetGetMethod(true));

        /// <summary>
        /// The factory.
        /// </summary>
        private DbProviderFactory _factory;

        /// <summary>
        /// Initialises a new instance of the <see cref="EFProfiledDbConnection"/> class.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        /// <param name="profiler">
        /// The profiler.
        /// </param>
        public EFProfiledDbConnection(DbConnection connection, IDbProfiler profiler)
            : base(connection, profiler)
        {
        }

        /// <summary>
        /// Gets the database provider factory.
        /// </summary>
        protected override DbProviderFactory DbProviderFactory
        {
            get
            {
                if (_factory != null) 
                    return _factory;
                DbProviderFactory tail = RipInnerProvider(_connection);
               
                var field = EFProviderUtilities.ResolveFactoryTypeOrOriginal(tail.GetType()).GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                    _factory = (DbProviderFactory)field.GetValue(null);
                
                return _factory;
            }
        }

        /// <summary>
        /// dispose the connection.
        /// </summary>
        /// <param name="disposing">false if called from a <c>finalizer</c></param>
        protected override void Dispose(bool disposing)
        {
            _factory = null;
            base.Dispose(disposing);
        }
    }
}
