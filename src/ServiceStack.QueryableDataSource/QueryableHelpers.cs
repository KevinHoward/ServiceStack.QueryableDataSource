using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace ServiceStack
{
    public static class QueryableHelpers
    {
        public static IQueryable<T> ApplyConditions<T>(this IQueryable<T> query, IEnumerable<DataConditionExpression> conditions)
        {
            // TODO: Add some paranethesis for OR and AND to work together

            var c = conditions.Count();

            if (c == 0)
                return query;

            var predicate = string.Empty;
            var values = new List<object>();
            var i = 0;

            foreach (var condition in conditions)
            {
                switch (condition.QueryCondition.GetType().Name)
                {
                    // binary operators 
                    case nameof(EqualsCondition): // ==
                    case nameof(NotEqualCondition): // !=
                    case nameof(LessCondition): // <
                    case nameof(LessEqualCondition): // <=
                    case nameof(GreaterCondition): // >
                    case nameof(GreaterEqualCondition): // >=
                        predicate += $"{condition.Field.Name} {condition.QueryCondition.Alias} @{i}";
                        values.Add(condition.Value);
                        break;
                    case nameof(StartsWithCondition):  // LIKE @%
                        predicate += $"{condition.Field.Name}.StartsWith(@{i})";
                        values.Add(condition.Value);
                        break;
                    case nameof(EndsWithCondition):  // LIKE %@
                        predicate += $"{condition.Field.Name}.EndsWith(@{i})";
                        values.Add(condition.Value);
                        break;
                    case nameof(ContainsCondition):  // CONTAINS(@) same as LIKE %@%
                    case nameof(InCollectionCondition):  // IN (@)
                        predicate += $"{condition.Field.Name}.Contains(@{i})";
                        values.Add(condition.Value);
                        break;
                    case nameof(InBetweenCondition):  // BETWEEN @ AND @
                        predicate += $"{condition.Field.Name} Between @{i} and @{i + 1}";
                        var s = condition.Value?.ToString().Split(',');
                        values.AddRange(s);
                        i++;
                        break;
                    default:
                        break;
                }

                if (conditions.Last() != condition)
                {
                    if (condition.Term == QueryTerm.Or)
                        predicate += (condition.Term == QueryTerm.Or) ? " OR " : " AND ";
                }

                i++;
            }

            query = query.Where(predicate, values.ToArray());

            return query;
        }

        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, OrderByExpression orderBy)
        {
            if (orderBy == null || orderBy.FieldNames.Length == 0)
                return query;

            var condition = new List<string>();
            for (int i = 0; i < orderBy.FieldNames.Length; i++)
            {
                var field = orderBy.FieldNames[i];
                var direction = orderBy.OrderAsc[i] ? "ASC" : "DESC";

                condition.Add(field + " " + direction);
            }

            return query.OrderBy(condition.Join(","));
        }

        public static IQueryable<T> ApplyLimits<T>(this IQueryable<T> query, int? skip, int? take)
        {
            if (skip.HasValue && skip.Value > 0)
                query = query.Skip(skip.Value);

            if (take.HasValue && take.Value >= 1)
                query = query.Take(take.Value);

            return query;
        }

        public static IQueryable<T> ApplySpecificFields<T>(this IQueryable<T> query, IEnumerable<string> fields)
        {
            if (fields == null || fields.Count() == 0)
                return query;

            var columns = string.Join(",", fields);

            return query.Select("T(@0)", columns).Cast<T>();
        }



        public static IQueryable ApplyConditions(this IQueryable query, IEnumerable<DataConditionExpression> conditions)
        {
            // TODO: Add some paranethesis for OR and AND to work together

            var c = conditions.Count();

            if (c == 0)
                return query;

            var predicate = string.Empty;
            var values = new List<object>();
            var i = 0;

            foreach (var condition in conditions)
            {
                switch (condition.QueryCondition.GetType().Name)
                {
                    // binary operators 
                    case nameof(EqualsCondition): // ==
                    case nameof(NotEqualCondition): // !=
                    case nameof(LessCondition): // <
                    case nameof(LessEqualCondition): // <=
                    case nameof(GreaterCondition): // >
                    case nameof(GreaterEqualCondition): // >=
                        predicate += $"{condition.Field.Name} {condition.QueryCondition.Alias} @{i}";
                        values.Add(condition.Value);
                        break;
                    case nameof(StartsWithCondition):  // LIKE @%
                        predicate += $"{condition.Field.Name}.StartsWith(@{i})";
                        values.Add(condition.Value);
                        break;
                    case nameof(EndsWithCondition):  // LIKE %@
                        predicate += $"{condition.Field.Name}.EndsWith(@{i})";
                        values.Add(condition.Value);
                        break;
                    case nameof(ContainsCondition):  // CONTAINS(@) same as LIKE %@%
                    case nameof(InCollectionCondition):  // IN (@)
                        predicate += $"{condition.Field.Name}.Contains(@{i})";
                        values.Add(condition.Value);
                        break;
                    case nameof(InBetweenCondition):  // BETWEEN @ AND @
                        predicate += $"{condition.Field.Name} Between @{i} and @{i + 1}";
                        var s = condition.Value?.ToString().Split(',');
                        values.AddRange(s);
                        i++;
                        break;
                    default:
                        break;
                }

                if (conditions.Last() != condition)
                {
                    if (condition.Term == QueryTerm.Or)
                        predicate += (condition.Term == QueryTerm.Or) ? " OR " : " AND ";
                }

                i++;
            }

            query = query.Where(predicate, values.ToArray());

            return query;
        }

        public static IQueryable ApplySorting(this IQueryable query, OrderByExpression orderBy)
        {
            if (orderBy == null)
                return query;

            if (orderBy.FieldNames.Length == 0)
                return query;

            var condition = new List<string>();
            for (int i = 0; i < orderBy.FieldNames.Length; i++)
            {
                var field = orderBy.FieldNames[i];
                var direction = orderBy.OrderAsc[i] ? "ASC" : "DESC";

                condition.Add(field + " " + direction);
            }

            return query.OrderBy(condition.Join(","));
        }

        public static IQueryable ApplyLimits(this IQueryable query, int? skip, int? take)
        {
            if (skip.HasValue && skip.Value > 0)
                query = query.Skip(skip.Value);

            if (take.HasValue && take.Value >= 1)
                query = query.Take(take.Value);

            return query;
        }

        public static IQueryable ApplySpecificFields(this IQueryable query, IEnumerable<string> fields)
        {
            if (fields == null || fields.Count() == 0)
                return query;

            if (fields.Count() == 1)
            {
                return query.Select("@0", fields.First());
            }
            else
            {
                var columns = string.Join(",", fields);

                return query.Select("new(@0)", columns);
            }
        }
    }
}
