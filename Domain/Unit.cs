using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Domain
{
    public class DbContextHelper
    {
        private readonly IServiceProvider _internalServiceProvider;
        private readonly MemoryCache _memoryCache;

        public DbContextHelper(DbContext dbContext)
        {
            _internalServiceProvider = (IServiceProvider)typeof(DbContext).GetProperty("InternalServiceProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dbContext);
            _memoryCache = (MemoryCache)_internalServiceProvider.GetRequiredService<IMemoryCache>();
        }

        public int CacheCount => _memoryCache.Count;
    }

    public class Unit
    {
        private readonly IServiceProvider serviceProvider;

        public Unit(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task ExecuteAsync()
        {
            var sp = serviceProvider;
            var ctx = sp.GetRequiredService<ApplicationContext>();
            //using ()
            //{
                var q = EF.CompileQuery<ApplicationContext, Person>(cc => cc.Persons);
                //q(ctx);

                var helper = new DbContextHelper(ctx);
                Console.WriteLine(helper.CacheCount);
                var persons = await ctx.Persons.ToListAsync();
                Console.WriteLine(helper.CacheCount);
                persons = await ctx.Persons.ToListAsync();
                Console.WriteLine(helper.CacheCount);
                persons = await ctx.Persons.AsNoTracking().ToListAsync();
                Console.WriteLine(helper.CacheCount);
                string name = "";
                persons = await ctx.Persons.Where(p => p.Name == name).ToListAsync();
                Console.WriteLine(helper.CacheCount);
                persons = await ctx.Persons.Where(p => p.Name == name).ToListAsync();
                Console.WriteLine(helper.CacheCount);
                name = "22";
                persons = await ctx.Persons.Where(p => p.Name == name).ToListAsync();
                Console.WriteLine(helper.CacheCount);
                persons = await ctx.Persons.AsNoTracking().Where(p => p.Name == "").ToListAsync();
                Console.WriteLine(helper.CacheCount);
                persons = await ctx.Persons.AsNoTracking().Where(p => p.Name == "").ToListAsync();
                Console.WriteLine(helper.CacheCount);
                persons = await ctx.Persons.AsNoTracking().Where(p => p.Name == "22").ToListAsync();
                Console.WriteLine(helper.CacheCount);


                Expression<Func<Person, bool>> filter()
                {
                    var p = Expression.Parameter(typeof(Person));
                    var body = Expression.Equal(Expression.Property(p, "Name"), Expression.Constant("22"));
                    return Expression.Lambda<Func<Person, bool>>(body, p);
                }

                persons = await ctx.Persons.Where(filter()).ToListAsync();
                Console.WriteLine(helper.CacheCount);
                persons = await ctx.Persons.Where(filter()).ToListAsync();
                Console.WriteLine(helper.CacheCount);
            ////}

            ////using (var ctx = sp.GetRequiredService<ApplicationContext>())
            ////{
            //    var helper = new DbContextHelper(ctx);
            //    Console.WriteLine(helper.CacheCount);
            //}
        }

        public async Task ExecuteAsyncOld()
        {
            var sp = serviceProvider;
            using (var c1 = sp.GetRequiredService<ApplicationContext>())
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
                var set = c1.Persons;

                var query = c1.Orders
                    //.Join(set, oid, pid, s)
                    .Join(set.Where(p => p.Name != null), oid, pid, s)
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
    }
}
