using Microsoft.EntityFrameworkCore;

namespace Samples.AspNetCore.Models
{
    public class SampleContext : DbContext
    {
        public DbSet<RouteHit> RouteHits { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(Startup.SqliteConnectionString);
        }
    }

    public class RouteHit
    {
        public int Id { get; set; }
        public string RouteName { get; set; }
        public int HitCount { get; set; }
    }
}
