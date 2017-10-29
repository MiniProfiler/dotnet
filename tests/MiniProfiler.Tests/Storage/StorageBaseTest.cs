using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapper;
using StackExchange.Profiling.Storage;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public abstract class StorageBaseTest : BaseTest
    {
        protected StorageFixtureBase Fixture { get; }
        protected virtual IAsyncStorage Storage => Fixture.GetStorage();

        protected StorageBaseTest(StorageFixtureBase fixture, ITestOutputHelper output) : base(output)
        {
            Fixture = fixture;
            if (fixture.ShouldSkip)
            {
                Skip.Inconclusive("Couldn't test against: " + (Storage?.GetType() ?? GetType()) + "\n" + fixture.SkipReason);
            }
        }

        [Fact]
        public void GetUnviewedIds()
        {
            var mp1 = GetMiniProfiler("Test1");
            var mp2 = GetMiniProfiler("Test2");
            var mp3 = GetMiniProfiler("Test3");
            Storage.Save(mp1);
            Storage.Save(mp2);
            Storage.Save(mp3);

            var unviewed = Storage.GetUnviewedIds(nameof(GetUnviewedIds));
            Assert.Equal(3, unviewed.Count);
            Assert.Contains(mp1.Id, unviewed);
            Assert.Contains(mp2.Id, unviewed);
            Assert.Contains(mp3.Id, unviewed);
        }

        [Fact]
        public async Task GetUnviewedIdsAsync()
        {
            var mp1 = GetMiniProfiler("Test1");
            var mp2 = GetMiniProfiler("Test2");
            var mp3 = GetMiniProfiler("Test3");
            await Storage.SaveAsync(mp1).ConfigureAwait(false);
            await Storage.SaveAsync(mp2).ConfigureAwait(false);
            await Storage.SaveAsync(mp3).ConfigureAwait(false);

            var unviewed = await Storage.GetUnviewedIdsAsync(nameof(GetUnviewedIdsAsync)).ConfigureAwait(false);
            Assert.Equal(3, unviewed.Count);
            Assert.Contains(mp1.Id, unviewed);
            Assert.Contains(mp2.Id, unviewed);
            Assert.Contains(mp3.Id, unviewed);
        }

        [Fact]
        public void List()
        {
            var mp1 = GetMiniProfiler("Test1");
            var mp2 = GetMiniProfiler("Test2");
            var mp3 = GetMiniProfiler("Test3");
            Storage.Save(mp1);
            Storage.Save(mp2);
            Storage.Save(mp3);

            var stored = Storage.List(200).ToList();
            Assert.True(stored.Count >= 3);
            Assert.Contains(mp1.Id, stored);
            Assert.Contains(mp2.Id, stored);
            Assert.Contains(mp3.Id, stored);
        }

        [Fact]
        public async Task ListAsync()
        {
            var mp1 = GetMiniProfiler("Test1");
            var mp2 = GetMiniProfiler("Test2");
            var mp3 = GetMiniProfiler("Test3");
            Storage.Save(mp1);
            Storage.Save(mp2);
            Storage.Save(mp3);

            var stored = (await Storage.ListAsync(200).ConfigureAwait(false)).ToList();
            Assert.True(stored.Count >= 3);
            Assert.Contains(mp1.Id, stored);
            Assert.Contains(mp2.Id, stored);
            Assert.Contains(mp3.Id, stored);
        }

        [Fact]
        public void SaveAndLoad()
        {
            var mp = GetMiniProfiler();
            Storage.Save(mp);

            var fetched = Storage.Load(mp.Id);
            Assert.Equal(mp, fetched);
            Assert.NotNull(fetched.Options);
        }

        [Fact]
        public async Task SaveAndLoadAsync()
        {
            var mp = GetMiniProfiler();
            await Storage.SaveAsync(mp).ConfigureAwait(false);

            var fetched = await Storage.LoadAsync(mp.Id).ConfigureAwait(false);
            Assert.Equal(mp, fetched);
            Assert.NotNull(fetched.Options);
        }

        [Fact]
        public void SetViewed()
        {
            var mp = GetMiniProfiler();
            Assert.False(mp.HasUserViewed);
            Storage.Save(mp);
            Assert.False(mp.HasUserViewed);

            var unviewedIds = Storage.GetUnviewedIds(mp.User);
            Assert.Contains(mp.Id, unviewedIds);
            Storage.SetViewed(mp);
            var unviewedIds2 = Storage.GetUnviewedIds(mp.User);
            Assert.DoesNotContain(mp.Id, unviewedIds2);
        }

        [Fact]
        public async Task SetViewedAsync()
        {
            var mp = GetMiniProfiler();
            Assert.False(mp.HasUserViewed);
            await Storage.SaveAsync(mp).ConfigureAwait(false);
            Assert.False(mp.HasUserViewed);

            var unviewedIds = await Storage.GetUnviewedIdsAsync(mp.User).ConfigureAwait(false);
            Assert.Contains(mp.Id, unviewedIds);
            await Storage.SetViewedAsync(mp).ConfigureAwait(false);
            var unviewedIds2 = await Storage.GetUnviewedIdsAsync(mp.User).ConfigureAwait(false);
            Assert.DoesNotContain(mp.Id, unviewedIds2);
        }

        [Fact]
        public void SetUnviewed()
        {
            var mp = GetMiniProfiler();
            Storage.Save(mp);

            var unviewedIds = Storage.GetUnviewedIds(mp.User);
            Assert.Contains(mp.Id, unviewedIds);

            Storage.SetViewed(mp);
            var unviewedIds2 = Storage.GetUnviewedIds(mp.User);
            Assert.DoesNotContain(mp.Id, unviewedIds2);

            Storage.SetUnviewed(mp);
            var unviewedIds3 = Storage.GetUnviewedIds(mp.User);
            Assert.Contains(mp.Id, unviewedIds3);
        }

        [Fact]
        public async Task SetUnviewedAsync()
        {
            var mp = GetMiniProfiler();
            await Storage.SaveAsync(mp).ConfigureAwait(false);

            var unviewedIds = await Storage.GetUnviewedIdsAsync(mp.User).ConfigureAwait(false);
            Assert.Contains(mp.Id, unviewedIds);

            await Storage.SetViewedAsync(mp).ConfigureAwait(false);
            var unviewedIds2 = await Storage.GetUnviewedIdsAsync(mp.User).ConfigureAwait(false);
            Assert.DoesNotContain(mp.Id, unviewedIds2);

            await Storage.SetUnviewedAsync(mp).ConfigureAwait(false);
            var unviewedIds3 = await Storage.GetUnviewedIdsAsync(mp.User).ConfigureAwait(false);
            Assert.Contains(mp.Id, unviewedIds3);
        }

        protected MiniProfiler GetMiniProfiler(string name = "Test", [CallerMemberName]string user = null)
        {
            var mp = new MiniProfiler(name, Options)
            {
                User = user
            };
            using (mp.Step("Foo"))
            {
                using (mp.CustomTiming("Hey", "There"))
                {
                    // heyyyyyyyyy
                }
            }
            mp.Stop();
            return mp;
        }
    }

    public static class DatabaseStorageExtensions
    {
        /// <summary>
        /// Creates the tables for this storage provider to use.
        /// </summary>
        /// <param name="storage">The storage to create schema for.</param>
        public static void CreateSchema(this IAsyncStorage storage)
        {
            if (storage is DatabaseStorageBase dbs && storage is IDatabaseStorageConnectable dbsc)
            {
                using (var conn = dbsc.GetConnection())
                {
                    foreach (var script in dbs.TableCreationScripts)
                    {
                        conn.Execute(script);
                    }
                }
            }
        }

        /// <summary>
        /// Drops the tables for this storage provider.
        /// </summary>
        /// <param name="storage">The storage to drop schema for.</param>
        public static void DropSchema(this IAsyncStorage storage)
        {
            if (storage is DatabaseStorageBase dbs && storage is IDatabaseStorageConnectable dbsc)
            {
                using (var conn = dbsc.GetConnection())
                {
                    conn.Execute("Drop Table " + dbs.MiniProfilerClientTimingsTable);
                    conn.Execute("Drop Table " + dbs.MiniProfilerTimingsTable);
                    conn.Execute("Drop Table " + dbs.MiniProfilersTable);
                }
            }
        }
    }

    public abstract class StorageFixtureBase
    {
        public string TestId { get; } = Guid.NewGuid().ToString("N").Substring(20);
        public bool ShouldSkip { get; protected set; }
        public string SkipReason { get; protected set; }
        public abstract IAsyncStorage GetStorage();
    }

    public abstract class StorageFixtureBase<TStorage> : StorageFixtureBase where TStorage : IAsyncStorage
    {
        public TStorage Storage { get; protected set; }
        public override IAsyncStorage GetStorage() => Storage;
    }
}
