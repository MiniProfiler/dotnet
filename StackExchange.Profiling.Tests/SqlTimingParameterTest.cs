using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackExchange.Profiling.Tests
{
    [TestFixture]
    public class SqlTimingParameterTest : BaseTest
    {
        [Test]
        public void GetHashCodeWithNullParameterValue()
        {
            SqlTimingParameter parameter = new SqlTimingParameter();
            parameter.Name = "TestParameter";
            parameter.Value = null;
            parameter.ParentSqlTimingId = Guid.NewGuid();

            Assert.DoesNotThrow(() => parameter.GetHashCode());
        }
    }
}
