using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using ServiceStack.QueryableDataSource.Test;

namespace ServiceStack.QueryableDataSource.MongoDB.Test
{

    [TestFixture]
    public class MongoDBAutoQueryUnitTests
    {
        const string BaseUri = "http://localhost:2000/";
        IWebHost host;

        JsonServiceClient client;

        [OneTimeSetUp]
        public async Task TestFixtureSetUp()
        {
            host = WebHost.CreateDefaultBuilder()
                .UseStartup<StartUp>()
                .UseUrls(BaseUri)
                .Build();

            client = new JsonServiceClient(BaseUri);

            await host.StartAsync();
        }

        [OneTimeTearDown]
        public async Task TestTearDown()
        {
            await host.StopAsync();

            if (host != null)
            {
                host.Dispose();
                host = null;
            }
        }

        [Test]
        public void Can_GET_document_from_MongoDB_with_Name_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Name = "TestExample1" };
            var r = client.Get(request);

            Assert.AreEqual(1, r.Results.Count);
            Assert.AreEqual("TestExample1", r.Results[0].Name);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_with_NameStartsWith_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { NameStartsWith = "Test" };
            var r = client.Get(request);

            Assert.AreEqual(10, r.Results.Count);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_with_NameEndsWith_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { NameEndsWith = "0" };
            var r = client.Get(request);

            Assert.AreEqual(1, r.Results.Count);
            Assert.AreEqual("TestExample0", r.Results[0].Name);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_with_NumberGreaterThan_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { NumberGreaterThan = 5 };
            var r = client.Get(request);

            Assert.AreEqual(4, r.Results.Count);
            Assert.AreEqual(6, r.Results[0].Number);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_include_TOTAL_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Include = "TOTAL" };
            var r = client.Get(request);

            Assert.AreEqual(10, r.Total);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_take_5_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Take = 5, OrderBy = nameof(TestDocument.Name) };
            var r = client.Get(request);

            Assert.AreEqual(5, r.Results.Count);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_skip_2_take_5_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Skip = 2, Take = 5, OrderBy = nameof(TestDocument.Name) };
            var r = client.Get(request);

            // skips items 0 and 1, then returns 2 through 6
            Assert.AreEqual(2, r.Results.FirstOrDefault()?.Number ?? -1);
            Assert.AreEqual(6, r.Results.LastOrDefault()?.Number ?? -1);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_include_single_aggregate_AutoQueryDataSource()
        {
            // SELECT COUNT(*) Total FROM c
            var request = new TestQueryRequest() { Include = "COUNT(*) Total" };

            var r = client.Get(request);

            Assert.AreEqual(10, r.Meta["Total"]?.ToInt());
        }

        [Test]
        public void Can_GET_document_from_MongoDB_SUM_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Include = "SUM(Number) NumberSum" };

            var r = client.Get(request);

            Assert.AreEqual("45", r.Meta["NumberSum"]);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_AVG_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Include = "AVG(Number) NumberAvg" };

            var r = client.Get(request);

            Assert.AreEqual(4.5, r.Meta["NumberAvg"].ToDouble());
        }

        [Test]
        public void Can_GET_document_from_MongoDB_MIN_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Include = "MIN(Number) MinNumber" };

            var r = client.Get(request);

            Assert.AreEqual(0, r.Meta["MinNumber"].ToInt());
        }

        [Test]
        public void Can_GET_document_from_MongoDB_MAX_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Include = "MAX(Number) MaxNumber" };

            var r = client.Get(request);

            Assert.AreEqual(9, r.Meta["MaxNumber"].ToInt());
        }

        [Test]
        public void Can_GET_document_from_MongoDB_FIRST_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Include = "FIRST(Name) FirstName" };

            var r = client.Get(request);

            Assert.AreEqual("TestExample0", r.Meta["FirstName"]);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_LAST_AutoQueryDataSource()
        {
            var request = new TestQueryRequest() { Include = "LAST(Name) LastName" };

            var r = client.Get(request);

            Assert.AreEqual("TestExample9", r.Meta["LastName"]);
        }

        [Test]
        public void Can_GET_document_from_MongoDB_include_comma_delimited_aggregates_AutoQueryDataSource()
        {
            var aggregations = new[] { "COUNT(*) as TotalCount", "SUM(Number) as NumberSum", "FIRST(Name) as FirstName" };
            var includes = string.Join(", ", aggregations);
            var request = new TestQueryRequest() { Include = includes };

            var r = client.Get(request);

            Assert.AreEqual("10", r.Meta["TotalCount"]);
            Assert.AreEqual("45", r.Meta["NumberSum"]);
            Assert.AreEqual("TestExample0", r.Meta["FirstName"]);
        }
    }
}
