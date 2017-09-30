using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StackExchange.Profiling.Storage
{
    public interface IDataContextFactory
    {
        ProfilerDbContext Create();
    }

    public class ProfilerDbContextFactory : IDataContextFactory
    {
        private string Connectionstring { get; }
        public ProfilerDbContextFactory(string connectionString)
        {
            Connectionstring = connectionString;
        }
        public ProfilerDbContext Create()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ProfilerDbContext>();
            optionsBuilder.UseSqlite(Connectionstring);

            return new ProfilerDbContext(optionsBuilder.Options);
        }
    }
}
