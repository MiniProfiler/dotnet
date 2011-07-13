using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using System.Data.Common;
using System.Data;
using MvcMiniProfiler.Data;
using System.Data.SqlServerCe;

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

                Assert.That(c.Root, Is.Not.Null);
                Assert.That(c.Root.HasChildren, Is.False);
            }

            var p = GetProfiler();
            Assert.That(p.Root.HasChildren, Is.False);
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
            var depth = 2;
            var ms = 10;
            var timeWithRoot = (depth + 1) * ms;
            var fudgeFactor = 5;

            var p = GetProfiler(childDepth: depth, stepSleepMilliseconds: ms);

            Assert.That(p.DurationMilliseconds, Is.EqualTo(timeWithRoot).Within(fudgeFactor));

            Assert.That(p.Root, Is.Not.Null);
            Assert.That(p.Root.DurationMilliseconds, Is.EqualTo(timeWithRoot).Within(fudgeFactor));
            Assert.That(p.Root.DurationWithoutChildrenMilliseconds, Is.EqualTo(ms).Within(fudgeFactor));

            Assert.That(p.GetTimingHierarchy().Count(), Is.EqualTo(3)); // root -> child -> child

            Assert.That(p.Root.HasChildren, Is.True);
            Assert.That(p.Root.Children, Has.Count.EqualTo(1));

            Assert.That(p.Root.Children.Single().HasChildren, Is.True);
        }

    }
}
