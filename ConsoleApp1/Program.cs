using EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
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
            using (var appContext = new ApplicationContext())
            {
                var count = await appContext.Persons.CountAsync();
                if (count <= 0)
                {
                    appContext.Persons.AddRange(new[]
                    {
                        new Person { Id = Guid.NewGuid(), Name = "p1", Kinds = JsonConvert.SerializeObject(new string[] { }) },
                        new Person { Id = Guid.NewGuid(), Name = "p2", Kinds = JsonConvert.SerializeObject(new [] { "kind1", "kind2" }) },
                        new Person { Id = Guid.NewGuid(), Name = "p2", Kinds = JsonConvert.SerializeObject(new [] { "kind1" }) },
                    });
                    await appContext.SaveChangesAsync();
                    count = await appContext.Persons.CountAsync();
                }

                var json = appContext.Set<JsonResult<string>>();
                var query = appContext.Persons
                    .Where(p => json.ValueFromOpenJson(p.Kinds, "$").Select(jr => jr.Value).Contains("kind2"));

                var result = await query.ToListAsync();
                //var result = query.ToList();
                Console.WriteLine(result.Count);

                Console.WriteLine($"count: {count}");
            }
        }
    }
}
