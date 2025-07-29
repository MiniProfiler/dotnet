using System;
using System.Web;
using Xunit;

namespace StackExchange.Profiling.Tests
{
    public class WebRequestProfilerTests : AspNetTest, IDisposable
    {
        public WebRequestProfilerTests(ITestOutputHelper output) : base(output)
        {
            Options.ProfilerProvider = new AspNetRequestProvider();
        }

        public void Dispose()
        {
            Options = null!;
        }

        [Fact]
        public void WebRequestEnsureName()
        {
            Skip.IfNotWindows();
            using (var rq = GetRequest("http://localhost/Test.aspx"))
            {
                try
                {
                    _ = HttpContext.Current.Request.Url;
                }
                catch (Exception ex)
                {
                    Output.WriteLine("eating initial .config load exception: " + ex);
                }
                var mp = new MiniProfiler(null, Options);
                mp.Increment(); // 1 ms
                Output.WriteLine("Url: " + HttpContext.Current.Request.Url);
                mp.Stop(false);

                Assert.NotNull(mp);
                Assert.Equal("http://localhost/Test.aspx", mp.Name);

                Assert.NotNull(mp.Root);
                Assert.False(mp.Root.HasChildren);
            }
        }
    }
}
