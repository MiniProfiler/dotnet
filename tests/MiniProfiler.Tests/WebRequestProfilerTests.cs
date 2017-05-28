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
            _provider = WebRequestProfilerProvider.Setup();
        }

        public void Dispose()
        {
            _provider = null;
        }

        [Fact]
        public void WebRequestEnsureName()
        {
            using (var rq = GetRequest("http://localhost/Test.aspx", startAndStopProfiler: false))
            {
                var c = MiniProfiler.Start(null, _provider);
                Increment(); // 1 ms
                MiniProfiler.Stop(false, _provider);

                Assert.NotNull(c);
                Assert.Equal("/Test.aspx", c.Name);

                Assert.NotNull(c.Root);
                Assert.False(c.Root.HasChildren);
            }
        }
    }
}
