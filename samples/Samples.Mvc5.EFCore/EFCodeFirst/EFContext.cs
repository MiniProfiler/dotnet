using Microsoft.EntityFrameworkCore;

namespace Samples.Mvc5.EFCodeFirst
{
    /// <summary>
    /// The EF context.
    /// </summary>
    public class EFContext : DbContext
    {
        public EFContext() { }
        public EFContext(DbContextOptions<EFContext> options) : base(options) { }

        /// <summary>
        /// Gets or sets the people.
        /// </summary>
        public DbSet<Person> People { get; set; }
    }
}
