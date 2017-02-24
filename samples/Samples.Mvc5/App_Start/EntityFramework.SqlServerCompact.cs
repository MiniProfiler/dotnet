using System.Data.Entity;
using System.Data.Entity.Infrastructure;

//[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Samples.Mvc5.App_Start.EntityFrameworkSqlServerCompact), "Start")]

namespace Samples.Mvc5.App_Start
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
