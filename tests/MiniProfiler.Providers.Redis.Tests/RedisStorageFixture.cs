using StackExchange.Profiling.Storage;
using System;

namespace Tests.Redis
{
    public class RedisStorageFixture<T> : IDisposable
    {
        public RedisStorage Storage { get; }

        public RedisStorageFixture() => Storage = new RedisStorage("localhost:6379");

        public void Dispose() => Storage.Dispose();
    }
}