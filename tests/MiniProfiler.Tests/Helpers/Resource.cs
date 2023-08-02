using System.IO;

namespace StackExchange.Profiling.Tests
{
    public static class Resource
    {
        public static string? Get(string name)
        {
            using (var stream = typeof(Resource).Assembly.GetManifestResourceStream("StackExchange.Pofiling.Tests." + name))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return null;
        }
    }
}
