using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;

namespace ServiceStack.QueryableDataSource.Test
{
    public class TestDocument
    {
        [Alias("_id")]
        public string Id { get; set; }

        public int Number { get; set; }

        public string Name { get; set; }
    }

    [Route("/query/test")]
    public class TestQueryRequest : QueryData<TestDocument>
    {
        public string Name { get; set; }

        public string NameStartsWith { get; set; }
        public string NameEndsWith { get; set; }
        public string NameContains { get; set; }

        public int? Number { get; set; }

        public int? NumberLessThan { get; set; }
        public int? NumberGreaterThan { get; set; }
    }

    public class TestQueryServices : Service
    {
        public ILogFactory Logger { get; set; }

        public IAutoQueryData AutoQuery { get; set; }

        public object Any(TestQueryRequest request)
        {
            try
            {
                var q = AutoQuery.CreateQuery(request, base.Request);
                var result = AutoQuery.Execute(request, q);

                return result;
            }
            catch (Exception ex)
            {
                var requestType = request?.GetType();

                var log = Logger?.GetLogger(requestType);
                if (log != null)
                    log.Error(ex);

                throw;
            }
        }
    }

}
