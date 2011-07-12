using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

using Assert = NUnit.Framework.Assert;
using NUnit.Framework;

namespace MvcMiniProfiler.Tests
{
    [TestClass]
    public class MiniProfilerTest : BaseTest
    {
        [TestMethod]
        public void Simple()
        {
            using (var req = SimulateRequest("http://localhost/Test.aspx"))
            {
                MiniProfiler.Start();
                Thread.Sleep(10);
                MiniProfiler.Stop();

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Not.Null);
                Assert.That(c.DurationMilliseconds, Is.GreaterThan(8).And.LessThan(15)); // hopefully, we should hit this target
                Assert.That(c.Name, Is.EqualTo("/Test.aspx"));
            }
        }

        [TestMethod]
        public void DiscardResults()
        {
            using (var req = SimulateRequest("http://localhost/Test.aspx"))
            {
                MiniProfiler.Start();
                MiniProfiler.Stop(discardResults: true);

                var c = MiniProfiler.Current;

                Assert.That(c, Is.Null);
            }
        }

        [TestMethod]
        public void SmallSteps()
        {
            using (var req = SimulateRequest("http://localhost/Test.aspx"))
            {
                var c = MiniProfiler.Start();

                using (c.Step("test step"))
                {
                    Thread.Sleep(10);
                }

                MiniProfiler.Stop();

                Assert.That(c.DurationMilliseconds, Is.GreaterThan(8).And.LessThan(15));
                Assert.That(c.Name, Is.EqualTo("/Test.aspx"));

                Assert.That(c.Root, Is.Not.Null);
                Assert.That(c.Root.DurationMilliseconds, Is.EqualTo(c.DurationMilliseconds).Within(1));
                Assert.That(c.Root.HasChildren, Is.True);
                Assert.That(c.Root.Children, Has.Count.EqualTo(1));
            }
        }
    }
}
