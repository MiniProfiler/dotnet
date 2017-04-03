using System.IO;
using ProtoBuf;
using StackExchange.Redis;

namespace StackExchange.Profiling.Storage
{
    internal static class SerializationHelpers
    {
        public static RedisValue ToRedisValue(this MiniProfiler profiler)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, profiler);
                return stream.ToArray();
            }
        }

        public static MiniProfiler ToMiniProfiler(this RedisValue value)
        {
            using (var stream = new MemoryStream(value))
            {
                return Serializer.Deserialize<MiniProfiler>(stream);
            }
        }
    }
}
