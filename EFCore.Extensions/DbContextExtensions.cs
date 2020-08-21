using EFCore.Extensions.ChangeTracker;
using EFCore.Extensions.SqlCommandCatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EFCore.Extensions
{
    public static class DbContextExtensions
    {
        public static IQueryable<JsonResult<T>> Json<T>(this DbContext self)
        {
            return self.Set<JsonResult<T>>();
        }

        public static ISqlCommandCatcher GetSqlCommandCatcher<TContext>(this TContext self)
            where TContext : DbContext
        {
            var provider = (IInfrastructure<IServiceProvider>)self;
            var catcher = provider.GetService<ISqlCommandCatcher>();
            return catcher;
        }

        public static string CatchFirstRawSql<TContext>(this TContext self, Action<TContext> action)
            where TContext : DbContext
        {
            using (var scope = self.GetSqlCommandCatcher().EnableCatching())
            {
                action(self);
                var info = scope.Commands.FirstOrDefault();
                if (info == null) return null;
                return info.Command.CommandText;
            }
        }

        public static async Task<string> CatchFirstRawSqlAsync<TContext>(this TContext self, Func<TContext, Task> action)
            where TContext : DbContext
        {
            using (var scope = self.GetSqlCommandCatcher().EnableCatching())
            {
                await action(self);
                var info = scope.Commands.FirstOrDefault();
                if (info == null) return null;
                return info.Command.CommandText;
            }
        }

        public static ChangeTrackerWatcher GetChangeTrackerWatcher(this DbContext self)
        {
            return new ChangeTrackerWatcher(self);
        }
    }
}
