using System;

using NUnit.Framework;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling.Tests.Storage
{
    /// <summary>
    /// test the HTTP runtime cache storage.
    /// </summary>
    [TestFixture]
    public class MultiStorageProviderTests
    {
        [Test]
        public void Constructor_LoadWithNoStores_ThrowsError()
        {
            bool errorCaught = false;
            try
            {
                MultiStorageProvider p = new MultiStorageProvider();
            }
            catch (ArgumentNullException ex)
            {
                Assert.AreEqual("stores", ex.ParamName, "wrong exception param");
                errorCaught = true;
            }
            Assert.IsTrue(errorCaught, "No Error caught");
        }

        [Test]
        public void Constructor_LoadWithNullStores_ThrowsError()
        {
            bool errorCaught = false;
            try
            {
                MultiStorageProvider p = new MultiStorageProvider(null, null);
            }
            catch (ArgumentNullException ex)
            {
                Assert.AreEqual("stores", ex.ParamName, "wrong exception param");
                errorCaught = true;
            }
            Assert.IsTrue(errorCaught, "No Error caught");
        }

        [Test]
        public void Constructor_LoadStores_MaintainOrder()
        {
            MultiStorageProvider p = new MultiStorageProvider(new HttpRuntimeCacheStorage(new TimeSpan(1, 0, 0)), new SqlServerStorage(""));
            Assert.AreEqual(2, p.Stores.Count);
            Assert.IsTrue(p.Stores[0].GetType() == typeof(HttpRuntimeCacheStorage));
            Assert.IsTrue(p.Stores[1].GetType() == typeof(SqlServerStorage));
        }
    }
}
