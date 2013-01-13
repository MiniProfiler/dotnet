namespace Dapper
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The SQL builder.
    /// </summary>
    internal class SqlBuilder
    {
        /// <summary>
        /// The data.
        /// </summary>
        private readonly Dictionary<string, Clauses> _data = new Dictionary<string, Clauses>();

        /// <summary>
        /// The sequence.
        /// </summary>
        private int _seq;

        /// <summary>
        /// The SQL clause.
        /// </summary>
        internal class Clause
        {
            /// <summary>
            /// Gets or sets the SQL.
            /// </summary>
            public string Sql { get; set; }

            /// <summary>
            /// Gets or sets the parameters.
            /// </summary>
            public object Parameters { get; set; }
        }

        /// <summary>
        /// The clauses.
        /// </summary>
        internal class Clauses : List<Clause>
        {
            /// <summary>
            /// The _joiner.
            /// </summary>
            private readonly string _joiner;

            /// <summary>
            /// The prefix.
            /// </summary>
            private readonly string _prefix;

            /// <summary>
            /// The postfix.
            /// </summary>
            private readonly string _postfix;

            /// <summary>
            /// Initialises a new instance of the <see cref="Clauses"/> class.
            /// </summary>
            /// <param name="joiner">The joiner expression.</param>
            /// <param name="prefix">The prefix expression.</param>
            /// <param name="postfix">The postfix expression.</param>
            public Clauses(string joiner, string prefix = "", string postfix = "")
            {
                this._joiner = joiner;
                this._prefix = prefix;
                this._postfix = postfix;
            }

            /// <summary>
            /// resolve the clauses.
            /// </summary>
            /// <param name="parameters">The parameters.</param>
            /// <returns>The <see cref="string"/>.</returns>
            public string ResolveClauses(DynamicParameters parameters)
            {
                foreach (var item in this)
                {
                    parameters.AddDynamicParams(item.Parameters);
                }
                return this._prefix + string.Join(this._joiner, this.Select(c => c.Sql)) + this._postfix;
            }
        }

        /// <summary>
        /// The template.
        /// </summary>
        public class Template
        {
            /// <summary>
            /// The regular expression.
            /// </summary>
            private static readonly System.Text.RegularExpressions.Regex Regex =
                new System.Text.RegularExpressions.Regex(@"\/\*\*.+\*\*\/", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline);

            /// <summary>
            /// The SQL statement.
            /// </summary>
            private readonly string _sql;

            /// <summary>
            /// The builder.
            /// </summary>
            private readonly SqlBuilder _builder;

            /// <summary>
            /// The initialisation parameters.
            /// </summary>
            private readonly object _initParams;

            /// <summary>
            /// The data sequence.
            /// </summary>
            private int _dataSeq = -1; // Unresolved

            /// <summary>
            /// The raw SQL.
            /// </summary>
            private string _rawSql;

            /// <summary>
            /// The _parameters.
            /// </summary>
            private object _parameters;

            /// <summary>
            /// Initialises a new instance of the <see cref="Template"/> class.
            /// </summary>
            /// <param name="builder">The builder.</param>
            /// <param name="sql">The SQL.</param>
            /// <param name="parameters">The parameters.</param>
            public Template(SqlBuilder builder, string sql, dynamic parameters)
            {
                this._initParams = parameters;
                this._sql = sql;
                this._builder = builder;
            }

            /// <summary>
            /// Gets the raw SQL.
            /// </summary>
            public string RawSql
            {
                get
                {
                    this.ResolveSql();
                    return this._rawSql;
                }
            }

            /// <summary>
            /// Gets the parameters.
            /// </summary>
            public object Parameters
            {
                get
                {
                    this.ResolveSql();
                    return this._parameters;
                }
            }

            /// <summary>
            /// resolve the SQL.
            /// </summary>
            private void ResolveSql()
            {
                if (this._dataSeq != this._builder._seq)
                {
                    var p = new DynamicParameters(this._initParams);

                    this._rawSql = this._sql;

                    foreach (var pair in this._builder._data)
                    {
                        this._rawSql = this._rawSql.Replace("/**" + pair.Key + "**/", pair.Value.ResolveClauses(p));
                    }
                    this._parameters = p;

                    // replace all that is left with empty
                    this._rawSql = Regex.Replace(this._rawSql, string.Empty);

                    this._dataSeq = this._builder._seq;
                }
            }
        }

        /// <summary>
        /// add the template.
        /// </summary>
        /// <param name="sql">The template SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="Template"/>.</returns>
        public Template AddTemplate(string sql, dynamic parameters = null)
        {
            return new Template(this, sql, parameters);
        }

        /// <summary>
        /// add the clause.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="joiner">The joiner.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="postfix">The postfix expression.</param>
        private void AddClause(string name, string sql, object parameters, string joiner, string prefix = "", string postfix = "")
        {
            Clauses clauses;
            if (!this._data.TryGetValue(name, out clauses))
            {
                clauses = new Clauses(joiner, prefix, postfix);
                this._data[name] = clauses;
            }
            clauses.Add(new Clause { Sql = sql, Parameters = parameters });
            this._seq++;
        }

        /// <summary>
        /// The left join.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="SqlBuilder"/>.</returns>
        public SqlBuilder LeftJoin(string sql, dynamic parameters = null)
        {
            AddClause("leftjoin", sql, parameters, joiner: "\nLEFT JOIN ", prefix: "\nLEFT JOIN ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// The where.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="SqlBuilder"/>.</returns>
        public SqlBuilder Where(string sql, dynamic parameters = null)
        {
            AddClause("where", sql, parameters, " AND ", prefix: "WHERE ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// The order by.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="SqlBuilder"/>.</returns>
        public SqlBuilder OrderBy(string sql, dynamic parameters = null)
        {
            AddClause("orderby", sql, parameters, " , ", prefix: "ORDER BY ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// The select.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="SqlBuilder"/>.</returns>
        public SqlBuilder Select(string sql, dynamic parameters = null)
        {
            AddClause("select", sql, parameters, " , ", prefix: string.Empty, postfix: "\n");
            return this;
        }

        /// <summary>
        /// The add parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="SqlBuilder"/>.</returns>
        public SqlBuilder AddParameters(dynamic parameters)
        {
            AddClause("--parameters", string.Empty, parameters, string.Empty);
            return this;
        }

        /// <summary>
        /// The join.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="SqlBuilder"/>.</returns>
        public SqlBuilder Join(string sql, dynamic parameters = null)
        {
            AddClause("join", sql, parameters, joiner: "\nJOIN ", prefix: "\nJOIN ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// The group by.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="SqlBuilder"/>.</returns>
        public SqlBuilder GroupBy(string sql, dynamic parameters = null)
        {
            AddClause("groupby", sql, parameters, joiner: " , ", prefix: "\nGROUP BY ", postfix: "\n");
            return this;
        }

        /// <summary>
        /// The having.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The <see cref="SqlBuilder"/>.</returns>
        public SqlBuilder Having(string sql, dynamic parameters = null)
        {
            AddClause("having", sql, parameters, joiner: "\nAND ", prefix: "HAVING ", postfix: "\n");
            return this;
        }
    }
}
