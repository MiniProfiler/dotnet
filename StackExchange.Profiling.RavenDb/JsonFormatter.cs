using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raven.Client.Connection.Profiling;
using Raven.Imports.Newtonsoft.Json;
using Raven.Imports.Newtonsoft.Json.Linq;
using Raven.Json.Linq;

namespace StackExchange.Profiling.RavenDb
{
    internal static class JsonFormatter
    {
        public static RequestResultArgs FormatRequest(RequestResultArgs input)
        {
            return new RequestResultArgs
            {
                DurationMilliseconds = input.DurationMilliseconds,
                At = input.At,
                HttpResult = input.HttpResult,
                Method = input.Method,
                Status = input.Status,
                Url = input.Url,
                PostedData = FilterData(input.PostedData),
                Result = FilterData(input.Result)

            };
        }

        private static string FilterData(string result)
        {
            RavenJToken token;

            try
            {
                token = RavenJToken.Parse(result);
            }
            catch (Exception)
            {
                return result;
            }

            Visit(token);

            return token.ToString(Formatting.Indented);
        }

        private static void Visit(RavenJToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var item in (RavenJObject)token)
                        Visit(item.Value);

                    break;

                case JTokenType.Array:
                    foreach (var items in (RavenJArray)token)
                        Visit(items);

                    break;

                case JTokenType.Constructor:
                case JTokenType.Property:
                case JTokenType.Comment:
                case JTokenType.None:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(token.Type.ToString());
            }
        }
    }
}
