using System;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Profiling.Storage;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class MemoryCacheStorageTests : StorageBaseTest, IClassFixture<MemoryCacheStorageFixture>
    {
        public MemoryCacheStorageTests(MemoryCacheStorageFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }

    public class MemoryCacheStorageFixture : StorageFixtureBase<MemoryCacheStorage>
    {
        public MemoryCacheStorageFixture()
        {
            Storage = new MemoryCacheStorage(new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromMinutes(5));
        }

        protected override void Dispose(bool disposing) { }
    }
}
