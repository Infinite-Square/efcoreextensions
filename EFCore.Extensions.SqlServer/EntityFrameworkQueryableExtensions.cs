using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EFCore.Extensions.SqlServer
{
    public static class EntityFrameworkQueryableExtensions
    {

        public static ISystemTimeQueryable<TEntity> ForSystemTime<TEntity>(this IQueryable<TEntity> source)
        {
            return new SystemTimeQueryable<TEntity>(source);
        }

        internal static readonly MethodInfo ForSystemTimeAsOfMethodInfo
            = typeof(EntityFrameworkQueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods(nameof(ForSystemTimeAsOf))
                .Single();

        internal static IQueryable<TEntity> ForSystemTimeAsOf<TEntity>(this IQueryable<TEntity> _queryable, [NotParameterized] DateTime dateTime)
        {
            return _queryable;
        }

        private class SystemTimeQueryable<TEntity> : ISystemTimeQueryable<TEntity>, IAsyncEnumerable<TEntity>
        {
            private readonly IQueryable<TEntity> _queryable;

            public SystemTimeQueryable(IQueryable<TEntity> queryable)
            {
                _queryable = queryable;
            }

            public Expression Expression => _queryable.Expression;
            public Type ElementType => _queryable.ElementType;
            public IQueryProvider Provider => _queryable.Provider;

            public IEnumerator<TEntity> GetEnumerator() => _queryable.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetEnumerator()
                => ((IAsyncEnumerable<TEntity>)_queryable).GetEnumerator();

            public IQueryable<TEntity> AsOf(DateTime dateTime)
            {
                return _queryable.Provider is EntityQueryProvider
                    ? _queryable.Provider.CreateQuery<TEntity>(
                        Expression.Call(null
                            , ForSystemTimeAsOfMethodInfo.MakeGenericMethod(typeof(TEntity))
                            , _queryable.Expression
                            , Expression.Constant(dateTime)))
                    : _queryable;
            }
        }
    }

    public interface ISystemTimeQueryable<out TEntity> : IQueryable<TEntity>
    {
        IQueryable<TEntity> AsOf(DateTime dateTime);
    }
}
