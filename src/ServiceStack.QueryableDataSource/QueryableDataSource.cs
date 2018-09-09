using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace ServiceStack
{
    public abstract class QueryableDataSource<T> : IQueryDataSource<T>
    {
        // Several Data Providers don't support Last, LastOrDefault, or Reverse
        // This is a flag to indicate whether it does or does not
        public abstract bool DataSourceSupportsLast { get; }

        public abstract IQueryable<T> InitQuery(IDataQuery q);

        public abstract IQueryable<From> InitQuery<From>(IDataQuery q);

        public virtual IQueryable<From> GetQuery<From>(IDataQuery q)
            => InitQuery<From>(q)
                .ApplyConditions(q.Conditions)
                .ApplySorting(q.OrderBy)
                .ApplyLimits(q.Offset, q.Rows)
                .ApplySpecificFields(q.OnlyFields);



        #region ServiceStack.IQueryDataSource

        public virtual int Count(IDataQuery q)
        {
            var query = InitQuery(q)
                    .ApplyLimits(q.Offset, q.Rows)
                    .ApplyConditions(q.Conditions);

            var result = query.Count();
            return result;
        }

        public abstract IDataQuery From<TSource>();

        public abstract List<Into> LoadSelect<Into, From>(IDataQuery q);

        public virtual object SelectAggregate(IDataQuery q, string name, IEnumerable<string> args)
        {
            name = name?.ToUpper() ?? throw new ArgumentNullException(nameof(name));

            if (name != "COUNT" && name != "MIN" && name != "MAX" && name != "AVG" && name != "SUM"
                && name != "FIRST" && name != "LAST")
                    return null;

            var firstArg = args.FirstOrDefault();

            // if COUNT with no or all selector, return Count 
            if (name == "COUNT" && (string.IsNullOrWhiteSpace(firstArg) || firstArg == "*"))
                return Count(q);

            var query = InitQuery(q)
                        .ApplyConditions(q.Conditions);

            object result;

            if (name == "FIRST" || (name == "LAST" && !DataSourceSupportsLast))
            {
                if (q.OrderBy == null)
                    q.OrderByPrimaryKey();

                var orderBy = q.OrderBy;

                // if provider doesn't support Last, invert sort and pick first
                if (name == "LAST") 
                {
                    for (var i = 0; i < q.OrderBy.OrderAsc.Length; i++)
                    {
                        orderBy.OrderAsc[i] = !q.OrderBy.OrderAsc[i];
                    }
                }

                query = query.ApplySorting(orderBy).ApplyLimits(q.Offset, q.Rows);

                if (string.IsNullOrWhiteSpace(firstArg) || firstArg == "*")
                {

                    result = query.FirstOrDefault();
                }
                else
                {
                    result = query.Select(firstArg).FirstOrDefault();
                }
            }
            else if (name == "LAST" && DataSourceSupportsLast)
            {
                query = query.ApplySorting(q.OrderBy).ApplyLimits(q.Offset, q.Rows);

                if (string.IsNullOrWhiteSpace(firstArg) || firstArg == "*")
                {
                    result = query.Last();
                }
                else
                {
                    result = query.Select(firstArg).Last();
                }
            }
            else
            {
                query = query
                    .ApplySorting(q.OrderBy)
                    .ApplyLimits(q.Offset, q.Rows);

                if (q.OnlyFields != null && q.OnlyFields.Count > 0)
                    query.GroupBy(firstArg, string.Join(",", q.OnlyFields));

                var n = (name == "AVG")
                            ? "Average" //  Method for AVG is Average 
                            : name.ToPascalCase(); // ex. SUM => Sum  (otherwise Method won't be found)

                result = query.Aggregate(n, firstArg);
            }

            return result;
        }

        #endregion ServiceStack.IQueryDataSource

        #region IDisposable

        public virtual void Dispose() { }

        #endregion IDisposable

    }

}
