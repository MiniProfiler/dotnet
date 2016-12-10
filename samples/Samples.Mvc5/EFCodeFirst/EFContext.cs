namespace Samples.Mvc5.EFCodeFirst
{
    using System.Data.Entity;

    /// <summary>
    /// The EF context.
    /// </summary>
    public class EFContext : DbContext
    {
        /// <summary>
        /// Gets or sets the people.
        /// </summary>
        public DbSet<Person> People { get; set; }
    }
}