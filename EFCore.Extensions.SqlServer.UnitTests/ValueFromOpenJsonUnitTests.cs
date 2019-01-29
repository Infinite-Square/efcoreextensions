using EFCore.Extensions.SqlServer.UnitTests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Xunit;

namespace EFCore.Extensions.SqlServer.UnitTests
{
    public enum SqlRequestType
    {
        Select
    }

    public interface ISqlParserResult
    {
        SqlRequestType Type { get; }
    }

    public class SelectSqlParserResult : ISqlParserResult
    {
        public SqlRequestType Type => SqlRequestType.Select;
    }

    public enum SqlSourceType
    {
        Table,
        SubRequest,
        Function
    }

    public interface ISqlSource
    {
        SqlSourceType Type { get; }
    }

    public class TableSqlSource : ISqlSource
    {
        public SqlSourceType Type => SqlSourceType.Table;
    }

    public class FromSql
    {
        public string Alias { get; set; }
        public string Source { get; set; }
    }

    public class SqlParser
    {
        private const string SELECT = "select";
        private const string FROM = "from";

        private static FromSql ParseFrom(string sql, int start)
        {
            throw new NotImplementedException();
            //while (sql[start] == ' ') start++;
            //if (sql[start] == '(')
            //{
            //    //subrequest
            //    throw new NotImplementedException();
            //}
        }

        private static SelectSqlParserResult ParseSelect(string sql)
        {
            var result = new SelectSqlParserResult();
            var fromix = sql.IndexOf($"{FROM} ");
            if (fromix > 0)
            {
                var from = ParseFrom(sql, fromix + FROM.Length + 1);
            }

            return result;
        }

        public static ISqlParserResult Parse(string sql)
        {
            if (sql.StartsWith(SELECT, StringComparison.OrdinalIgnoreCase))
            {
                return ParseSelect(sql);
            }
            throw new NotImplementedException();
        }
    }

    [Collection("globalfakedatabase")]
    public class ValueFromOpenJsonUnitTests
    {
        private readonly IServiceProvider _services;

        public ValueFromOpenJsonUnitTests(GlobalFakeDatabaseFixture fixture)
        {
            _services = fixture.Services;
        }

        private DataContext DataContext()
        {
            return _services.GetRequiredService<DataContext>();
        }

        [Fact]
        public void AssertPersonCountWhereSqlIsOk()
        {
            using (var ctx = DataContext())
            {
                var catcher = ctx.GetSqlCommandCatcher();
                using (var catchingScope = catcher.EnableCatching())
                {
                    var json = ctx.Json<string>();
                    Assert.Equal(0, ctx.Persons
                        .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(j => j.Value).Contains("test"))
                        .Count());
                    var commands = catchingScope.Commands.ToArray();
                    Assert.Single(commands);
                    var sql = commands[0].Command.CommandText;
                    Assert.False(string.IsNullOrWhiteSpace(sql));

                }
            }
        }
    }
}
