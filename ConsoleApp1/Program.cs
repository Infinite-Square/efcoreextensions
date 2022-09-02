using Domain;
using EFCore.Extensions;
using EFCore.Extensions.SqlCommandCatching;
using EFCore.Extensions.SqlServer.Update.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //MainAsyncOld2().Wait();
            //MainAsync().Wait();
            TestMemoryIssuesAsync().Wait();
            MainAsynTest().Wait();
            Console.ReadKey();
        }

        private static readonly string _connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=testdbapp3;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        private static ServiceCollection GetServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddEntityFrameworkSqlServer();
            serviceCollection
                .AddDbContext<ApplicationContext>((sp, builder) =>
                {
                    builder.UseSqlServer(_connectionString);
                    builder.UseInternalServiceProvider(sp);
                    //builder.UseApplicationServiceProvider(sp);
                    //builder.UseLoggerFactory(new LoggerFactory(new[]
                    //{
                    //    //new ConsoleLoggerProvider((category, level) => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information, true)
                    //    new ConsoleLoggerProvider((category, level) => true, true)
                    //}));
                    //builder.UseExtensions(extensions =>
                    //{
                    //    extensions.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=testdbapp3;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
                    //    //extensions.UseInMemoryDatabase("inmemory");
                    //    extensions.EnableSqlServerCommandCatcher();
                    //    extensions.EnableSqlServerModificationCommandBatchEvents(() => new ModificationCommandBatchEvents());
                    //});
                }/*, ServiceLifetime.Transient*/);
            return serviceCollection;
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var serviceCollection = GetServiceCollection();
            return serviceCollection.BuildServiceProvider();
        }

        private class ModificationCommandBatchEvents : ISqlServerModificationCommandBatchEvents
        {
            public void AddedCommand(ModificationCommand command, bool added, ModificationCommandBatch batch, ISqlServerModificationCommandBatchAppender appender)
            {
            }

            public void AddingCommand(ModificationCommand command, ModificationCommandBatch batch, ISqlServerModificationCommandBatchAppender appender)
            {
                if (command.EntityState == EntityState.Deleted)
                {
                    foreach (var entry in command.Entries)
                    {
                        if (entry.EntityType.ClrType == typeof(Person))
                        {
                            //nameof(Person.Values) = "deleted"
                            var valuesProperty = entry.EntityType.FindProperty(nameof(Person.Values));

                            //var modificationCommand = new ModificationCommand(command.TableName, command.Schema, new List<ColumnModification>(command.ColumnModifications)
                            //{
                            //    new ColumnModification(valuesProperty.Relational().ColumnName, entry.GetOriginalValue(valuesProperty), "deleted", valuesProperty, false, true, false, false)
                            //});

                            var modificationCommand = new ModificationCommand(command.TableName, command.Schema
                                , new List<ColumnModification>(command.ColumnModifications.Select(cm => new ColumnModification(cm.Entry
                                    , cm.Property
                                    , new Microsoft.EntityFrameworkCore.Metadata.RelationalPropertyAnnotations(cm.Property)
                                    {
                                        ColumnName = cm.ColumnName
                                    }
                                    , () => $"{cm.ParameterName}_ewh"
                                    , cm.IsRead
                                    , cm.IsWrite
                                    , cm.IsKey
                                    , cm.IsCondition
                                    , cm.IsConcurrencyToken)))
                            {
                                new ColumnModification(valuesProperty.Relational().ColumnName, entry.GetOriginalValue(valuesProperty), "deleted", valuesProperty, false, true, false, false)
                            });

                            //appender.Append(modificationCommand);
                            appender.Prepend(modificationCommand);

                        }
                    }
                }
            }
        }

        public class Test
        {
            public DateTimeOffset? Date { get; set; }
            public string Ab { get; set; }
            public List<string> Flags { get; set; }
        }

        private static async Task TestMemoryIssuesAsync()
        {
            //var id = Guid.NewGuid();
            var servies = GetServiceCollection();
            //while(true)
            for (var i = 0; i < 1000; i++)
            {
                using (var provider = servies.BuildServiceProvider())
                using (var scope1 = provider.CreateScope())
                using (var c1 = scope1.ServiceProvider.GetRequiredService<ApplicationContext>())
                {
                    //c1.Persons.Add(new Person
                    //{
                    //    Id = id,
                    //    Name = "aa"
                    //});
                    //await c1.SaveChangesAsync();


                    var persons = await c1.Persons.AsNoTracking().Take(10).ToListAsync();
                    if (persons.Count <= 0)
                        Console.WriteLine("aa");
                }
            }
        }

        private static async Task MainAsynTest()
        {
            using (var provider = BuildServiceProvider())
            using (var scope1 = provider.CreateScope())
            using (var c1 = scope1.ServiceProvider.GetRequiredService<ApplicationContext>())
            {
                Console.Clear();

                while (true)
                {
                    {
                        var t = new Test { Ab = "a", Flags = new List<string> { "n1", "n2" } };
                        var pp = await c1.Persons
                            .Where(p => !new[] { "n1", "n2" }.Contains(p.Name))
                            .ToListAsync();
                        //Console.WriteLine(pp);
                    }

                    {
                        var t = new Test { Ab = "b", Flags = new List<string> { "a1", "a2", "a3" } };
                        var pp = await c1.Persons
                            .Where(p => !new[] { "n1", "n3" }.Contains(p.Name))
                            .ToListAsync();
                        //Console.WriteLine(pp);
                    }

                    //{
                    //    var t = new Test { Ab = "a", Flags = new List<string> { "n1", "n2" } };
                    //    var pp = await c1.Persons
                    //        .In(t.Flags, p => p.Name)
                    //        .ToListAsync();
                    //    //Console.WriteLine(pp);
                    //}

                    //{
                    //    var t = new Test { Ab = "b", Flags = new List<string> { "a1", "a2", "a3" } };
                    //    var pp = await c1.Persons
                    //        .In(t.Flags, p => p.Name)
                    //        .ToListAsync();
                    //    //Console.WriteLine(pp);
                    //}

                    //{
                    //    var t = new Test { Ab = "a", Flags = new List<string> { "n1", "n2" } };
                    //    var pp = await c1.Persons
                    //        .Where(p => p.Name == t.Ab || !t.Flags.Contains(p.Name))
                    //        .ToListAsync();
                    //    //Console.WriteLine(pp);
                    //}

                    //{
                    //    var t = new Test { Ab = "b", Flags = new List<string> { "a1", "a2", "a3" } };
                    //    var pp = await c1.Persons
                    //        .Where(p => p.Name == t.Ab || !t.Flags.Contains(p.Name))
                    //        .ToListAsync();
                    //    //Console.WriteLine(pp);
                    //}

                    await Task.Delay(500);
                }
            }

            using (var provider = BuildServiceProvider())
            using (var scope1 = provider.CreateScope())
            using (var c1 = scope1.ServiceProvider.GetRequiredService<ApplicationContext>())
            {
                var json = c1.Json<string>();
                var query = c1.Persons
                    .Where(pp => pp.KindsList != null && json.ValueFromOpenJson(pp.KindsList, "$")
                        .Select(p => p.Value)
                        .Contains("kind1")
                    );

                var persons = await query.ToArrayAsync();
                Console.WriteLine(persons);


            }
        }

        private static async Task MainAsync()
        {
            using (var provider = BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                var p = scope.ServiceProvider;
                var ctx = p.GetRequiredService<ApplicationContext>();
                //var persons = await ctx.Persons.ToListAsync();
                //foreach(var person in persons)
                //{
                //    person.Values = "abcd";
                //}
                //await ctx.SaveChangesAsync();

                var newp = new Person
                {
                    Name = "newp"
                };
                ctx.Persons.Add(newp);
                await ctx.SaveChangesAsync();

                ctx.Persons.Remove(newp);

                var res = await ctx.SaveChangesAsync();
                if (res != 2)
                {
                    Debugger.Break();
                }
                //var toDeletes = ctx.ChangeTracker.Entries<Person>().Where(e => e.State == EntityState.Deleted).ToList();
                //using (var sqlCatch = ctx.GetSqlCommandCatcher().EnableCatching())
                //{
                //    await ctx.SaveChangesAsync();
                //}
            }
        }

        private static async Task MainAsyncOld4()
        {
            using (var provider = BuildServiceProvider())
            {
                //using(var scope = provider.CreateScope())
                //{
                //}


                using (var scope1 = provider.CreateScope())
                {
                    var unit = new Unit(scope1.ServiceProvider);
                    await unit.ExecuteAsync();
                }

                using (var scope1 = provider.CreateScope())
                {
                    var unit = new Unit(scope1.ServiceProvider);
                    await unit.ExecuteAsync();
                }
            }
        }

        private static async Task MainAsyncOld3()
        {
            using (var provider = BuildServiceProvider())
            using (var scope1 = provider.CreateScope())
            using (var c1 = scope1.ServiceProvider.GetRequiredService<ApplicationContext>())
            {
                var json = c1.Json<string>();

                // fail because missing inclue
                //var query = c1.Orders.Where(o => json.ValueFromOpenJson(o.Person.KindsList, "$").Select(p => p.Value).Contains("kind1"));

                //var query = c1.Orders
                //    .Include(o => o.Person)
                //    .Where(o => json.ValueFromOpenJson(o.Person.KindsList, "$")
                //    .Select(p => p.Value)
                //    .Contains("kind1")
                //    );

                Expression<Func<Order, Guid?>> oid = o => o.PersonId;
                Expression<Func<Person, Guid?>> pid = o => o.Id;
                Expression<Func<Order, Person, Order>> s = (o, p) => o;

                var query = c1.Orders
                    .Join(c1.Persons, oid, pid, s)
                    .Where(o => json.ValueFromOpenJson(o.Person.KindsList, "$")
                        .Select(p => p.Value)
                        .Contains("kind1")
                    );

                var orders = await query.ToArrayAsync();
                Console.WriteLine(orders);


                var count = await query.CountAsync();
                Console.WriteLine(count);
            }
        }

        private static async Task MainAsyncOld2()
        {
            using (var provider = BuildServiceProvider())
            {
                using (var scope1 = provider.CreateScope())
                using (var scope2 = provider.CreateScope())
                using (var c1 = scope1.ServiceProvider.GetRequiredService<ApplicationContext>())
                using (var c2 = scope1.ServiceProvider.GetRequiredService<ApplicationContext>())
                {
                    await c1.Persons.BulkInsertAsync(new[]
                    {
                        new Person
                        {
                            Name = "toto1"
                        },
                        new Person
                        {
                            Name = "toto2"
                        },
                        new Person
                        {
                            Name = "toto3"
                        }
                    }, CancellationToken.None);


                    var p1 = (IInfrastructure<IServiceProvider>)c1;
                    var catcher1 = p1.GetService<ISqlCommandCatcher>();
                    using (var scope = catcher1.EnableCatching())
                    {
                        var count1 = await c1.Persons.CountAsync();
                        if (count1 != 0)
                            throw new Exception();
                        if (scope.Commands.Count() != 1)
                            throw new Exception();
                        var count2 = await c2.Persons.CountAsync();
                        if (count2 == 0)
                            throw new Exception();
                        if (scope.Commands.Count() != 1)
                            throw new Exception();

                        IQueryable<JsonResult<string>> json = c1.Set<JsonResult<string>>();
                        var pp = await c1.Persons
                            .Where(p => json.ValueFromOpenJson(p.KindsList, "$").Select(jr => jr.Value).Contains("kind2"))
                            .ToListAsync();
                        if (scope.Commands.Count() != 2)
                            throw new Exception();

                        c1.Persons.Add(new Person
                        {
                            Name = "toto"
                        });
                        await c1.SaveChangesAsync();

                        if (scope.Commands.Count() != 3)
                            throw new Exception();
                    }
                }
            }
        }

        private static async Task MainAsyncOld()
        {
            using (var provider = BuildServiceProvider())
            {
                using (var appContext = provider.GetRequiredService<ApplicationContext>())
                {
                    //IQueryCompilationContextFactory

                    Expression<Func<Person, bool>> expr2 = p => p.KindsList != null;

                    var dbServices = ((IInfrastructure<IServiceProvider>)appContext.Database).Instance;
                    var queryCompilicationContextFactory = dbServices.GetRequiredService<IQueryCompilationContextFactory>();
                    var compilationContext = queryCompilicationContextFactory.Create(false);
                    var entityQueryModelVisitor = compilationContext.CreateQueryModelVisitor();
                    var test = entityQueryModelVisitor.ReplaceClauseReferences(expr2);


                    var count = await appContext.Persons.CountAsync();
                    if (count <= 0)
                    {
                        appContext.Persons.AddRange(new[]
                        {
                        new Person { Id = Guid.NewGuid(), Name = "p1", KindsList = JsonConvert.SerializeObject(new string[] { }) },
                        new Person { Id = Guid.NewGuid(), Name = "p2", KindsList = JsonConvert.SerializeObject(new [] { "kind1", "kind2" }) },
                        new Person { Id = Guid.NewGuid(), Name = "p2", KindsList = JsonConvert.SerializeObject(new [] { "kind1" }) },
                    });
                        await appContext.SaveChangesAsync();
                        count = await appContext.Persons.CountAsync();
                    }

                    IQueryable<JsonResult<string>> json = appContext.Set<JsonResult<string>>();
                    Expression<Func<Person, bool>> expr = p => json.ValueFromOpenJson(p.KindsList, "$").Select(jr => jr.Value).Contains("kind2");

                    //var p = Expression.Parameter(typeof(Person));
                    //var valueFromOpenJson = ExtensionsDbFunctionsExtensions.ValueFromOpenJsonMethod.MakeGenericMethod(typeof(string));
                    //var v = Expression.Call(valueFromOpenJson
                    //    , Expression.Constant(json)
                    //    , Expression.Property(p, "KindsList")
                    //    , Expression.Constant("$"));

                    //var select = typeof(Enumerable)
                    //    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    //    .Where(m => m.Name == nameof(Enumerable.Select))
                    //    .ElementAt(0)
                    //    .MakeGenericMethod(typeof(JsonResult<string>), typeof(string));

                    //var jr = Expression.Parameter(typeof(JsonResult<string>));
                    //var j = Expression.Lambda(typeof(Func<JsonResult<string>, string>), Expression.Property(jr, "Value"), jr);
                    //var s = Expression.Call(select, v, j);

                    //var contains = typeof(Enumerable)
                    //    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    //    .Where(m => m.Name == nameof(Enumerable.Contains))
                    //    .ElementAt(0)
                    //    .MakeGenericMethod(typeof(string));
                    //var c = Expression.Call(contains, s, Expression.Constant("kind2"));

                    //var expr = Expression.Lambda<Func<Person, bool>>(c, p);


                    var query = appContext.Persons
                        .Where(expr);

                    var rawSql = appContext.CatchFirstRawSql(db =>
                    {
                        db.Persons.ToList();
                    });
                    Console.WriteLine(rawSql);

                    count = await query.CountAsync();

                    var rawSql2 = await appContext.CatchFirstRawSqlAsync(async db =>
                    {
                        var r1 = await query.ToListAsync();
                    });

                    //var result = query.ToList();
                    var result = await query.ToListAsync();
                    Console.WriteLine(result.Count);
                    Console.WriteLine($"count: {count}");

                    if (count != result.Count)
                        throw new Exception();
                }
            }
        }


    }

    public static class CollectionPredicateBuilder
    {
        public static IQueryable<TSource> In<TSource, TCollection>(
            this IQueryable<TSource> source,
            IList<TCollection> collection,
            Expression<Func<TSource, TCollection>> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            var listType = typeof(List<TCollection>);
            var addMethod = listType.GetMethod("Add");
            var getItemMethod = listType.GetMethod("get_Item");
            var containsMethod = listType.GetMethod("Contains");

            // to-do: if index is > 2100 then we need to use constants
            var initializers = collection
                .Select((value, index) =>
                    Expression.ElementInit(
                        addMethod,
                        new[]
                        {
                            Expression.Call(
                                Expression.Constant(
                                    collection,
                                    listType),
                                getItemMethod,
                                new []
                                {
                                    Expression.Constant(
                                        index,
                                        typeof(int))
                                })
                        }))
                        .ToList();

            var bucket = 1;
            while (initializers.Count > bucket)
            {
                bucket <<= 1;
            }

            bucket = bucket > 2098 ? 2098 : bucket;

            if (initializers.Count > bucket)
            {
                throw new InvalidOperationException("In cannot be used with more than 2100 elements");
            }

            for (var index = initializers.Count; index < bucket; index++)
            {
                initializers.Add(initializers[index - 1]);
            }

            return source.Where(
                Expression.Lambda<Func<TSource, bool>>(
                    Expression.Call(
                        Expression.ListInit(
                            Expression.New(
                                listType),
                            initializers),
                            containsMethod,
                        selector.Body),
                    selector.Parameters));
        }
    }
}
