using StackExchange.Profiling;
using StackExchange.Profiling.Helpers;
using Xunit;

namespace Tests
{
    public class SerializationTests : BaseTest
    {
        #region Payloads

        private const string _simpleJson = @"
{
  ""Id"": ""b5f56a27-8a42-4664-8e22-4f1eb79c6440"",
  ""Started"": ""2017-04-10T01:27:43.185073Z"",
  ""DurationMilliseconds"": 0.0,
  ""MachineName"": ""CRAVER-WINBOOK8"",
  ""Root"": {
    ""Id"": ""f80f620b-221f-4eb7-97e6-042e2049cf33"",
    ""Name"": ""Test"",
    ""StartMilliseconds"": 0.0,
    ""Children"": [
      {
        ""Id"": ""fa4db196-092a-484d-ba28-75e68cd2ba7a"",
        ""Name"": ""Main"",
        ""DurationMilliseconds"": 0.0,
        ""StartMilliseconds"": 0.0,
        ""Children"": [
          {
            ""Id"": ""e5790ef2-e55e-4927-8b63-aa0eb68990e3"",
            ""Name"": ""Sub Step 1"",
            ""DurationMilliseconds"": 0.0,
            ""StartMilliseconds"": 0.0
          },
          {
            ""Id"": ""4a317ab1-aed2-4ec5-a653-7b63fc46f4c1"",
            ""Name"": ""Sub Step 2"",
            ""DurationMilliseconds"": 0.0,
            ""StartMilliseconds"": 0.0
          }
        ]
      }
    ]
  },
  ""HasUserViewed"": false
}";
        #endregion

        [Fact]
        public void ParentMapping()
        {
            var mp = new MiniProfiler("Test", Options);
            using (mp.Step("Main"))
            {
                using (mp.Step("Sub Step 1"))
                {
                    using (mp.CustomTiming("cat", "Command 1")) {}
                    using (mp.CustomTiming("cat", "Command 2")) {}
                    using (mp.CustomTiming("cat", "Command 3")) {}
                }
                using (mp.Step("Sub Step 2"))
                {
                    using (mp.CustomTiming("cat", "Command 4")) {}
                    using (mp.CustomTiming("cat", "Command 5")) {}
                    using (mp.CustomTiming("cat", "Command 6")) {}
                }
            }
            mp.Stop();
            var json = mp.ToJson();

            var deserialized = MiniProfiler.FromJson(json);
            var root = deserialized.Root;
            foreach (var t in root.Children)
            {
                Assert.Equal(root, t.ParentTiming);
                Assert.True(root == t.ParentTiming);

                foreach (var tc in t.Children)
                {
                    Assert.Equal(t, tc.ParentTiming);
                    Assert.True(t == tc.ParentTiming);
                }
            }
        }
    }
}
