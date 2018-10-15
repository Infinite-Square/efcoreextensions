using EFCore.Extensions.SqlCommandCatching;
using EFCore.Extensions.SqlConnectionUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCore.Extensions
{
    public static class DbContextExtensions
    {
        public static string CatchFirstRawSql<TContext>(this TContext self, Action<TContext> action)
            where TContext : DbContext
        {
            var provider = (IInfrastructure<IServiceProvider>)self;
            var catcher = provider.GetService<ISqlCommandCatcher>();
            using (var scope = catcher.EnableCatching())
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
            var provider = (IInfrastructure<IServiceProvider>)self;
            var catcher = provider.GetService<ISqlCommandCatcher>();
            using (var scope = catcher.EnableCatching())
            {
                await action(self);
                var info = scope.Commands.FirstOrDefault();
                if (info == null) return null;
                return info.Command.CommandText;
            }
        }
    }
}
