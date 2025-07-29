using System;
using StackExchange.Profiling.Storage;
using Xunit;

namespace StackExchange.Profiling.Tests.Storage
{
    public class MemoryCacheStorageTests(MemoryCacheStorageFixture fixture, ITestOutputHelper output) : StorageBaseTest(fixture, output), IClassFixture<MemoryCacheStorageFixture>
    {
    }

    public class MemoryCacheStorageFixture : StorageFixtureBase<MemoryCacheStorage>
    {
        public MemoryCacheStorageFixture()
        {
            Storage = new MemoryCacheStorage(TimeSpan.FromMinutes(5));
        }

        protected override void Dispose(bool disposing) { }
    }
}
