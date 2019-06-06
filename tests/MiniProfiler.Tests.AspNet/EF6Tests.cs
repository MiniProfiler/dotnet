using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using StackExchange.Profiling.Data;
using StackExchange.Profiling.EntityFramework6;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests
{
    public class EF6Tests : AspNetTest
    {
        public EF6Tests(ITestOutputHelper output) : base(output)
        {
            MiniProfilerEF6.Initialize();
        }

        [Fact]
        public void ServicesCheck()
        {
            const string providerKey = "System.Data.SQLite";

            Assert.IsType<EFProfiledDbProviderServices>(DbConfiguration.DependencyResolver.GetService(typeof(DbProviderServices), providerKey));
            Assert.IsType<ProfiledDbProviderFactory>(DbConfiguration.DependencyResolver.GetService(typeof(DbProviderFactory), providerKey));
            Assert.IsType<EFProfiledDbProviderFactoryResolver>(DbConfiguration.DependencyResolver.GetService(typeof(IDbProviderFactoryResolver), providerKey));
            Assert.IsType<EFProfiledDbConnectionFactory>(DbConfiguration.DependencyResolver.GetService(typeof(IDbConnectionFactory), providerKey));
        }
    }
}
