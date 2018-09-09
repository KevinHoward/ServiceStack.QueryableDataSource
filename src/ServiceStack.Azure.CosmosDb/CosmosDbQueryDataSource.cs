using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;


using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace ServiceStack.Azure.CosmosDb
{
    public class CosmosDbQueryDataSource<T> : QueryableDataSource<T>
    {
        private readonly QueryDataContext context;
        private readonly IDocumentClient client;
        private readonly string databaseId;
        private readonly string collectionId;
        private readonly string partitionKey;
        private readonly ConnectionPolicy connectionPolicy;
        private readonly RequestOptions requestOptions;

        public override bool DataSourceSupportsLast { get => false; }

        public CosmosDbQueryDataSource(QueryDataContext context, IDocumentClient client, string databaseId, string collectionId = null, ConnectionPolicy connectionPolicy = null, RequestOptions requestOptions = null)
            : this(context, client, databaseId, collectionId, null, connectionPolicy, requestOptions)
        { }

        public CosmosDbQueryDataSource(QueryDataContext context, IDocumentClient client, string databaseId, string collectionId = null, string partitionKey = null, ConnectionPolicy connectionPolicy = null, RequestOptions requestOptions = null)
        {
            this.context = context;
            this.client = client;
            this.databaseId = databaseId ?? throw new ArgumentNullException("databaseId");
            this.partitionKey = partitionKey;
            this.collectionId = collectionId ?? typeof(T).Name;
            this.connectionPolicy = connectionPolicy ?? new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };
            this.requestOptions = requestOptions ?? new RequestOptions
            {
                ConsistencyLevel = ConsistencyLevel.Session
            };
        }



        public override IQueryable<T> InitQuery(IDataQuery q)
            => InitQuery<T>(q, null);

        public override IQueryable<From> InitQuery<From>(IDataQuery q)
            => InitQuery<From>(q, null);

        public IQueryable<From> InitQuery<From>(IDataQuery q, string continuationToken)
        {
            var options = new FeedOptions
            {
                EnableScanInQuery = true,
                MaxItemCount = (!q.Rows.HasValue) 
                    ? -1  // set max item if provided, else set to -1 for no limit
                    : q.Rows.Value + (q.Offset ?? 0)  // limit to only needed records  
            };

            // if no provided PartionKey, enable cross partition querying 
            if (string.IsNullOrEmpty(partitionKey))
                options.EnableCrossPartitionQuery = true;
            else
                options.PartitionKey = new PartitionKey(partitionKey);

            // append continuation token is provided 
            if (!string.IsNullOrEmpty(continuationToken))
                options.RequestContinuation = continuationToken;

            return client.CreateDocumentQuery<From>(
                  UriFactory.CreateDocumentCollectionUri(databaseId, collectionId),
                  options);
        }

        public override IQueryable<From> GetQuery<From>(IDataQuery q)
            => GetQuery<From>(q, null);

        // CosmosDb doesn't support Take/Skip (continuation token with sized queue will handle this short-fall)
        public IQueryable<From> GetQuery<From>(IDataQuery q, string continuationToken)
            => InitQuery<From>(q, continuationToken)
                .ApplyConditions(q.Conditions)
                .ApplySorting(q.OrderBy)
                .ApplySpecificFields(q.OnlyFields)
                .ApplyLimits(null, (q.Rows.HasValue) ? q.Rows.Value + (q.Offset ?? 0) : (int?)null);

        #region ServiceStack.IQueryDataSource

        public override IDataQuery From<TSource>()
            => new DataQuery<TSource>(context);

        public override List<Into> LoadSelect<Into, From>(IDataQuery q)
            => LoadSelectAsync<Into, From>(q).Result;

        public override object SelectAggregate(IDataQuery q, string name, IEnumerable<string> args)
        {
            name = name?.ToUpper() ?? throw new ArgumentNullException(nameof(name));

            if (name != "COUNT" && name != "MIN" && name != "MAX" && name != "AVG" && name != "SUM"
                && name != "FIRST" && name != "LAST")
                return null;

            var firstArg = args.FirstOrDefault();

            // if COUNT with no or all selector, return Count 
            if (name == "COUNT" && (firstArg == null || firstArg == "*"))
                return Count(q);

            var query = InitQuery(q)
                .ApplyConditions(q.Conditions);

            object result;

            if (name == "FIRST" || name == "LAST")
            {
                if (q.OrderBy == null)
                    q.OrderByPrimaryKey();

                var orderBy = q.OrderBy;

                // invert sort and pick first
                if (name == "LAST")
                {
                    for (var i = 0; i < q.OrderBy.OrderAsc.Length; i++)
                    {
                        orderBy.OrderAsc[i] = !q.OrderBy.OrderAsc[i];
                    }
                }

                query = query.ApplySorting(orderBy);

                if (string.IsNullOrWhiteSpace(firstArg) || firstArg == "*")
                {

                    result = query.ApplyLimits(0,1).AsEnumerable<T>().FirstOrDefault();
                }
                else
                {
                    result = query.Select(firstArg).ApplyLimits(0, 1).AsEnumerable().FirstOrDefault();
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

        public async Task<List<Into>> LoadSelectAsync<Into, From>(IDataQuery q, CancellationToken cancellationToken = default(CancellationToken))
            => (await QueryAsync(q, cancellationToken)).Select(x => x.ConvertTo<Into>()).ToList();

        private async Task<IEnumerable<T>> QueryAsync(IDataQuery q, CancellationToken cancellationToken = default(CancellationToken))
        {
            // set queue size if provided (fallsback to feed option MaxLimit setting)
            var resultQueue = (q.Rows.HasValue)
                                    ? new Queue<T>(q.Rows.Value)
                                    : new Queue<T>();

            string continuationToken = null;

            // create a negative offset to skip results 
            var offset = -1 * q.Offset ?? 0;

            do
            {
                var pagedResults = await GetQuery<T>(q, continuationToken).ToPagedResults(cancellationToken);

                continuationToken = pagedResults.ContinuationToken;

                foreach (var item in pagedResults.Results)
                {
                    // don't add results until past offset 
                    if (offset >= 0)
                        resultQueue.Enqueue(item);

                    offset++;
                }
            }
            while (!string.IsNullOrEmpty(continuationToken));

            return resultQueue;
        }
    }

    public static class CosmosDbQueryDataSourceExtensions
    {
        public static IQueryDataSource<T> CosmosDbDataSource<T>(this QueryDataContext context, string databaseId, string collectionId = null, ConnectionPolicy connectionPolicy = null, RequestOptions requestOptions = null)
            => new CosmosDbQueryDataSource<T>(context, HostContext.TryResolve<IDocumentClient>(), databaseId, collectionId, connectionPolicy, requestOptions);

        public static IQueryDataSource<T> CosmosDbDataSource<T>(this QueryDataContext context, IDocumentClient client, string databaseId, string collectionId = null, ConnectionPolicy connectionPolicy = null, RequestOptions requestOptions = null)
            => new CosmosDbQueryDataSource<T>(context, client ?? HostContext.TryResolve<IDocumentClient>(), databaseId, collectionId, connectionPolicy, requestOptions);

        public static IQueryDataSource<T> CosmosDbDataSource<T>(this QueryDataContext context, IDocumentClient client, string databaseId, string collectionId, RequestOptions requestOptions = null)
            => new CosmosDbQueryDataSource<T>(context, client ?? HostContext.TryResolve<IDocumentClient>(), databaseId, collectionId, null, requestOptions);

        public static IQueryDataSource<T> CosmosDbDataSource<T>(this QueryDataContext context, IDocumentClient client, string databaseId, string collectionId, string partitionKey)
            => new CosmosDbQueryDataSource<T>(context, client ?? HostContext.TryResolve<IDocumentClient>(), databaseId, collectionId, partitionKey);

        public static async Task<CosmosDbContinuedResults<T>> ToPagedResults<T>(this IQueryable<T> query, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var documentQuery = query.AsDocumentQuery())
            {
                var results = new CosmosDbContinuedResults<T>();

                do
                {
                    var queryResult = await documentQuery.ExecuteNextAsync<T>(cancellationToken);

                    results.ContinuationToken = queryResult.ResponseContinuation;
                    results.Results.AddRange(queryResult);
                }
                while (documentQuery.HasMoreResults);

                return results;
            }
        }

        public static async Task<CosmosDbContinuedResults<dynamic>> ToPagedResults(this IQueryable<dynamic> query, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var documentQuery = query.AsDocumentQuery())
            {
                var results = new CosmosDbContinuedResults<dynamic>();

                do
                {
                    var queryResult = await documentQuery.ExecuteNextAsync<dynamic>(cancellationToken);

                    results.ContinuationToken = queryResult.ResponseContinuation;
                    results.Results.AddRange(queryResult);
                }
                while (documentQuery.HasMoreResults);

                return results;
            }
        }
    }

    public class CosmosDbContinuedResults<T>
    {
        public string ContinuationToken { get; set; }

        public List<T> Results { get; set; } = new List<T>();
    }

}
