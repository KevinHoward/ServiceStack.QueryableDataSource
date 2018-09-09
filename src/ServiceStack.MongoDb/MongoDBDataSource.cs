using System;
using System.Collections.Generic;
using System.Linq;

using MongoDB.Driver;

namespace ServiceStack.MongoDB
{
    public static class MongoDBDataSourceExtensions
    {
        public static IQueryDataSource<T> MongoDBDataSource<T>(this QueryDataContext context, string databaseId, string collectionId = null)
            => new MongoDBDataSource<T>(context, HostContext.TryResolve<IMongoClient>(), databaseId, collectionId);

        public static IQueryDataSource<T> MongoDBDataSource<T>(this QueryDataContext context, IMongoClient client, string databaseId, string collectionId = null)
            => new MongoDBDataSource<T>(context, client ?? HostContext.TryResolve<IMongoClient>(), databaseId, collectionId);

        public static IQueryDataSource<T> MongoDBDataSource<T>(this QueryDataContext context, IMongoDatabase database = null, string collectionId = null)
            => new MongoDBDataSource<T>(context, database ?? HostContext.TryResolve<IMongoDatabase>(), collectionId);
    }

    public class MongoDBDataSource<T> : QueryableDataSource<T>
    {
        private readonly QueryDataContext context;
        private readonly IMongoDatabase db;
        private readonly string collectionId;

        public override bool DataSourceSupportsLast { get => false; }

        public AggregateOptions AggregateOptions { get; set; } 
            = new AggregateOptions();

        public MongoDBDataSource(QueryDataContext context, IMongoClient client, string databaseId, string collectionId = null) 
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentNullException("databaseId");

            this.context = context;
            db = client.GetDatabase(databaseId);
            this.collectionId = collectionId ?? typeof(T).Name;
        }

        public MongoDBDataSource(QueryDataContext context, IMongoDatabase database, string collectionId = null)
        {
            this.context = context;
            db = database ?? throw new ArgumentNullException("database");
            this.collectionId = collectionId ?? typeof(T).Name;
        }

        public override IQueryable<T> InitQuery(IDataQuery q)
            => InitQuery<T>(q);

        public override IQueryable<From> InitQuery<From>(IDataQuery q)
            => db.GetCollection<From>(collectionId).AsQueryable(AggregateOptions);

        public override IDataQuery From<TSource>()
            => new DataQuery<TSource>(context);

        public override List<Into> LoadSelect<Into, From>(IDataQuery q)
        {
            var query = GetQuery<From>(q);

            var results = query.ConvertTo<IEnumerable<Into>>().ToList(); // materialize results 

            if (results.Count == 0)
                throw HttpError.NotFound("No results found.");

            return results;
        }
    }
}
