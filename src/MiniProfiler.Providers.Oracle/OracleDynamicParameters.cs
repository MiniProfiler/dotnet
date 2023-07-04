using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Handle Parameters for Oracle Storage Provider
    /// </summary>
    public class OracleDynamicParameters : SqlMapper.IDynamicParameters
    {
        private static readonly Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> _paramReaderCache = new Dictionary<SqlMapper.Identity, Action<IDbCommand, object>>();

        private readonly Dictionary<string, ParamInfo> _parameters = new Dictionary<string, ParamInfo>();

        private List<object> _templates;

        private sealed class ParamInfo
        {

            public string Name { get; set; }

            public object Value { get; set; }

            public ParameterDirection ParameterDirection { get; set; }

            public OracleDbType? DbType { get; set; }

            public int? Size { get; set; }

            public IDbDataParameter AttachedParam { get; set; }
        }

        /// <summary>
        /// construct a dynamic parameter bag
        /// </summary>
        public OracleDynamicParameters() { }

        /// <summary>
        /// construct a dynamic parameter bag
        /// </summary>
        /// <param name="template">can be an anonymous type or a DynamicParameters bag</param>
        public OracleDynamicParameters(object template) => AddDynamicParams(template);

        /// <summary>
        /// Append a whole object full of params to the dynamic
        /// EG: AddDynamicParams(new {A = 1, B = 2}) // will add property A and B to the dynamic
        /// </summary>
        /// <param name="param"></param>
        public void AddDynamicParams(
#if CSHARP30
		object param
#else
             dynamic param
#endif
     )
        {
            if (param is object obj)
            {
                if (obj is not OracleDynamicParameters subDynamic)
                {
                    if (obj is not IEnumerable<KeyValuePair<string, object>> dictionary)
                    {
                        _templates ??= new List<object>();
                        _templates.Add(obj);
                    }
                    else
                    {
                        foreach (var kvp in dictionary)
                        {
#if CSHARP30
						Add(kvp.Key, kvp.Value, null, null, null);
#else
                            Add(kvp.Key, kvp.Value);
#endif
                        }
                    }
                }
                else
                {
                    if (subDynamic._parameters != null)
                    {
                        foreach (var kvp in subDynamic._parameters)
                        {
                            _parameters.Add(kvp.Key, kvp.Value);
                        }
                    }

                    if (subDynamic._templates != null)
                    {
                        _templates ??= new List<object>();
                        foreach (var t in subDynamic._templates)
                        {
                            _templates.Add(t);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a parameter to this dynamic parameter list
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        /// <param name="direction"></param>
        /// <param name="size"></param>
        public void Add(
#if CSHARP30
		string name, object value, DbType? dbType, ParameterDirection? direction, int? size
#else
            string name, object value = null, OracleDbType? dbType = null, ParameterDirection? direction = null, int? size = null
#endif
        ) => _parameters[Clean(name)] = new ParamInfo() { Name = name, Value = value, ParameterDirection = direction ?? ParameterDirection.Input, DbType = dbType, Size = size };

        private static string Clean(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                switch (name[0])
                {
                    case '@':
                    case ':':
                    case '?':
                        return name.Substring(1);
                }
            }
            return name;
        }

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity) => AddParameters(command, identity);

        /// <summary>
        /// Add all the parameters needed to the command just before it executes
        /// </summary>
        /// <param name="command">The raw command prior to execution</param>
        /// <param name="identity">Information about the query</param>
        protected void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            if (_templates != null)
            {
                foreach (var template in _templates)
                {
                    var newIdent = identity.ForDynamicParameters(template.GetType());
                    Action<IDbCommand, object> appender;

                    lock (_paramReaderCache)
                    {
                        if (!_paramReaderCache.TryGetValue(newIdent, out appender))
                        {
                            appender = SqlMapper.CreateParamInfoGenerator(newIdent, false, false);
                            _paramReaderCache[newIdent] = appender;
                        }
                    }

                    appender(command, template);
                }
            }

            foreach (var param in _parameters.Values)
            {
                string name = Clean(param.Name);
                bool add = !((OracleCommand)command).Parameters.Contains(name);
                OracleParameter p;
                if (add)
                {
                    p = ((OracleCommand)command).CreateParameter();
                    p.ParameterName = name;
                }
                else
                {
                    p = ((OracleCommand)command).Parameters[name];
                }
                var val = param.Value;
                p.Value = val ?? DBNull.Value;
                p.Direction = param.ParameterDirection;
                if (val is string s && s.Length <= 4000)
                {
                    p.Size = 4000;
                }
                if (param.Size != null)
                {
                    p.Size = param.Size.Value;
                }
                if (param.DbType != null)
                {
                    p.OracleDbType = param.DbType.Value;
                }
                if (add)
                {
                    command.Parameters.Add(p);
                }
                param.AttachedParam = p;
            }
        }

        /// <summary>
        /// All the names of the param in the bag, use Get to yank them out
        /// </summary>
        public IEnumerable<string> ParameterNames => _parameters.Select(p => p.Key);

        /// <summary>
        /// Get the value of a parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns>The value, note DBNull.Value is not returned, instead the value is returned as null</returns>
        public T Get<T>(string name)
        {
            var val = _parameters[Clean(name)].AttachedParam.Value;
            if (val == DBNull.Value)
            {
                if (default(T) != null)
                {
                    throw new InvalidCastException("Attempting to cast a DBNull to a non nullable type!");
                }
                return default;
            }
            return (T)val;
        }
    }
}
