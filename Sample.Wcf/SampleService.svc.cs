namespace Sample.Wcf
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Threading;

    using Dapper;

    using StackExchange.Profiling;

    /// <summary>
    /// The sample service.
    /// NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    /// </summary>
    public class SampleService : ISampleService
    {
        /// <summary>
        /// Returns an open connection that will have its queries profiled.
        /// </summary>
        /// <param name="profiler">
        /// The profiler.
        /// </param>
        /// <returns>the abstracted connection</returns>
        public static DbConnection GetConnection(MiniProfiler profiler = null)
        {
            using (profiler.Step("GetOpenConnection"))
            {
                DbConnection cnn = new System.Data.SQLite.SQLiteConnection(WcfCommon.ConnectionString);

                // to get profiling times, we have to wrap whatever connection we're using in a ProfiledDbConnection
                // when MiniProfiler.Current is null, this connection will not record any database timings
                if (MiniProfiler.Current != null)
                {
                    cnn = new StackExchange.Profiling.Data.ProfiledDbConnection(cnn, MiniProfiler.Current);
                }

                cnn.Open();
                return cnn;
            }
        }

        /// <summary>
        /// fetch the route hits.
        /// </summary>
        /// <returns>the set of route hits.</returns>
        public IEnumerable<RouteHit> FetchRouteHits()
        {
            var profiler = MiniProfiler.Current;
            using (profiler.Step("Do more complex stuff"))
            {
                Thread.Sleep(new Random().Next(100, 400));
            }

            using (profiler.Step("FetchRouteHits"))
            using (var conn = GetConnection(profiler))
            {
                var result = conn.Query<RouteHit>("select RouteName, HitCount from RouteHits order by RouteName");
                return result.ToList();
            }
        }

        /// <summary>
        /// The service method that is not profiled.
        /// </summary>
        /// <returns>a method that is not profiled.</returns>
        public string ServiceMethodThatIsNotProfiled()
        {
            MiniProfiler.Stop(true);

            return "Result";
        }

        /// <summary>
        /// The massive nesting.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public string MassiveNesting()
        {
            var i = 0;
            using (var conn = GetConnection())
            {
                RecursiveMethod(ref i, conn, MiniProfiler.Current);
            }
            return "MassiveNesting completed";
        }

        /// <summary>
        /// The second massive nesting
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public string MassiveNesting2()
        {
            for (int i = 0; i < 6; i++)
            {
                MassiveNesting();
            }
            return "MassiveNesting2 completed";
        }

        /// <summary>
        /// duplicated statements.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public string Duplicated()
        {
            using (var conn = GetConnection())
            {
                long total = 0;

                for (int i = 0; i < 20; i++)
                {
                    total += conn.Query<long>("select count(1) from RouteHits where HitCount = @i", new { i }).First();
                }
                return string.Format("Duplicate queries completed: {0}", total);
            }
        }

        /// <summary>
        /// a recursive method.
        /// </summary>
        /// <param name="recursiveDepth">The recursive depth</param>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        private void RecursiveMethod(ref int recursiveDepth, DbConnection connection, MiniProfiler profiler)
        {
            Thread.Sleep(5); // ensure we show up in the profiler

            if (recursiveDepth >= 10) return;

            using (profiler.Step("Nested call " + recursiveDepth))
            {
                // run some meaningless queries to illustrate formatting
                connection.Query(
                    @"select *
                    from   MiniProfilers
                    where  Name like @name
                            or Name = @name
                            or DurationMilliseconds >= @duration
                            or HasSqlTimings = @hasSqlTimings
                            or Started > @yesterday ",
                    new
                        {
                            name = "Home/Index",
                                     duration = 100.5,
                                     hasSqlTimings = true,
                                     yesterday = DateTime.UtcNow.AddDays(-1)
                                 });

                connection.Query(@"select RouteName, HitCount from RouteHits where HitCount < 100000000 or HitCount > 0 order by HitCount, RouteName -- this should hopefully wrap");

                // massive query to test if max-height is properly removed from <pre> stylings
                connection.Query(
                        @"select *
                        from   (select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 0 and 9
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 10 and 19
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 20 and 29
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 30 and 39
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 40 and 49
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 50 and 59
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 60 and 69
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 70 and 79
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 80 and 89
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount between 90 and 99
                                union all
                                select RouteName,
                                       HitCount
                                from   RouteHits
                                where  HitCount > 100)
                        order  by RouteName");
                                         
                // need a long title to test max-width
                using (profiler.Step("Incrementing a reference parameter named i"))
                {
                    recursiveDepth++;
                }
                RecursiveMethod(ref recursiveDepth, connection, profiler);
            }
        }
    }
}
