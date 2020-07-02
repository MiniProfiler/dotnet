namespace Samples.Mvc5.EFCodeFirst
{
    using System.Data.Entity;

    /// <summary>
    /// The EF context.
    /// </summary>
    public class EFContext : DbContext
    {
        public EFContext(string connectionString) : base(connectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Set default esquema for Oracle User
            modelBuilder.HasDefaultSchema("USERID");

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Gets or sets the people.
        /// </summary>
        public DbSet<Person> People { get; set; }
    }
}
