using System;
using MongoDB.Driver;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledMongoServer : MongoServer
    {
        private readonly static object __staticLock = new object();
        private readonly static Dictionary<MongoServerSettings, ProfiledMongoServer> __servers = new Dictionary<MongoServerSettings, ProfiledMongoServer>();
        private static int __maxServerCount = 100;

        public ProfiledMongoServer(MongoServerSettings settings) : base(settings)
        {
        }

        public static ProfiledMongoServer Create(MongoClient client)
        {
            MongoServerSettings settings = MongoServerSettings.FromClientSettings(client.Settings);
            lock (__staticLock)
            {
                ProfiledMongoServer server;
                if (!__servers.TryGetValue(settings, out server))
                {
                    if (__servers.Count >= __maxServerCount)
                    {
                        var message = string.Format("ProfiledMongoServer.Create has already created {0} servers which is the maximum number of servers allowed.", __maxServerCount);
                        throw new MongoException(message);
                    }
                    server = new ProfiledMongoServer(settings);
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
