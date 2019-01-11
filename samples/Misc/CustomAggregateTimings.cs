using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using StackExchange.Profiling;

namespace Misc
{
    public class CustomAggregateTimings
    {
        public void Example()
        {
            var profiler = MiniProfiler.StartNew();

            var myList = new List<string> { "A", "B", "C", "E" };
            using (profiler.Step("Doing a collection of repeated work"))
            using (var aggregateTimer1 = profiler.Step("Doing part 1"))
            using (var aggregateTimer2 = profiler.Step("Doing part 2"))
            {
                foreach (var listItem in myList)
                {
                    using (new Timing(profiler, aggregateTimer1, listItem))
                    {
                        Method1(listItem);
                    }
                    using (new Timing(profiler, aggregateTimer2, listItem))
                    {
                        Method2(listItem);
                    }
                }
            }

            profiler.Stop();
            Console.WriteLine(profiler.RenderPlainText());
        }

        public void Method1(string name)
        {
            Thread.Sleep(30);
        }

        public void Method2(string name)
        {
            Thread.Sleep(60);
        }
    }
}
