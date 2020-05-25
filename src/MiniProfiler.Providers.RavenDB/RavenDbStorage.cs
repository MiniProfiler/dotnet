using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using StackExchange.Profiling.Storage.Internal;

namespace StackExchange.Profiling.Storage
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a RavenDB database.
    /// </summary>
    public class RavenDbStorage : IAsyncStorage
    {
        private readonly IDocumentStore _store;
        private readonly bool _waitForReplication;
        private readonly bool _waitForIndexes;

        /// <summary>
        /// Returns a new <see cref="RavenDbStorage"/>.
        /// </summary>
        /// <param name="urls">The RavenDB Urls.</param>
        /// <param name="database">The RavenDB database name.</param>
        /// <param name="identifier">The identifier for store.</param>
        /// <param name="certificate">The client certificate to use for authentication.</param>
        /// <param name="waitForIndexes">Whether to wait for indexes after save.</param>
        /// <param name="waitForReplication">whether to wait for replication after save.</param>
        public RavenDbStorage(string[] urls, string database, string identifier = "mini-profiler",
            X509Certificate2 certificate = null, bool waitForIndexes = false, bool waitForReplication = false)
        {
            _waitForReplication = waitForReplication;
            _waitForIndexes = waitForIndexes;
            _store = new DocumentStore
            {
                Urls = urls,
                Database = database,
                Identifier = identifier,
                Certificate = certificate
            };
            _store.Conventions.FindCollectionName = type =>
            {
                if (type == typeof(MiniProfilerDoc))
                {
                    return nameof(MiniProfiler);
                }

                return DocumentConventions.DefaultGetCollectionName(type);
            };
            _store.Initialize();
            WithIndexCreation();
        }


        /// <summary>
        /// Creates indexes for faster querying.
        /// </summary>
        public RavenDbStorage WithIndexCreation()
        {
            new Index_ByProfilerId().Execute(_store);
            new Index_ByHasUserViewedAndUser().Execute(_store);
            new Index_ByStarted().Execute(_store);

            return this;
        }

        /// <summary>
        /// List the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results.</param>
        /// <param name="start">Search window start time (inclusive).</param>
        /// <param name="finish">Search window end time (inclusive).</param>
        /// <param name="orderBy">Result order.</param>
        /// <returns>A list of GUID keys matching the search.</returns>
        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            using var session = _store.OpenSession(new SessionOptions
            {
                NoTracking = true
            });

            var query = session.Query<MiniProfilerDoc, Index_ByStarted>();

            if (start.HasValue)
            {
                query = query.Where(x => x.Started >= start.Value);
            }

            if (finish.HasValue)
            {
                query = query.Where(x => x.Started <= finish.Value);
            }

            query.Take(maxResults);

            query = orderBy == ListResultsOrder.Descending
                ? query.OrderByDescending(x => x.Started)
                : query.OrderBy(x => x.Started);


            return query.Select(x => x.ProfilerId).ToList();
        }

        private void ConfigureWait(IDocumentSession session)
        {
            if (_waitForIndexes)
            {
                session.Advanced.WaitForIndexesAfterSaveChanges(throwOnTimeout: false);
            }

            if (_waitForReplication)
            {
                session.Advanced.WaitForReplicationAfterSaveChanges(throwOnTimeout: false);
            }
        }

        private void ConfigureWait(IAsyncDocumentSession session)
        {
            if (_waitForIndexes)
            {
                session.Advanced.WaitForIndexesAfterSaveChanges(throwOnTimeout: false);
            }

            if (_waitForReplication)
            {
                session.Advanced.WaitForReplicationAfterSaveChanges(throwOnTimeout: false);
            }
        }

        /// <summary>
        /// Stores to <c>profilers</c> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public void Save(MiniProfiler profiler)
        {
            using var session = _store.OpenSession();
            ConfigureWait(session);
            session.Store(new MiniProfilerDoc(profiler));
            session.SaveChanges();
        }

        /// <summary>
        /// Loads the <see cref="MiniProfiler"/> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public MiniProfiler Load(Guid id)
        {
            using var session = _store.OpenSession(new SessionOptions
            {
                NoTracking = true
            });
            return session.Query<MiniProfilerDoc, Index_ByProfilerId>()
                .FirstOrDefault(x => x.ProfilerId == id)?.ToMiniProfiler();
        }

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed".
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public void SetUnviewed(string user, Guid id)
        {
            using var session = _store.OpenSession();
            ConfigureWait(session);

            var profile = session.Query<MiniProfilerDoc, Index_ByProfilerId>()
                .First(x => x.ProfilerId == id);
            profile.HasUserViewed = false;
            session.SaveChanges();
        }

        /// <summary>
        /// Sets a particular profiler session to "viewed".
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public void SetViewed(string user, Guid id)
        {
            using var session = _store.OpenSession();
            ConfigureWait(session);
            var profile = session.Query<MiniProfilerDoc, Index_ByProfilerId>()
                .First(x => x.ProfilerId == id);
            profile.HasUserViewed = true;
            session.SaveChanges();
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User to get IDs for, identified by <see cref="MiniProfiler.User"/>.</param>
        public List<Guid> GetUnviewedIds(string user)
        {
            using var session = _store.OpenSession(new SessionOptions
            {
                NoTracking = true
            });
            return session.Query<MiniProfilerDoc, Index_ByHasUserViewedAndUser>()
                .Where(x => !x.HasUserViewed && x.User == user)
                .Select(x => x.ProfilerId)
                .ToList();
        }

        /// <summary>
        /// Asynchronously returns the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results.</param>
        /// <param name="start">Search window start time (inclusive).</param>
        /// <param name="finish">Search window end time (inclusive).</param>
        /// <param name="orderBy">Result order.</param>
        /// <returns>A list of GUID keys matching the search.</returns>
        public async Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            using var session = _store.OpenAsyncSession(new SessionOptions
            {
                NoTracking = true
            });

            var query = session.Query<MiniProfilerDoc, Index_ByStarted>();

            if (start.HasValue)
            {
                query = query.Where(x => x.Started >= start.Value);
            }

            if (finish.HasValue)
            {
                query = query.Where(x => x.Started <= finish.Value);
            }

            query.Take(maxResults);

            query = orderBy == ListResultsOrder.Descending
                ? query.OrderByDescending(x => x.Started)
                : query.OrderBy(x => x.Started);


            return await query
                .Select(x => x.ProfilerId)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously stores to <c>profilers</c> under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public async Task SaveAsync(MiniProfiler profiler)
        {
            using var session = _store.OpenAsyncSession();
            ConfigureWait(session);
            await session.StoreAsync(new MiniProfilerDoc(profiler)).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the <see cref="MiniProfiler"/> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public async Task<MiniProfiler> LoadAsync(Guid id)
        {
            using var session = _store.OpenAsyncSession(new SessionOptions
            {
                NoTracking = true
            });
            return (await session.Query<MiniProfilerDoc, Index_ByProfilerId>()
                .FirstOrDefaultAsync(x => x.ProfilerId == id)
                .ConfigureAwait(false)).ToMiniProfiler();
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "unviewed". 
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public async Task SetUnviewedAsync(string user, Guid id)
        {
            using var session = _store.OpenAsyncSession();
            ConfigureWait(session);

            var profile = await session.Query<MiniProfilerDoc, Index_ByProfilerId>()
                .FirstAsync(x => x.ProfilerId == id)
                .ConfigureAwait(false);

            profile.HasUserViewed = false;
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed".
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public async Task SetViewedAsync(string user, Guid id)
        {
            using var session = _store.OpenAsyncSession();
            ConfigureWait(session);

            var profile = await session.Query<MiniProfilerDoc, Index_ByProfilerId>()
                .FirstAsync(x => x.ProfilerId == id)
                .ConfigureAwait(false);

            profile.HasUserViewed = true;
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User to get IDs for, identified by <see cref="MiniProfiler.User"/>.</param>
        public async Task<List<Guid>> GetUnviewedIdsAsync(string user)
        {
            using var session = _store.OpenAsyncSession(new SessionOptions
            {
                NoTracking = true
            });

            return await session.Query<MiniProfilerDoc, Index_ByHasUserViewedAndUser>()
                .Where(x => !x.HasUserViewed && x.User == user)
                .Select(x => x.ProfilerId)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the underlying <see cref="IDocumentStore"/>.
        /// </summary>
        public IDocumentStore GetDocumentStore() => _store;
    }
}
