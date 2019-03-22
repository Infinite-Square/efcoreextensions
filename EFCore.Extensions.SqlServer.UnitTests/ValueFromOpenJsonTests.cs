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
    //need a true SQL Database (see appsettings.json)
    [Collection("globaltruedatabase")]
    public class ValueFromOpenJsonTests
    {
        private readonly IServiceProvider _services;

        public ValueFromOpenJsonTests(GlobalTrueDatabaseFixture fixture)
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

        private static bool IsEqual(IEnumerable<Group> expected, IEnumerable<Group> actual)
        {
            var e = expected.ToList();
            var a = actual.ToList();
            if (e.Count != a.Count)
                return false;
            Assert.Equal(e.Count, a.Count);
            for (var i = 0; i < e.Count; ++i)
                if (e[i].Id != a[i].Id)
                    return false;
            return true;
        }

        [Fact]
        public async Task Test1()
        {
            var seq = new SeqGuid();
            bool ok;
            var persons = new[]
            {
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k2" })
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
                    ok = IsEqual(persons.Take(1), result);
                    ctx.RemoveRange(persons);
                    await ctx.SaveChangesAsync();
                }
            }

            Assert.True(ok);
        }

        [Fact]
        public async Task Test2()
        {
            var seq = new SeqGuid();
            bool ok;
            var persons = new[]
            {
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1", "k2" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1,k2" })
                },
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

        [Fact]
        public async Task Test3()
        {
            var seq = new SeqGuid();
            bool ok;
            var persons = new[]
            {
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1", "k2" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1,k2" })
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
                    var query = ctx.Persons
                        .AsNoTracking()
                        .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(k => k.Value).Contains("k1"))
                        .Skip(0)
                        .Take(5)
                        .Skip(0)
                        .Take(1);

                    var result = await query.ToListAsync();
                    Assert.Single(result);
                    ok = IsEqual(new[] { persons[1] }, result);
                    ctx.RemoveRange(persons);
                    await ctx.SaveChangesAsync();
                }
            }

            Assert.True(ok);
        }

        [Fact]
        public async Task Test4()
        {
            var seq = new SeqGuid();
            bool ok;
            var persons = new[]
            {
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1", "k2" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1,k2" })
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
                    //var query = ctx.Persons
                    //    .AsNoTracking()
                    //    .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(k => k.Value).Intersect(new[] { "k2" }).Count() > 0)
                    //var kinds = ;
                    var query = ctx.Persons
                        .AsNoTracking()
                        .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(k => k.Value).Any(k => new[] { "k2" }.Contains(k)))
                        .OrderBy(p => p.Firstname)
                        .Skip(0)
                        .Take(5)
                        .Skip(0)
                        .Take(1)

                        ;

                    var result = await query.ToListAsync();
                    Assert.Single(result);
                    ok = IsEqual(new[] { persons[2] }, result);
                    ctx.RemoveRange(persons);
                    await ctx.SaveChangesAsync();
                }
            }

            Assert.True(ok);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        [InlineData(false, true, false)]
        public async Task Test5(bool include, bool count, bool ignoreQueryFilters)
        {
            //var isTest = include && !count && !ignoreQueryFilters;
            //var isTest = include && !count && ignoreQueryFilters;
            //if (!isTest)
            //    return;

            var seq = new SeqGuid();
            bool ok;
            var persons = new[]
            {
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1", "k2" })
                }
            };

            var groups = new[]
            {
                new Group
                {
                    Id = seq++,
                    Name = "1"
                },
                new Group
                {
                    Id = seq++,
                    Name = "2"
                }
            };

            var groupPersons = new[]
            {
                new GroupPerson
                {
                    Id = seq++,
                    GroupId = groups[0].Id,
                    PersonId = persons[0].Id
                },
                new GroupPerson
                {
                    Id = seq++,
                    GroupId = groups[0].Id,
                    PersonId = persons[1].Id
                },
                new GroupPerson
                {
                    Id = seq++,
                    GroupId = groups[1].Id,
                    PersonId = persons[1].Id
                }
            };


            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var ctx = _services.GetRequiredService<DataContext>())
                {
                    ctx.AddRange(persons);
                    ctx.AddRange(groups);
                    ctx.AddRange(groupPersons);
                    await ctx.SaveChangesAsync();
                }

                using (var ctx = _services.GetRequiredService<DataContext>())
                {
                    var allPersonsWithGroups = await ctx.Persons
                        .AsNoTracking()
                        .Include(p => p.Groups).ThenInclude(gp => gp.Group)
                        .ToListAsync();
                    Assert.Equal(2, allPersonsWithGroups.Count);
                    var p0 = allPersonsWithGroups.Find(p => p.Id == persons[0].Id);
                    Assert.NotNull(p0);
                    Assert.Single(p0.Groups);
                    Assert.Equal(groups[0].Id, p0.Groups.First().Group.Id);
                    var p1 = allPersonsWithGroups.Find(p => p.Id == persons[1].Id);
                    Assert.NotNull(p1);
                    Assert.Equal(2, p1.Groups.Count);
                    Assert.Contains(p1.Groups, g => g.Group.Id == groups[0].Id);
                    Assert.Contains(p1.Groups, g => g.Group.Id == groups[1].Id);

                    IQueryable<JsonResult<string>> json = ctx.Set<JsonResult<string>>();
                    var query = ctx.Persons
                        .AsNoTracking();

                    if (include)
                        query = query
                            .Include(p => p.Groups).ThenInclude(gp => gp.Group);

                    if (ignoreQueryFilters)
                        query = query
                            .IgnoreQueryFilters();

                    query = query
                        .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(k => k.Value).Any(k => new[] { "k2" }.Contains(k)));

                    if (count)
                    {
                        var result = await query.CountAsync();
                        Assert.Equal(1, result);
                    }
                    else
                    {
                        var result = await query.ToListAsync();
                        Assert.Single(result);
                        ok = IsEqual(new[] { persons[1] }, result);
                    }
                    ctx.RemoveRange(groupPersons);
                    ctx.RemoveRange(groups);
                    ctx.RemoveRange(persons);
                    await ctx.SaveChangesAsync();
                }
            }

            //Assert.True(ok);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, true)]
        [InlineData(true, true, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        [InlineData(false, true, false)]
        public async Task Test6(bool include, bool count, bool ignoreQueryFilters)
        {
            //var isTest = include && !count && !ignoreQueryFilters;
            //if (!isTest)
            //    return;

            var seq = new SeqGuid();
            bool ok;
            var persons = new[]
            {
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1" })
                },
                new Person
                {
                    Id = seq++,
                    Firstname = "",
                    Lastname = "",
                    Kinds = JsonConvert.SerializeObject(new[] { "k1", "k2" })
                }
            };

            var groups = new[]
            {
                new Group
                {
                    Id = seq++,
                    Name = "1"
                },
                new Group
                {
                    Id = seq++,
                    Name = "2"
                }
            };

            var groupPersons = new[]
            {
                new GroupPerson
                {
                    Id = seq++,
                    GroupId = groups[0].Id,
                    PersonId = persons[0].Id
                },
                new GroupPerson
                {
                    Id = seq++,
                    GroupId = groups[1].Id,
                    PersonId = persons[0].Id
                },
                new GroupPerson
                {
                    Id = seq++,
                    GroupId = groups[1].Id,
                    PersonId = persons[1].Id
                }
            };


            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var ctx = _services.GetRequiredService<DataContext>())
                {
                    ctx.AddRange(persons);
                    ctx.AddRange(groups);
                    ctx.AddRange(groupPersons);
                    await ctx.SaveChangesAsync();
                }

                using (var ctx = _services.GetRequiredService<DataContext>())
                {
                    var allPersonsWithGroups = await ctx.Persons
                        .AsNoTracking()
                        .Include(p => p.Groups).ThenInclude(gp => gp.Group)
                        .ToListAsync();
                    Assert.Equal(2, allPersonsWithGroups.Count);
                    var p0 = allPersonsWithGroups.Find(p => p.Id == persons[0].Id);
                    Assert.NotNull(p0);
                    Assert.Equal(2, p0.Groups.Count);
                    Assert.Contains(p0.Groups, g => g.Group.Id == groups[0].Id);
                    Assert.Contains(p0.Groups, g => g.Group.Id == groups[1].Id);
                    var p1 = allPersonsWithGroups.Find(p => p.Id == persons[1].Id);
                    Assert.NotNull(p1);
                    Assert.Single(p1.Groups);
                    Assert.Contains(p1.Groups, g => g.Group.Id == groups[1].Id);

                    IQueryable<JsonResult<string>> json = ctx.Set<JsonResult<string>>();
                    var query = ctx.Groups
                        .AsNoTracking();
                    if (include)
                        query = query
                            .Include(g => g.Persons).ThenInclude(gp => gp.Person);
                    if (ignoreQueryFilters)
                        query = query
                            .IgnoreQueryFilters();

                    query = query
                        .Where(g => g.Persons.Select(gp => gp.Person).Any(p => json.ValueFromOpenJson(p.Kinds, "$").Select(k => k.Value).Contains("k2")));
                        //.Where(g => g.Persons.Select(gp => gp.Person).Any(p => json.ValueFromOpenJson(p.Kinds, "$").Select(k => k.Value).Any(k => new[] { "k2" }.Contains(k))));
                    //.Where(g => g.Persons.Any(gp => json.ValueFromOpenJson(gp.Person.Kinds, "$").Select(k => k.Value).Any(k => new[] { "k2" }.Contains(k))));

                    if (count)
                    {
                        var result = await query.CountAsync();
                        Assert.Equal(1, result);
                    }
                    else
                    {
                        var result = await query.ToListAsync();
                        Assert.Single(result);
                        ok = IsEqual(new[] { groups[1] }, result);
                    }
                    ctx.RemoveRange(groupPersons);
                    ctx.RemoveRange(groups);
                    ctx.RemoveRange(persons);
                    await ctx.SaveChangesAsync();
                }
            }

            //Assert.True(ok);
        }
    }
}
