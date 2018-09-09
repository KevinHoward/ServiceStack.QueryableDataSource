using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Funq;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

using ServiceStack.Configuration;

using ServiceStack.Azure.CosmosDb;
using ServiceStack.Logging;

using ServiceStack.QueryableDataSource.Test;

namespace ServiceStack.QueryableDataSource.CosmosDb.Test
{
    public class CosmosDbAppHost : AppHostBase
    {
        //Read config
        private static string endpointUrl;
        private static string authorizationKey;
        private static string databaseId;
        private static string collectionId;

        public CosmosDbAppHost() : base(nameof(CosmosDbAppHost), typeof(CosmosDbAppHost).Assembly) { }

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

            endpointUrl = AppSettings.Get<string>("CosmosDb.EndPointUrl");
            authorizationKey = AppSettings.Get<string>("CosmosDb.AuthorizationKey");
            databaseId = AppSettings.Get<string>("CosmosDb.DatabaseId");
            collectionId = AppSettings.Get<string>("CosmosDb.CollectionId");

            // create a client
            var docClient = new DocumentClient(
                new Uri(endpointUrl),
                authorizationKey);

            container.Register<IDocumentClient>(c => docClient);

            LogManager.LogFactory = new DebugLogFactory(debugEnabled: true);

            var requestOptions = new RequestOptions { ConsistencyLevel = ConsistencyLevel.Session };

            InitDatabase(docClient).GetAwaiter().GetResult();

            Plugins.Add(new AutoQueryDataFeature() { MaxLimit = 100 }
                .AddDataSource(ctx => ctx.CosmosDbDataSource<TestDocument>(docClient, databaseId, collectionId, requestOptions)));
        }

        private async Task InitDatabase(DocumentClient docClient)
        {
            await docClient.GetOrCreateDatabaseAsync(databaseId);
            await docClient.DeleteDocumentCollectionIfExistsAsync(databaseId, collectionId);
            await docClient.GetOrCreateCollectionAsync(databaseId, collectionId);
            await docClient.AddTestDocuments(databaseId, collectionId);
        }
    }

    public static class CosmosDbDatabaseHelpers
    {
        public static async Task<Database> GetOrCreateDatabaseAsync(this IDocumentClient client, string databaseId)
        {
            try
            {
                var response = await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
                return response.Resource;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var response = await client.CreateDatabaseAsync(new Database { Id = databaseId });
                    return response.Resource;
                }
                else
                {
                    throw e.InnerException;
                }
            }
        }

        public static async Task<DocumentCollection> GetOrCreateCollectionAsync(this IDocumentClient client, string databaseId, string collectionId, RequestOptions requestOptions = null)
        {
            try
            {
                var response = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId), requestOptions);
                return response.Resource;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var docCollection = new DocumentCollection { Id = collectionId };
                    docCollection.IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });
                    docCollection.IndexingPolicy.IndexingMode = IndexingMode.Consistent;

                    var response = await client.CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(databaseId), docCollection, requestOptions);
                    response.Resource.IndexingPolicy = new IndexingPolicy();
                    return response.Resource;
                }
                else
                {
                    throw e.InnerException;
                }
            }
        }

        public static async Task<Document> GetOrCreateDocumentAsync(this IDocumentClient client, string databaseId, string collectionId, object document, RequestOptions requestOptions = null, string partitionKey = null)
        {
            var response = await client.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseId, collectionId),
                document,
                new RequestOptions { ConsistencyLevel = ConsistencyLevel.Session });

            return response.Resource;
        }

        public static async Task<List<TestDocument>> AddTestDocuments(this IDocumentClient client, string databaseId, string collectionId, RequestOptions requestOptions = null)
        {
            var results = new List<TestDocument>();

            // create some documents
            for (var i = 0; i < 10; i++)
            {
                var docName = $"TestExample{i}";
                var doc = new TestDocument { Number = i, Name = docName };
                await client.GetOrCreateDocumentAsync(databaseId, collectionId, doc, requestOptions ?? new RequestOptions { ConsistencyLevel = ConsistencyLevel.Session });

                results.Add(doc);
            }

            return results;
        }

        public static async Task DeleteDocumentCollectionIfExistsAsync(this IDocumentClient client, string databaseId, string collectionId, RequestOptions requestOptions = null)
        {
            try
            {
                var response = await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId), requestOptions);
                await client.DeleteDocumentCollectionAsync(response.Resource.SelfLink, requestOptions);
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode != System.Net.HttpStatusCode.NotFound)
                    throw e.InnerException;
            }

        }
    }

}
