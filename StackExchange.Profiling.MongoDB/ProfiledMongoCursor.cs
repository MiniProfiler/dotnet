using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using StackExchange.Profiling.MongoDB.Utils;

namespace StackExchange.Profiling.MongoDB
{
    public class ProfiledMongoCursor<TDocument> : MongoCursor<TDocument>
    {
        public ProfiledMongoCursor(MongoCollection collection, IMongoQuery query, ReadPreference readPreference,
            IBsonSerializer serializer, IBsonSerializationOptions serializationOptions)
            : base(collection, query, readPreference, serializer, serializationOptions)
        {
        }

        public override BsonDocument Explain()
        {
            var sw = new Stopwatch();

            sw.Start();
            var result = base.Explain();
            sw.Stop();

            string commandString = Query != null
                ? string.Format("{0}.find(query).explain()\n\nquery = {1}", Collection.Name, Query.ToBsonDocument())
                : string.Format("{0}.find().explain()", Collection.Name);

            ProfilerUtils.AddMongoTiming(commandString, sw.ElapsedMilliseconds, ExecuteType.Command);

            return result;
        }

        public override IEnumerator<TDocument> GetEnumerator()
        {
            var underlyingEnumerator = base.GetEnumerator();
            var profiledEnumerator = new ProfiledEnumerator<TDocument>(underlyingEnumerator);

            profiledEnumerator.EnumerationEnded += ProfiledEnumeratorOnEnumerationEnded;

            return profiledEnumerator;
        }

        private void ProfiledEnumeratorOnEnumerationEnded(object sender, ProfiledEnumerator<TDocument>.EnumerationEndedEventArgs enumerationEndedEventArgs)
        {
            BsonValue hint, orderBy;

            var hasHint = Options.TryGetValue("$hint", out hint);
            var hasOrderBy = Options.TryGetValue("$orderby", out orderBy);

            var commandStringBuilder = new StringBuilder(1024);

            commandStringBuilder.Append(Collection.Name);
            commandStringBuilder.Append(".find(");

            if (Query != null)
                commandStringBuilder.Append("query");

            if (Fields != null)
                commandStringBuilder.Append(",fields");

            commandStringBuilder.Append(")");

            if (hasOrderBy)
                commandStringBuilder.Append(".sort(orderBy)");

            if (hasHint)
                commandStringBuilder.Append(".hint(hint)");

            if (Skip != 0)
                commandStringBuilder.AppendFormat(".skip({0})", Skip);

            if (Limit != 0)
                commandStringBuilder.AppendFormat(".limit({0})", Limit);

            if (Query != null)
                commandStringBuilder.AppendFormat("\nquery = {0}", Query.ToBsonDocument());

            if (Fields != null)
                commandStringBuilder.AppendFormat("\nfields = {0}", Fields.ToBsonDocument());

            if (hasOrderBy)
                commandStringBuilder.AppendFormat("\norderBy = {0}", orderBy.ToBsonDocument());

            if (hasHint)
                commandStringBuilder.AppendFormat("\nhint = {0}", hint.ToBsonDocument());

            // TODO: implement other options printout if needed

            string commandString = commandStringBuilder.ToString();

            ProfilerUtils.AddMongoTiming(commandString, enumerationEndedEventArgs.ElapsedMilliseconds, ExecuteType.Read);
        }
    }
}
