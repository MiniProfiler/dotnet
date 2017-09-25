using System.IO;
using ProtoBuf;
using StackExchange.Redis;

namespace StackExchange.Profiling.Storage.Internal
{
    /// <summary>
    /// Extension methods for <see cref="RedisStorage"/> and testing.
    /// These can and will change without notice and are not supported APIs.
    /// </summary>
    public static class RedisStorageHelpers
    {
        /// <summary>
        /// Converts a <see cref="MiniProfiler"/> into a <see cref="RedisValue"/>
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to convert.</param>
        /// <returns>The <see cref="RedisValue"/> created.</returns>
        public static RedisValue ToRedisValue(this MiniProfiler profiler)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, profiler);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Converts a <see cref="RedisValue"/> into a <see cref="MiniProfiler"./>
        /// </summary>
        /// <param name="value">The <see cref="RedisValue"/> to convert.</param>
        /// <returns>The <see cref="MiniProfiler"/> created.</returns>
        public static MiniProfiler ToMiniProfiler(this RedisValue value)
        {
            using (var stream = new MemoryStream(value))
            {
                return Serializer.Deserialize<MiniProfiler>(stream);
            }
        }
    }
}
