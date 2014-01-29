using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Profiling.Helpers.Dapper
{
    /// <summary>
    /// Help to build out sql
    /// </summary>
    public class SqlBuilder
    {
        Dictionary<string, Clauses> data = new Dictionary<string, Clauses>();
        int seq;

        class Clause
        {
            public string Sql { get; set; }
            public object Parameters { get; set; }
        }

        class Clauses : List<Clause>
        {
            string joiner;
            string prefix;
            string postfix;

            public Clauses(string joiner, string prefix = "", string postfix = "")
            {
                this.joiner = joiner;
                this.prefix = prefix;
                this.postfix = postfix;
            }

            public string ResolveClauses(DynamicParameters p)
            {
                foreach (var item in this)
                {
                    p.AddDynamicParams(item.Parameters);
                }
                return prefix + string.Join(joiner, this.Select(c => c.Sql)) + postfix;
            }
        }

        /// <summary>
        /// Template helper class for SqlBuilder
        /// </summary>
        public class Template
        {
            readonly string sql;
            readonly SqlBuilder builder;
            readonly object initParams;
            int dataSeq = -1; // Unresolved

            /// <summary>
            /// Template constructor
            /// </summary>
            /// <param name="builder"></param>
            /// <param name="sql"></param>
            /// <param name="parameters"></param>
            public Template(SqlBuilder builder, string sql, dynamic parameters)
            {
                this.initParams = parameters;
                this.sql = sql;
                this.builder = builder;
            }

            static readonly System.Text.RegularExpressions.Regex regex =
                new System.Text.RegularExpressions.Regex(@"\/\*\*.+\*\*\/", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline);

            void ResolveSql()
            {
                if (dataSeq != builder.seq)
                {
                    var p = new DynamicParameters(initParams);

                    rawSql = sql;

                    foreach (var pair in builder.data)
                    {
                        rawSql = rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(p));
                    }
                    parameters = p;

                    // replace all that is left with empty
                    rawSql = regex.Replace(rawSql, "");

                    dataSeq = builder.seq;
                }
            }

            string rawSql;
            object parameters;

            /// <summary>
            /// Raw Sql returns by the <see cref="SqlBuilder"/>
            /// </summary>
            public string RawSql { get { ResolveSql(); return rawSql; } }

            /// <summary>
            /// Parameters being used
            /// </summary>
            public object Parameters { get { ResolveSql(); return parameters; } }
        }

        /// <summary>
        /// Add a template to the SqlBuilder
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Template AddTemplate(string sql, dynamic parameters = null)
        {
            return new Template(this, sql, parameters);
        }

        void AddClause(string name, string sql, object parameters, string joiner, string prefix = "", string postfix = "")
        {
            Clauses clauses;
            if (!data.TryGetValue(name, out clauses))
            {
                clauses = new Clauses(joiner, prefix, postfix);
                data[name] = clauses;
            }
            clauses.Add(new Clause { Sql = sql, Parameters = parameters });
            seq++;
        }


        /// <summary>
        /// Add a Left Join
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder LeftJoin(string sql, dynamic parameters = null)
        {
            AddClause("leftjoin", sql, parameters, joiner: "\nLEFT JOIN ", prefix: "\nLEFT JOIN ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// Add a Where Clause
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder Where(string sql, dynamic parameters = null)
        {
            AddClause("where", sql, parameters, " AND ", prefix: "WHERE ", postfix: "\n");
            return this;
        }
        
        /// <summary>
        /// Add an OrderBy Clause
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder OrderBy(string sql, dynamic parameters = null)
        {
            AddClause("orderby", sql, parameters, " , ", prefix: "ORDER BY ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// Add the Select section
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder Select(string sql, dynamic parameters = null)
        {
            AddClause("select", sql, parameters, " , ", prefix: "", postfix: "\n");
            return this;
        }

        /// <summary>
        /// Add a parameters to the query
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder AddParameters(dynamic parameters)
        {
            AddClause("--parameters", "", parameters, "");
            return this;
        }

        /// <summary>
        /// Add a Join statement
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder Join(string sql, dynamic parameters = null)
        {
            AddClause("join", sql, parameters, joiner: "\nJOIN ", prefix: "\nJOIN ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// Add a Group By section
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder GroupBy(string sql, dynamic parameters = null)
        {
            AddClause("groupby", sql, parameters, joiner: " , ", prefix: "\nGROUP BY ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// Add a Having section
        /// </summary>
        /// <returns>itself</returns>
        public SqlBuilder Having(string sql, dynamic parameters = null)
        {
            AddClause("having", sql, parameters, joiner: "\nAND ", prefix: "HAVING ", postfix: "\n");
            return this;
        }
    }
}
