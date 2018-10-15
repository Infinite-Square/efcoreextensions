using EFCore.Extensions;
using EFCore.Extensions.SqlCommandCatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            MainAsync().Wait();
            Console.ReadKey();
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddDbContext<ApplicationContext>(builder =>
                {
                    builder.UseLoggerFactory(new LoggerFactory(new[]
                    {
                      new ConsoleLoggerProvider((category, level) => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information, true)
                  }));
                    builder.UseExtensions(extensions =>
                    {
                        extensions.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=testdbapp3;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
                        //extensions.UseInMemoryDatabase("inmemory");
                        extensions.EnableSqlServerCommandCatcher();
                    });
                }, ServiceLifetime.Transient);
            return serviceCollection.BuildServiceProvider();
        }

        private static async Task MainAsync()
        {
            using (var provider = BuildServiceProvider())
            {
                using (var scope1 = provider.CreateScope())
                using (var scope2 = provider.CreateScope())
                using (var c1 = scope1.ServiceProvider.GetRequiredService<ApplicationContext>())
                using (var c2 = scope1.ServiceProvider.GetRequiredService<ApplicationContext>())
                {
                    var p1 = (IInfrastructure<IServiceProvider>)c1;
                    var catcher1 = p1.GetService<ISqlCommandCatcher>();
                    using (var scope = catcher1.EnableCatching())
                    {
                        var count1 = await c1.Persons.CountAsync();
                        if (count1 != 0) throw new Exception();
                        if (scope.Commands.Count() != 1) throw new Exception();
                        var count2 = await c2.Persons.CountAsync();
                        if (count2 == 0) throw new Exception();
                        if (scope.Commands.Count() != 1) throw new Exception();
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
}
