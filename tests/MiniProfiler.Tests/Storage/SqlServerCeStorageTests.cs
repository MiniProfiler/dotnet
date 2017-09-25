#if NET452 || NET46
using System;
using System.Data.SqlServerCe;
using System.IO;
using StackExchange.Profiling.Storage;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Profiling.Tests.Storage
{
    public class SqlServerCeStorageTests : StorageBaseTest, IClassFixture<SqlServerCeStorageFixture>
    {
        public SqlServerCeStorageTests(SqlServerCeStorageFixture fixture, ITestOutputHelper output) : base(fixture, output)
        {
        }
    }

    public class SqlServerCeStorageFixture : StorageFixtureBase<SqlServerCeStorage>, IDisposable
    {
        public SqlServerCeStorageFixture()
        {
            Skip.IfNoConfig(nameof(TestConfig.Current.SQLServerCeConnectionString), TestConfig.Current.SQLServerCeConnectionString);

            var connString = TestConfig.Current.SQLServerCeConnectionString;
            var csb = new SqlCeConnectionStringBuilder(connString);
            var filename = csb.DataSource.Replace("|DataDirectory|", AppDomain.CurrentDomain.GetData("DataDirectory").ToString());

            Storage = new SqlServerCeStorage(
                connString,
                "MPTest" + TestId,
                "MPTimingsTest" + TestId,
                "MPClientTimingsTest" + TestId);
            try
            {
                try
                {
                    File.Delete(filename);
                }
                catch { /* expected */ }

                var engine = new SqlCeEngine(connString);
                engine.CreateDatabase();
                Storage.CreateSchema();
            }
            catch (Exception e)
            {
                ShouldSkip = true;
                SkipReason = e.Message;
            }
        }

        public void Dispose()
        {
            Storage.DropSchema();
        }
    }
}
#endif
