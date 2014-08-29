using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledMongoServer : MongoServer
    {
        private readonly static object __staticLock = new object();
        private readonly static Dictionary<MongoServerSettings, ProfiledMongoServer> __servers = new Dictionary<MongoServerSettings, ProfiledMongoServer>();
        private static int __maxServerCount = MongoMiniProfiler.Settings.MaxServerCount;

        [Obsolete("This may leak server connections, use ProfiledMongoServer.Create instead.")]
        public ProfiledMongoServer(MongoServerSettings settings) : base(settings)
        {
        }

        [Obsolete("This may leak server connections, use ProfiledMongoServer.Create instead.")]
        public ProfiledMongoServer(MongoServer server) : base (server.Settings)
        {
        }

        public static ProfiledMongoServer Create(MongoClient client)
        {
            return Create(MongoServerSettings.FromClientSettings(client.Settings));
        }

        public static new ProfiledMongoServer Create(MongoServerSettings settings)
        {
            lock (__staticLock)
            {
                ProfiledMongoServer server;
                if (!__servers.TryGetValue(settings, out server))
                {
                    if (__servers.Count >= __maxServerCount)
                    {
                        var message = string.Format("ProfiledMongoServer.Create has already created {0} servers which is the maximum number of servers allowed.", __maxServerCount);
                        throw new Exception(message);
                    }
#pragma warning disable 618
                    server = new ProfiledMongoServer(settings);
#pragma warning restore
                    __servers.Add(settings, server);
                }
                return server;
            }
        }

        public override MongoDatabase GetDatabase(string databaseName, MongoDatabaseSettings databaseSettings)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            if (databaseSettings == null)
            {
                throw new ArgumentNullException("databaseSettings");
            }
            return new ProfiledMongoDatabase(this, databaseName, databaseSettings);
        }
    }
}
