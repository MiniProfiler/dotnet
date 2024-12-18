﻿using System;
using StackExchange.Profiling.Storage;
using StackExchange.Profiling.Storage.Internal;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class RedisStorageTests : StorageBaseTest, IClassFixture<RedisStorageFixture>
    {
        public RedisStorageTests(RedisStorageFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }

        [Fact]
        public void Serialization()
        {
            var mp = GetMiniProfiler();

            var serialized = mp.ToRedisValue();
            Assert.NotEqual(default, serialized);

            var deserialized = serialized.ToMiniProfiler();
            Assert.Equal(mp, deserialized);
        }

        [Fact]
        public void ConnectionString()
        {
            var storage = new RedisStorage(TestConfig.Current.RedisConnectionString);
            storage.GetUnviewedIds("");
        }

        [Fact]
        public void ConnectionOptions()
        {
            var configOptions = ConfigurationOptions.Parse(TestConfig.Current.RedisConnectionString);
            var storage = new RedisStorage(configOptions);
            storage.GetUnviewedIds("");
        }

        [Fact]
        public void Multiplexer()
        {
            var multiplexer = ConnectionMultiplexer.Connect(TestConfig.Current.RedisConnectionString);
            var storage = new RedisStorage(multiplexer);
            storage.GetUnviewedIds("");
        }

        [Fact]
        public void IDatabase()
        {
            var multiplexer = ConnectionMultiplexer.Connect(TestConfig.Current.RedisConnectionString);
            var database = multiplexer.GetDatabase();
            var storage = new RedisStorage(database);
            storage.GetUnviewedIds("");
        }
    }

    public class RedisStorageFixture : StorageFixtureBase<RedisStorage>, IDisposable
    {
        public RedisStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.RedisConnectionString), TestConfig.Current.RedisConnectionString);

            var testSuffix = Guid.NewGuid().ToString("N") + "_";
            try
            {
                Storage = new RedisStorage(TestConfig.Current.RedisConnectionString);
                Storage.ProfilerResultKeyPrefix = Storage.ProfilerResultKeyPrefix.Append(testSuffix);
                Storage.ProfilerResultSetKey = Storage.ProfilerResultSetKey.Append(testSuffix);
                Storage.ProfilerResultUnviewedSetKeyPrefix = Storage.ProfilerResultUnviewedSetKeyPrefix.Append(testSuffix);
            }
            catch (Exception e)
            {
                e.MaybeLog(TestConfig.Current.RedisConnectionString);
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        protected override void Dispose(bool disposing) => Storage?.Dispose();
    }
}
