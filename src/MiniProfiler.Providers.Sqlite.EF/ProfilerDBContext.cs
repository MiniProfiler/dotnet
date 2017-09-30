using Microsoft.EntityFrameworkCore;

namespace StackExchange.Profiling.Storage
{
    public class ProfilerDbContext : DbContext
    {
        public DbSet<MiniProfilers> MiniProfilers { get; set; }
        public DbSet<MiniProfilerTimings> MiniProfilerTimings { get; set; }
        public DbSet<MiniProfilerClientTimings> MiniProfilerClientTimings { get; set; }

        public ProfilerDbContext(DbContextOptions<ProfilerDbContext> options) : base(options)
        {
        }

        public bool Init(bool recreate)
        {
            if(recreate) Database.EnsureDeleted();
            return Database.EnsureCreated();
        }
    }
}
