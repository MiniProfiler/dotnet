using System.Data.Entity;
using System.Data.Entity.Infrastructure;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(SampleWeb.App_Start.EntityFrameworkSqlServerCompact), "Start")]

namespace SampleWeb.App_Start 
{
    /// <summary>
    /// web server activation point where the connection factory is configured.
    /// </summary>
    public static class EntityFrameworkSqlServerCompact 
    {
        /// <summary>
        /// set the default connection factory.
        /// </summary>
        public static void Start()
        {
#pragma warning disable 0618
            Database.DefaultConnectionFactory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");
#pragma warning restore 0618
        }
    }
}
