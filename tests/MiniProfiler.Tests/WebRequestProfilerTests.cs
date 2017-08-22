using System;
using StackExchange.Profiling;
using Xunit;

namespace Tests
{
    [Collection("WebRequest")]
    public class WebRequestProfilerTests : BaseTest, IDisposable
    {
        public WebRequestProfilerTests()
        {
            Options.SetProvider(new AspNetRequestProvider());
        }

        public void Dispose()
        {
            Options = null;
        }

        [Fact]
        public void WebRequestEnsureName()
        {
            using (var rq = GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                var mp = new MiniProfiler(null, Options);
                mp.Increment(); // 1 ms
                mp.Stop(false);

                Assert.NotNull(mp);
                Assert.Equal("/Test.aspx", mp.Name);

                Assert.NotNull(mp.Root);
                Assert.False(mp.Root.HasChildren);
            }
        }
    }
}
