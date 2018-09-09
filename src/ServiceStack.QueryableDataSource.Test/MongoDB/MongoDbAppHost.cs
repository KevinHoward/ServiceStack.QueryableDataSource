using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Funq;

using MongoDB.Driver;

using ServiceStack.Configuration;

using ServiceStack.MongoDB;
using ServiceStack.Logging;

using ServiceStack.QueryableDataSource.Test;

namespace ServiceStack.QueryableDataSource.MongoDB.Test
{
    public class MongoDBAppHost : AppHostBase
    {
        //Read config
        private static string connectionString;
        private static string databaseId;
        private static string collectionId;

        public MongoDBAppHost() : base(nameof(MongoDBAppHost), typeof(MongoDBAppHost).Assembly) { }

        public override void Configure(Container container)
        {
            base.SetConfig(new HostConfig
            {
                DebugMode = true,
            });

            AppSettings = new MultiAppSettings(
                new EnvironmentVariableSettings(),
                new AppSettings(),
                //new TextFileSettings("~/app.settings".MapHostAbsolutePath()),  
                new AppSettings()); // fallback to Web.confg

            connectionString = AppSettings.Get<string>("MongoDB.ConnectionString");
            databaseId = AppSettings.Get<string>("MongoDB.DatabaseId");
            collectionId = AppSettings.Get<string>("MongoDB.CollectionId");

            container.Register<IMongoClient>(c => new MongoClient(new MongoUrl(connectionString)));

            LogManager.LogFactory = new DebugLogFactory(debugEnabled: true);

            var docClient = Resolve<IMongoClient>();

            InitDatabase(docClient).GetAwaiter().GetResult();

            Plugins.Add(new AutoQueryDataFeature()
                .AddDataSource(ctx => ctx.MongoDBDataSource<TestDocument>(docClient, databaseId, collectionId)));
        }

        private async Task InitDatabase(IMongoClient docClient)
        {
            var database = docClient.GetDatabase(databaseId);

            await database.ClearTestCollection(collectionId);
            await database.AddTestDocuments(collectionId);
        }
    }

    public static class DatabaseHelpers
    {
        public static async Task ClearTestCollection(this IMongoDatabase database, string collectionId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var collection = database.GetCollection<TestDocument>(collectionId);

            await collection.DeleteManyAsync(x => true, cancellationToken);
        }

        public static async Task<List<TestDocument>> AddTestDocuments(this IMongoDatabase database, string collectionId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var collection = database.GetCollection<TestDocument>(collectionId);
            var documents = new List<WriteModel<TestDocument>>();

            var results = new List<TestDocument>();

            // create some documents
            for (var i = 0; i < 10; i++)
            {
                var docName = $"TestExample{i}";
                var doc = new TestDocument { Id = i.ToString(), Number = i, Name = docName };
                await collection.InsertOneAsync(doc, new InsertOneOptions() { BypassDocumentValidation = false }, cancellationToken);

                results.Add(doc);
            }

            return results;
        }
    }

}
