using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Funq;

using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

using ServiceStack.Configuration;

using ServiceStack.RavenDb;
using ServiceStack.Logging;

using ServiceStack.QueryableDataSource.Test;

namespace ServiceStack.QueryableDataSource.RavenDb.Test
{
    public class RavenDbAppHost : AppHostBase
    {
        //Read config
        private static string[] endpointUrls;
        private static string authorizationKey;
        private static string databaseId;
        private static string collectionId;

        public RavenDbAppHost() : base(nameof(RavenDbAppHost), typeof(RavenDbAppHost).Assembly) { }

        public override void Configure(Container container)
        {
            base.SetConfig(new HostConfig
            {
                DebugMode = true,
            });

            AppSettings = new MultiAppSettings(
                new EnvironmentVariableSettings(),
                //new TextFileSettings("~/app.settings".MapHostAbsolutePath()),  
                new AppSettings()); // fallback to Web.confg

            endpointUrls = AppSettings.Get<string>("RavenDb.EndPointUrls").Split(",");
            authorizationKey = AppSettings.Get<string>("RavenDb.AuthorizationKey");
            databaseId = AppSettings.Get<string>("RavenDb.DatabaseId");

            var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(authorizationKey.ToAsciiBytes());

            // create a reference to document store 
            var docStore = new DocumentStore() {
                Database = databaseId, 
                Certificate = cert, 
                Urls = endpointUrls
            }.Initialize();

            container.Register(c => docStore.OpenSession());

            LogManager.LogFactory = new DebugLogFactory(debugEnabled: true);

            var docStoreSession = Resolve<IDocumentSession>();

            InitDatabase(docStoreSession).GetAwaiter().GetResult();

            Plugins.Add(new AutoQueryDataFeature() { MaxLimit = 100 }
                .AddDataSource(ctx => ctx.RavenDbDataSource<TestDocument>(docStoreSession, databaseId, collectionId)));
        }

        private async Task InitDatabase(IDocumentSession docStoreSession)
        {
            //await docStoreSession.GetOrCreateDatabaseAsync(databaseId);
            //await docStoreSession.DeleteDocumentCollectionIfExistsAsync(databaseId, collectionId);
            //await docStoreSession.GetOrCreateCollectionAsync(databaseId, collectionId);
            //await docStoreSession.AddTestDocuments(databaseId, collectionId);
        }
    }

    public static class RavenDbStoreHelpers
    {
        //public static async Task<DocumentStore> GetOrCreateDatabaseAsync(this IDocumentSession client, string databaseId)
        //{
        //    try
        //    {
        //        var response = await client.Advanced.DocumentStore.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
        //        return response.Resource;
        //    }
        //    catch (Raven.Client.Exceptions.RavenException e)
        //    {
        //        if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        //        {
        //            var response = await client.CreateDatabaseAsync(new Database { Id = databaseId });
        //            return response.Resource;
        //        }
        //        else
        //        {
        //            throw e.InnerException;
        //        }
        //    }
        //}

        //public static async Task<DocumentCollection> GetOrCreateCollectionAsync(this IDocumentClient client, string databaseId, string collectionId, RequestOptions requestOptions = null)
        //{
        //    try
        //    {
        //        var response = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId), requestOptions);
        //        return response.Resource;
        //    }
        //    catch (DocumentClientException e)
        //    {
        //        if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        //        {
        //            var docCollection = new DocumentCollection { Id = collectionId };
        //            docCollection.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
        //            docCollection.IndexingPolicy.IndexingMode = IndexingMode.Consistent;

        //            var response = await client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(databaseId), docCollection, requestOptions);
        //            response.Resource.IndexingPolicy = new IndexingPolicy();
        //            return response.Resource;
        //        }
        //        else
        //        {
        //            throw e.InnerException;
        //        }
        //    }
        //}

        //public static async Task<Document> GetOrCreateDocumentAsync(this IDocumentClient client, string databaseId, string collectionId, object document, RequestOptions requestOptions = null, string partitionKey = null)
        //{
        //    var response = await client.UpsertDocumentAsync(
        //        UriFactory.CreateDocumentCollectionUri(databaseId, collectionId),
        //        document,
        //        new RequestOptions { ConsistencyLevel = ConsistencyLevel.Session });

        //    return response.Resource;
        //}

        //public static async Task<List<TestDocument>> AddTestDocuments(this IDocumentClient client, string databaseId, string collectionId, RequestOptions requestOptions = null)
        //{
        //    var results = new List<TestDocument>();

        //    // create some documents
        //    for (var i = 0; i < 10; i++)
        //    {
        //        var docName = $"TestExample{i}";
        //        var doc = new TestDocument { Number = i, Name = docName };
        //        await client.GetOrCreateDocumentAsync(databaseId, collectionId, doc, requestOptions ?? new RequestOptions { ConsistencyLevel = ConsistencyLevel.Session });

        //        results.Add(doc);
        //    }

        //    return results;
        //}

        //public static async Task DeleteDocumentCollectionIfExistsAsync(this IDocumentClient client, string databaseId, string collectionId, RequestOptions requestOptions = null)
        //{
        //    try
        //    {
        //        var response = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId), requestOptions);
        //        await client.DeleteDocumentCollectionAsync(response.Resource.SelfLink, requestOptions);
        //    }
        //    catch (DocumentClientException e)
        //    {
        //        if (e.StatusCode != System.Net.HttpStatusCode.NotFound)
        //            throw e.InnerException;
        //    }

        //}
    }

}
