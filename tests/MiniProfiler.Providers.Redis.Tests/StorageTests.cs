using StackExchange.Profiling;
using StackExchange.Profiling.Storage;
using StackExchange.Redis;
using Xunit;

namespace Tests.Redis
{
    public class StorageTests : IClassFixture<RedisStorageFixture<StorageTests>>
    {
        private readonly RedisStorage _storage;
        private readonly MiniProfilerBaseOptions _options = new MiniProfilerBaseOptions();

        public StorageTests(RedisStorageFixture<StorageTests> fixture)
        {
            _storage = fixture.Storage;
        }

        [Fact]
        public void Serialization()
        {
            var mp = GetMiniProfiler();

            var serialized = mp.ToRedisValue();
            Assert.NotEqual(default(RedisValue), serialized);

            var deserialized = serialized.ToMiniProfiler();
            Assert.Equal(mp, deserialized);
        }

        [Fact]
        public void SaveAndGet()
        {
            var mp = GetMiniProfiler();
            _storage.Save(mp);

            var fetched = _storage.Load(mp.Id);
            Assert.Equal(mp, fetched);
        }

        private MiniProfiler GetMiniProfiler()
        {
            var mp = new MiniProfiler("Test", _options);
            using (mp.Step("Foo"))
            {
                using (mp.CustomTiming("Hey", "There"))
                {
                    // heyyyyyyyyy
                }
            }
            return mp;
        }
    }
}
