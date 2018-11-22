using Microsoft.EntityFrameworkCore;

namespace Samples.Remote.Api.Data
{
    public class SampleContext : DbContext
    {
        public SampleContext(DbContextOptions<SampleContext> options)
            : base(options)
        { }

        public DbSet<Sample> Samples { get; set; }
    }

    public class Sample
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
