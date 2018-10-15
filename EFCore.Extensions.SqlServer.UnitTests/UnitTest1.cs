using EFCore.Extensions.SqlServer.UnitTests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace EFCore.Extensions.SqlServer.UnitTests
{
    [Collection("global")]
    public class UnitTest1
    {
        private readonly IServiceProvider _services;

        public UnitTest1(GlobalFixture fixture)
        {
            _services = fixture.Services;
        }

        private static bool IsEqual(IEnumerable<Person> expected, IEnumerable<Person> actual)
        {
            var e = expected.ToList();
            var a = actual.ToList();
            if (e.Count != a.Count) return false;
            Assert.Equal(e.Count, a.Count);
            for (var i = 0; i < e.Count; ++i)
                if (e[i].Id != a[i].Id)
                    return false;
            return true;
        }

        [Fact]
        public async Task Test1()
        {
            bool ok;
            var persons = new[]
            {
                new Person
                {
                    Id = Guid.NewGuid(),
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1" })
                }
            };

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var ctx = _services.GetRequiredService<DataContext>())
                {
                    ctx.AddRange(persons);
                    await ctx.SaveChangesAsync();
                }

                using (var ctx = _services.GetRequiredService<DataContext>())
                {
                    IQueryable<JsonResult<string>> json = ctx.Set<JsonResult<string>>();
                    var result = await ctx.Persons
                        .AsNoTracking()
                        .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(k => k.Value).Contains("k1"))
                        .ToListAsync();
                    ok = IsEqual(persons, result);
                    ctx.RemoveRange(persons);
                    await ctx.SaveChangesAsync();
                }
            }

            Assert.True(ok);
        }

        [Fact]
        public async Task Test2()
        {
            bool ok;
            var persons = new[]
            {
                //new Person
                //{
                //    Id = Guid.NewGuid(),
                //    Firstname = "",
                //    Lastname = "",
                //    Kinds = null
                //},
                //new Person
                //{
                //    Id = Guid.NewGuid(),
                //    Firstname = "",
                //    Lastname = "",
                //    Kinds = ""
                //},
                new Person
                {
                    Id = Guid.NewGuid(),
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "" })
                },
                new Person
                {
                    Id = Guid.NewGuid(),
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1" })
                },
                new Person
                {
                    Id = Guid.NewGuid(),
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1", "k2" })
                },
                new Person
                {
                    Id = Guid.NewGuid(),
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1,k2" })
                },
                //new Person
                //{
                //    Id = Guid.NewGuid(),
                //    Firstname = "",
                //    Lastname = "",
                //    Kinds = "k1,k2"
                //}
            };

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var ctx = _services.GetRequiredService<DataContext>())
                {
                    ctx.AddRange(persons);
                    await ctx.SaveChangesAsync();
                }

                using (var ctx = _services.GetRequiredService<DataContext>())
                {
                    IQueryable<JsonResult<string>> json = ctx.Set<JsonResult<string>>();
                    var result = await ctx.Persons
                        .AsNoTracking()
                        .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(k => k.Value).Contains("k1"))
                        .ToListAsync();
                    ok = IsEqual(persons.Where(p =>
                    {
                        if (string.IsNullOrWhiteSpace(p.Kinds)) return false;
                        try
                        {
                            var kinds = JsonConvert.DeserializeObject<string[]>(p.Kinds);
                            return kinds.Contains("k1");
                        }
                        catch (Exception)
                        {
                        }
                        return false;
                    }), result);
                    ctx.RemoveRange(persons);
                    await ctx.SaveChangesAsync();
                }
            }

            Assert.True(ok);
        }
    }
}
