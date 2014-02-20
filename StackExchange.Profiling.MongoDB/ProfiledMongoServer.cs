using System;
using MongoDB.Driver;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledMongoServer : MongoServer
    {
        public ProfiledMongoServer(MongoServerSettings settings) : base(settings)
        {
        }

        public ProfiledMongoServer(MongoServer server)
            : base (server.Settings)
        {
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
