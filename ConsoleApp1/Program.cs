using EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            MainAsync().Wait();
            Console.ReadKey();
        }

        static async Task MainAsync()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddDbContext<ApplicationContext>(builder =>
              {
                  var lf = new LoggerFactory();
                  lf.AddProvider(new SimpleConsoleLoggerProvider());
                  builder.UseLoggerFactory(lf);
                  builder.UseExtensions(extensions =>
                  {
                      extensions.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=testdbapp3;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
                      //extensions.UseInMemoryDatabase("inmemory");
                  });
              }, ServiceLifetime.Transient);

            using (var provider = serviceCollection.BuildServiceProvider())
            {
                using (var appContext = provider.GetRequiredService<ApplicationContext>())
                {
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
                    //Expression<Func<Person, bool>> expr = p => json.ValueFromOpenJson(p.KindsList, "$").Select(jr => jr.Value).Contains("kind2");

                    var p = Expression.Parameter(typeof(Person));
                    var valueFromOpenJson = ExtensionsDbFunctionsExtensions.ValueFromOpenJsonMethod.MakeGenericMethod(typeof(string));
                    var v = Expression.Call(valueFromOpenJson
                        , Expression.Constant(json)
                        , Expression.Property(p, "KindsList")
                        , Expression.Constant("$"));

                    var select = typeof(Enumerable)
                        .GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .Where(m => m.Name == nameof(Enumerable.Select))
                        .ElementAt(0)
                        .MakeGenericMethod(typeof(JsonResult<string>), typeof(string));

                    var jr = Expression.Parameter(typeof(JsonResult<string>));
                    var j = Expression.Lambda(typeof(Func<JsonResult<string>, string>), Expression.Property(jr, "Value"), jr);
                    var s = Expression.Call(select, v, j);

                    var contains = typeof(Enumerable)
                        .GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .Where(m => m.Name == nameof(Enumerable.Contains))
                        .ElementAt(0)
                        .MakeGenericMethod(typeof(string));
                    var c = Expression.Call(contains, s, Expression.Constant("kind2"));

                    var expr = Expression.Lambda<Func<Person, bool>>(c, p);


                    var query = appContext.Persons
                        .Where(expr);

                    count = await query.CountAsync();
                    var result = await query.ToListAsync();
                    //var result = query.ToList();

                    Console.WriteLine(result.Count);
                    Console.WriteLine($"count: {count}");

                    if (count != result.Count)
                        throw new Exception();
                }
            }
        }
    }
}
