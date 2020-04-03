using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

using StackExchange.Profiling.Internal;
using StackExchange.Profiling.SqlFormatters;
using Xunit;
#if NET461
using System.Transactions;
#endif

namespace StackExchange.Profiling.Tests
{
    public class SqlFormatterTests
    {
        private static Dictionary<RuntimeTypeHandle, DbType> _dbTypeMap = new Dictionary<RuntimeTypeHandle, DbType>
        {
            [typeof(byte).TypeHandle] = DbType.Byte,
            [typeof(sbyte).TypeHandle] = DbType.SByte,
            [typeof(short).TypeHandle] = DbType.Int16,
            [typeof(ushort).TypeHandle] = DbType.UInt16,
            [typeof(int).TypeHandle] = DbType.Int32,
            [typeof(uint).TypeHandle] = DbType.UInt32,
            [typeof(long).TypeHandle] = DbType.Int64,
            [typeof(ulong).TypeHandle] = DbType.UInt64,
            [typeof(float).TypeHandle] = DbType.Single,
            [typeof(double).TypeHandle] = DbType.Double,
            [typeof(decimal).TypeHandle] = DbType.Decimal,
            [typeof(bool).TypeHandle] = DbType.Boolean,
            [typeof(string).TypeHandle] = DbType.String,
            [typeof(char).TypeHandle] = DbType.StringFixedLength,
            [typeof(Guid).TypeHandle] = DbType.Guid,
            [typeof(DateTime).TypeHandle] = DbType.DateTime,
            [typeof(DateTimeOffset).TypeHandle] = DbType.DateTimeOffset,
            [typeof(byte[]).TypeHandle] = DbType.Binary,
            [typeof(byte?).TypeHandle] = DbType.Byte,
            [typeof(sbyte?).TypeHandle] = DbType.SByte,
            [typeof(short?).TypeHandle] = DbType.Int16,
            [typeof(ushort?).TypeHandle] = DbType.UInt16,
            [typeof(int?).TypeHandle] = DbType.Int32,
            [typeof(uint?).TypeHandle] = DbType.UInt32,
            [typeof(long?).TypeHandle] = DbType.Int64,
            [typeof(ulong?).TypeHandle] = DbType.UInt64,
            [typeof(float?).TypeHandle] = DbType.Single,
            [typeof(double?).TypeHandle] = DbType.Double,
            [typeof(decimal?).TypeHandle] = DbType.Decimal,
            [typeof(bool?).TypeHandle] = DbType.Boolean,
            [typeof(char?).TypeHandle] = DbType.StringFixedLength,
            [typeof(Guid?).TypeHandle] = DbType.Guid,
            [typeof(DateTime?).TypeHandle] = DbType.DateTime,
            [typeof(DateTimeOffset?).TypeHandle] = DbType.DateTimeOffset
        };
        private static DbType GetDbType(Type type) => _dbTypeMap[type.TypeHandle];

        private const string None = "";
        private const string At = "@";
        public static IEnumerable<object[]> GetParamPrefixes()
        {
            yield return new object[] { None };
            yield return new object[] { At };
        }

        private SqlCommand CreateDbCommand(CommandType commandType, string text)
        {
            var sqlConnection = new SqlConnection("Initial Catalog=TestDatabase");
            return new SqlCommand(text, sqlConnection)
            {
                CommandType = commandType
            };
        }

        private string GenerateOutput(SqlServerFormatter _formatter, SqlCommand _dbCommand, string _commandText)
        {
            var sqlParameters = _dbCommand.GetParameters();
            return _formatter.GetFormattedSql(_commandText, sqlParameters, _dbCommand);
        }

        private void AddDbParameter<T>(SqlCommand command, string name, object value, ParameterDirection parameterDirection = ParameterDirection.Input, int? size = null, DbType? type = null)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            parameter.Direction = parameterDirection;
            parameter.DbType = type ?? GetDbType(typeof(T));
            if (size.HasValue)
                parameter.Size = size.Value;
            command.Parameters.Add(parameter);
        }

        [Fact]
        public void InlineParameterNamesInParameterValues()
        {
            var formatter = new InlineFormatter();
            var parameters = new List<SqlTimingParameter>
            {
                new SqlTimingParameter() { DbType = "string", Name = "url", Value = "http://www.example.com?myid=1" },
                new SqlTimingParameter() { DbType = "string", Name = "myid", Value = "1" }
            };
            const string command = "SELECT * FROM urls WHERE url = @url OR myid = @myid";
            var formatted = formatter.FormatSql(command, parameters);
            Assert.Equal("SELECT * FROM urls WHERE url = 'http://www.example.com?myid=1' OR myid = '1'", formatted);
        }

