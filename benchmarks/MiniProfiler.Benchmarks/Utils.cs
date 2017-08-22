using StackExchange.Profiling;

namespace Benchmarks
{
    public static class Utils
    {
        internal static MiniProfiler GetComplexProfiler(MiniProfilerBaseOptions options)
        {
            var mp = new MiniProfiler("Complex", options);
            for (var i = 0; i < 50; i++)
            {
                using (mp.Step("Step " + i))
                {
                    for (var j = 0; j < 50; j++)
                    {
                        using (mp.Step("SubStep " + j))
                        {
                            for (var k = 0; k < 50; k++)
                            {
                                using (mp.CustomTiming("Custom " + k, "YOLO!"))
                                {
                                }
                            }
                        }
                    }
                }
            }
            return mp;
        }
    }
}
