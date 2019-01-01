using System;
using System.Collections.Generic;
using System.Linq;

using Raven.Client.Documents.Session;

namespace ServiceStack.RavenDb
{
    public static class RavenDbQueryDataSourceExtensions
    {
        public static IQueryDataSource<T> RavenDbDataSource<T>(this QueryDataContext context, IDocumentSession session, string collectionId = null, string indexName = null, bool isMapReduce = false)
            => new RavenDbQueryDataSource<T>(context, session ?? HostContext.TryResolve<IDocumentSession>(), collectionId, indexName, isMapReduce);
    }

    public class RavenDbQueryDataSource<T> : QueryableDataSource<T>
    {
        private readonly QueryDataContext context;
        private readonly IDocumentSession session;
        private readonly string collectionId;
        private readonly string indexName;
        private readonly bool isMapReduce;

        public RavenDbQueryDataSource(QueryDataContext context, IDocumentSession session, string collectionId = null, string indexName = null, bool isMapReduce = false) 
        {
            this.context = context;
            this.session = session;
            this.collectionId = collectionId ?? typeof(T).Name;
            this.indexName = indexName;
            this.isMapReduce = isMapReduce;
        }

        public override bool DataSourceSupportsLast { get => true; }

        public override IQueryable<T> InitQuery(IDataQuery q)
            => InitQuery<T>(q);

        public override IQueryable<From> InitQuery<From>(IDataQuery q)
            => (String.IsNullOrWhiteSpace(indexName))
                ? session.Query<From>(null, collectionId, isMapReduce)
                : session.Query<From>(indexName, collectionId, isMapReduce);

        public override IDataQuery From<TSource>()
            => new DataQuery<TSource>(context);

        public override List<Into> LoadSelect<Into, From>(IDataQuery q)
        {
            var results = GetQuery<From>(q).ConvertTo<IEnumerable<Into>>().ToList(); // materialize results 

            if (results.Count == 0)
                throw HttpError.NotFound("No results found.");

            return results;
        }
    }
}
