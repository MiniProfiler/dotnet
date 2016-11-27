using System;

using StackExchange.Profiling.Storage;
using Xunit;

namespace StackExchange.Profiling.Tests.Storage
{
    /// <summary>
    /// test the HTTP runtime cache storage.
    /// </summary>
    public class MultiStorageProviderTests
    {
        [Fact]
        public void Constructor_LoadWithNoStores_ThrowsError()
        {
            bool errorCaught = false;
            try
            {
                MultiStorageProvider p = new MultiStorageProvider();
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal("stores", ex.ParamName); // wrong exception param
                errorCaught = true;
            }
            Assert.True(errorCaught, "No Error caught");
        }

        [Fact]
        public void Constructor_LoadWithNullStores_ThrowsError()
        {
            bool errorCaught = false;
            try
            {
                MultiStorageProvider p = new MultiStorageProvider(null, null);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal("stores", ex.ParamName); // wrong exception param
                errorCaught = true;
            }
            Assert.True(errorCaught, "No Error caught");
        }

        [Fact]
        public void Constructor_LoadStores_MaintainOrder()
        {
            MultiStorageProvider p = new MultiStorageProvider(new HttpRuntimeCacheStorage(new TimeSpan(1, 0, 0)), new SqlServerStorage(""));
            Assert.Equal(2, p.Stores.Count);
            Assert.True(p.Stores[0].GetType() == typeof(HttpRuntimeCacheStorage));
            Assert.True(p.Stores[1].GetType() == typeof(SqlServerStorage));
        }
    }
}