        [Fact]
        public void EnsureVerboseSqlServerFormatterOnlyAddsInformation()
        {
            const string text = "select 1";
            var cmd = CreateDbCommand(CommandType.Text, text);

            var formatter = new VerboseSqlServerFormatter(true);
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "-- Command Type: Text\n-- Database: TestDatabase\n\nselect 1;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void VerboseSqlServerFormatterAddsTransactionInformation()
        {
            // note: since we don't have an active sql connection we cannot test the transactions coupled to a connection
            // the only thing we can do is test the TransactionScope transaction

            var formatter = new VerboseSqlServerFormatter(true);
            const string text = "select 1";
            var cmd = CreateDbCommand(CommandType.Text, text);
#if NET461
            const string expectedOutput = "-- Command Type: Text\n-- Database: TestDatabase\n-- Transaction Scope Iso Level: Serializable\n\nselect 1;";
            var transactionScope = new TransactionScope();
            var actualOutput = GenerateOutput(formatter, cmd, text);
            transactionScope.Dispose();
#else
            const string expectedOutput = "-- Command Type: Text\n-- Database: TestDatabase\n\nselect 1;";
            var actualOutput = GenerateOutput(formatter, cmd, text);
#endif

            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void TabelQueryWithoutParameters()
        {
            const string text = "select 1";
            var cmd = CreateDbCommand(CommandType.Text, text);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "select 1;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithOneParameter(string at)
        {
            const string text = "select 1 from dbo.Table where x = @a";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<int>(cmd, at + "a", 123);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @a int = 123;\n\nselect 1 from dbo.Table where x = @a;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithOneParameterDisabled(string at)
        {
            const string text = "select 1 from dbo.Table where x = @a";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<int>(cmd, at + "a", 123);

            var formatter = new SqlServerFormatter() { IncludeParameterValues = false };
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @a int;\n\nselect 1 from dbo.Table where x = @a;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithTwoParameters(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<int>(cmd, at + "x", 123);
            AddDbParameter<long>(cmd, at + "y", 123);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int = 123,\n        @y bigint = 123;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithTwoParametersDisabled(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<int>(cmd, at + "x", 123);
            AddDbParameter<long>(cmd, at + "y", 123);

            var formatter = new SqlServerFormatter() { IncludeParameterValues = false };
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int,\n        @y bigint;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithBit(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<bool?>(cmd, at + "x", true, type: DbType.Boolean);
            AddDbParameter<bool?>(cmd, at + "y", null, type: DbType.Boolean);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x bit = 1,\n        @y bit = null;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithVarchar(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<string>(cmd, at + "x", "bob", size: 20, type: DbType.AnsiString);
            AddDbParameter<string>(cmd, at + "y", "bob2", size: -1, type: DbType.AnsiString);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x varchar(20) = 'bob',\n        @y varchar(max) = 'bob2';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDate(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<DateTime>(cmd, at + "x", new DateTime(2017, 1, 30, 5, 13, 21), type: DbType.Date);
            AddDbParameter<DateTime>(cmd, at + "y", new DateTime(2001, 1, 1, 18, 12, 11), type: DbType.Date);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x datetime = '2017-01-30',\n        @y datetime = '2001-01-01';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.NotEqual(expectedOutput, actualOutput); // Auto-translation of DbType.Date to DbType.DateTime breaks output
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithTime(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<DateTime>(cmd, at + "x", new DateTime(2017, 1, 30, 5, 13, 21), type: DbType.Time);
            AddDbParameter<DateTime>(cmd, at + "y", new DateTime(2001, 1, 1, 18, 12, 11), type: DbType.Time);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x datetime = '05:13:21',\n        @y datetime = '18:12:11';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.NotEqual(expectedOutput, actualOutput); // Auto-translation of DbType.Time to DbType.DateTime breaks output
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDateTime(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<DateTime>(cmd, at + "x", new DateTime(2017, 1, 30, 5, 13, 21), type: DbType.DateTime);
            AddDbParameter<DateTime>(cmd, at + "y", new DateTime(2001, 1, 1, 18, 12, 11), type: DbType.DateTime);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x datetime = '2017-01-30T05:13:21',\n        @y datetime = '2001-01-01T18:12:11';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDateTime2(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<DateTime>(cmd, at + "x", new DateTime(2017, 1, 30, 5, 13, 21), type: DbType.DateTime2);
            AddDbParameter<DateTime>(cmd, at + "y", new DateTime(2001, 1, 1, 18, 12, 11), type: DbType.DateTime2);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x datetime2 = '2017-01-30T05:13:21',\n        @y datetime2 = '2001-01-01T18:12:11';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDateTimeOffset(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<DateTimeOffset>(cmd, at + "x", new DateTimeOffset(2017, 1, 30, 5, 13, 21, TimeSpan.FromHours(4.5)), type: DbType.DateTimeOffset);
            AddDbParameter<DateTimeOffset>(cmd, at + "y", new DateTimeOffset(2001, 1, 1, 18, 12, 11, TimeSpan.FromHours(-4.5)), type: DbType.DateTimeOffset);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x datetimeoffset = '2017-01-30T05:13:21+04:30',\n        @y datetimeoffset = '2001-01-01T18:12:11-04:30';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDouble(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<double>(cmd, at + "x", 123.45, type: DbType.Double);
            AddDbParameter<double>(cmd, at + "y", -54.321, type: DbType.Double);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x float = 123.45,\n        @y float = -54.321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithSingle(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<float>(cmd, at + "x", 123.45, type: DbType.Single);
            AddDbParameter<float>(cmd, at + "y", -54.321, type: DbType.Single);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x real = 123.45,\n        @y real = -54.321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithCurrency(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<decimal>(cmd, at + "x", 123.45, type: DbType.Currency);
            AddDbParameter<decimal>(cmd, at + "y", -54.321, type: DbType.Currency);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x money = 123.45,\n        @y money = -54.321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDecimal(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<decimal>(cmd, at + "x", 123.45, type: DbType.Decimal);
            AddDbParameter<decimal>(cmd, at + "y", -54.321, type: DbType.Decimal);

            var formatter = new SqlServerFormatter(); 
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x decimal(5,2) = 123.45,\n        @y decimal(5,3) = -54.321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDecimalNullable(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<decimal?>(cmd, at + "x", 123.45);
            AddDbParameter<decimal?>(cmd, at + "y", null);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x decimal(5,2) = 123.45,\n        @y decimal = null;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDecimalZeroPrecision(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<decimal>(cmd, at + "x", 12345.0, type: DbType.Decimal);
            AddDbParameter<decimal>(cmd, at + "y", -54321.0, type: DbType.Decimal);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x decimal(5,0) = 12345,\n        @y decimal(5,0) = -54321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithXml(string at)
        {
            const string text = "select 1 from dbo.Table where x = @x, y = @y";
            var cmd = CreateDbCommand(CommandType.Text, text);
            AddDbParameter<string>(cmd, at + "x", "<root></root>", type: DbType.Xml);
            AddDbParameter<string>(cmd, at + "y", "<root><node/></root>", type: DbType.Xml);

            var formatter = new SqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x xml = '<root></root>',\n        @y xml = '<root><node/></root>';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void StoredProcedureCallWithoutParameters()
        {
            const string text = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "EXEC dbo.SOMEPROCEDURE;";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithOneParameter(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            AddDbParameter<int>(cmd, at + "x", 123, ParameterDirection.Input);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithTwoParameter(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            AddDbParameter<int>(cmd, at + "x", 123, ParameterDirection.Input);
            AddDbParameter<long>(cmd, at + "y", 123, ParameterDirection.Input);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int = 123,\n        @y bigint = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x, @y = @y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithOneReturnParameter(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            AddDbParameter<int>(cmd, at + "retval", null, ParameterDirection.ReturnValue);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @retval int;\n\nEXEC @retval = dbo.SOMEPROCEDURE;\nSELECT @retval AS ReturnValue;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithNormalAndReturnParameter(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            AddDbParameter<int>(cmd, at + "x", 123, ParameterDirection.Input);
            AddDbParameter<int>(cmd, at + "retval", null, ParameterDirection.ReturnValue);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int = 123,\n        @retval int;\n\nEXEC @retval = dbo.SOMEPROCEDURE @x = @x;\nSELECT @retval AS ReturnValue;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithNormalAndReturnParameterDisabled(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            AddDbParameter<int>(cmd, at + "x", 123, ParameterDirection.Input);
            AddDbParameter<int>(cmd, at + "retval", null, ParameterDirection.ReturnValue);

            var formatter = new VerboseSqlServerFormatter() { IncludeParameterValues = false };
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int,\n        @retval int;\n\nEXEC @retval = dbo.SOMEPROCEDURE @x = @x;\nSELECT @retval AS ReturnValue;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithOneOutputParameter(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>(cmd, at + "x", 123, ParameterDirection.Output);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT;\nSELECT @x AS x;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithTwoOutputParameter(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>(cmd, at + "x", 123, ParameterDirection.Output);
            AddDbParameter<int>(cmd, at + "y", 123, ParameterDirection.Output);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int = 123,\n        @y int = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT, @y = @y OUTPUT;\nSELECT @x AS x, @y AS y;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithOneOutputParameterAndOneReturnParameter(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>(cmd, at + "x", 123, ParameterDirection.Output);
            AddDbParameter<int>(cmd, at + "retval", null, ParameterDirection.ReturnValue);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int = 123,\n        @retval int;\n\nEXEC @retval = dbo.SOMEPROCEDURE @x = @x OUTPUT;\nSELECT @retval AS ReturnValue, @x AS x;";
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithInOutputParameter(string at)
        {
            const string text = "dbo.SOMEPROCEDURE";
            var cmd = CreateDbCommand(CommandType.StoredProcedure, text);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>(cmd, at + "x", 123, ParameterDirection.InputOutput);

            var formatter = new VerboseSqlServerFormatter();
            var actualOutput = GenerateOutput(formatter, cmd, text);

            const string expectedOutput = "DECLARE @x int = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT;\nSELECT @x AS x;";
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}
