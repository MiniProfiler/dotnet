using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using StackExchange.Profiling.Storage;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Understands how to store a <see cref="MiniProfiler"/> to a RavenDb database.
    /// </summary>
    public class RavenDbStorage : IAsyncStorage
    {
        private readonly IDocumentStore _store;
        private readonly bool _waitForReplication;
        private readonly bool _waitForIndexes;

        /// <summary>
        /// Returns a new <see cref="RavenDbStorage"/>.
        /// </summary>
        /// <param name="urls">The ravenDb Urls</param>
        /// <param name="database">The ravenDb Database</param>
        /// <param name="identifier">The identifier for store</param>
        /// <param name="certificate">The client certificate to use for authentication</param>
        /// <param name="waitForIndexes">If it should wait for Indexes after Save changes</param>
        /// <param name="waitForReplication">If it should wait for replication after Save changes</param>
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
                if (type == typeof(MiniProfilerWrapper))
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
            new MiniProfilerWrapper_ByProfileId()
                .Execute(_store);
            
            new MiniProfilerWrapper_ByHasUserViewedAndUser()
                .Execute(_store);
            
            new MiniProfilerWrapper_ByStarted()
                .Execute(_store);
            
            return this;
        }

        /// <summary>
        /// List the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results</param>
        /// <param name="start">Search window start</param>
        /// <param name="finish">Search window end</param>
        /// <param name="orderBy">Result order</param>
        /// <returns>The list of GUID keys</returns>
        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null,
            ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            using var session = _store.OpenSession(new SessionOptions
            {
                NoTracking = true
            });

            var query = session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByStarted>();

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


            return query.Select(x => x.ProfileId).ToList();
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
        /// Stores to <c>profilers</c> under its <see cref="MiniProfiler.Id"/>;
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to save.</param>
        public void Save(MiniProfiler profiler)
        {
            using var session = _store.OpenSession();
            ConfigureWait(session);
            session.Store(new MiniProfilerWrapper(profiler));
            session.SaveChanges();
        }

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public MiniProfiler Load(Guid id)
        {
            using var session = _store.OpenSession(new SessionOptions
            {
                NoTracking = true
            });
            return session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByProfileId>()
                .FirstOrDefault(x => x.ProfileId == id)?.ToMiniProfiler();
        }

        /// <summary>
        /// Sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public void SetUnviewed(string user, Guid id)
        {
            using var session = _store.OpenSession();
            ConfigureWait(session);
            
            var profile = session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByProfileId>()
                .First(x => x.ProfileId == id);
            profile.HasUserViewed = false;
            session.SaveChanges();
        }

        /// <summary>
        /// Sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public void SetViewed(string user, Guid id)
        {
            using var session = _store.OpenSession();
            ConfigureWait(session);
            var profile = session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByProfileId>()
                .First(x => x.ProfileId == id);
            profile.HasUserViewed = true;
            session.SaveChanges();
        }

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.User"/>.</param>
        public List<Guid> GetUnviewedIds(string user)
        {
            using var session = _store.OpenSession(new SessionOptions
            {
                NoTracking = true
            });
            return session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByHasUserViewedAndUser>()
                .Where(x => !x.HasUserViewed &&  x.User == user)
                .Select(x => x.ProfileId)
                .ToList();
        }

        /// <summary>
        /// Asynchronously returns the MiniProfiler Ids for the given search criteria.
        /// </summary>
        /// <param name="maxResults">The max number of results</param>
        /// <param name="start">Search window start</param>
        /// <param name="finish">Search window end</param>
        /// <param name="orderBy">Result order</param>
        /// <returns>The list of GUID keys</returns>
        public async Task<IEnumerable<Guid>> ListAsync(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending)
        {
            using var session = _store.OpenAsyncSession(new SessionOptions
            {
                NoTracking = true
            });

            var query = session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByStarted>();

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
                .Select(x => x.ProfileId)
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
            await session.StoreAsync(new MiniProfilerWrapper(profiler)).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the <c>MiniProfiler</c> identified by 'id' from the database.
        /// </summary>
        /// <param name="id">The profiler ID to load.</param>
        /// <returns>The loaded <see cref="MiniProfiler"/>.</returns>
        public async Task<MiniProfiler> LoadAsync(Guid id) 
        {
            using var session = _store.OpenAsyncSession(new SessionOptions
            {
                NoTracking = true
            });
            return (await session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByProfileId>()
                .FirstOrDefaultAsync(x => x.ProfileId == id)
                .ConfigureAwait(false)).ToMiniProfiler();
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session so it is considered "unviewed"  
        /// </summary>
        /// <param name="user">The user to set this profiler ID as unviewed for.</param>
        /// <param name="id">The profiler ID to set unviewed.</param>
        public async Task SetUnviewedAsync(string user, Guid id)
        {
            using var session = _store.OpenAsyncSession();
            ConfigureWait(session);
            
            var profile = await session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByProfileId>()
                .FirstAsync(x => x.ProfileId == id)
                .ConfigureAwait(false);
            
            profile.HasUserViewed = false;
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously sets a particular profiler session to "viewed"
        /// </summary>
        /// <param name="user">The user to set this profiler ID as viewed for.</param>
        /// <param name="id">The profiler ID to set viewed.</param>
        public async Task SetViewedAsync(string user, Guid id)
        {
            using var session = _store.OpenAsyncSession();
            ConfigureWait(session);
            
            var profile = await session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByProfileId>()
                .FirstAsync(x => x.ProfileId == id)
                .ConfigureAwait(false);
            
            profile.HasUserViewed = true;
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="MiniProfiler.User"/>.</param>
        public async Task<List<Guid>> GetUnviewedIdsAsync(string user)
        {
            using var session = _store.OpenAsyncSession(new SessionOptions
            {
                NoTracking = true
            });
            
            return await session.Query<MiniProfilerWrapper, MiniProfilerWrapper_ByHasUserViewedAndUser>()
                .Where(x => !x.HasUserViewed && x.User == user)
                .Select(x => x.ProfileId)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the <see cref="IDocumentStore"/>.
        /// </summary>
        /// <returns></returns>
        public IDocumentStore GetDocumentStore() => _store;
    }
}