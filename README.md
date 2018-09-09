[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0) [![Build status](https://ci.appveyor.com/api/projects/status/j2a8skqibee6d7vt/branch/master?svg=true)](https://ci.appveyor.com/project/JezzSantos/servicestack-iqueryable-datasource/branch/master)


[![NuGet](https://img.shields.io/nuget/v/ServiceStack.Webhooks.svg?label=ServiceStack.IQueryableDataSource)](https://www.nuget.org/packages/ServiceStack.Webhooks) 

# ServiceStack.QueryableDataSource
An IQueryable DataSource library for the ServiceStack AutoQuery feature

[![Release Notes](https://img.shields.io/nuget/v/ServiceStack.IQueryableDataSource.svg?label=Release%20Notes&colorB=green)](https://github.com/KevinHoward/ServiceStack.IQueryableDataSource/wiki/Release-Notes)

# Overview

This project makes it possible to expose data with the [ServiceStack AutoQuery](http://docs.servicestack.net/autoquery) from providers that leverage the [IQueryable<T>](https://docs.microsoft.com/en-us/dotnet/api/system.linq.iqueryable-1?view=netframework-4.7.2) interface.

There are several data providers that implement the IQueryable<T> interface for querying. The most popular is Microsoft's Entity Framework. However, ServiceStack's SQL provider can already handle this. So this library will initially focus on Document Database providers (i.e. *No-SQL*).

Data providers that include:

* [Microsoft Entity Framework](https://msdn.microsoft.com/en-us/library/system.data.entity.queryableextensions(v=vs.113).aspx)
* [Microsoft Azure CosmosDB](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.linq.documentqueryable.asdocumentquery?view=azure-dotnet)
* [MongoDB](https://mongodb.github.io/mongo-csharp-driver/2.4/apidocs/html/M_MongoDB_Driver_Linq_LinqExtensionMethods_AsQueryable__1.htm)
* [RavenDB](https://ravendb.net/docs/article-page/4.1/csharp/indexes/querying/query-vs-document-queryhttps://ravendb.net/learn/docs-guide)   
    * **NOTE:** *DocumentQuery doesn't implement IQueryable, but Query does translate into a DocumentQuery.* 


# How it works

The secret sauce in making this work is the use of the [System.Linq.Dynamic.Core](https://github.com/StefH/System.Linq.Dynamic.Core) library which dynamic transforms string based queries to LINQ expressions. This allows the library to construct the query without knowing type details. 

# Using ServiceStack.Azure.CosmosDB

Install from NuGet:
```
Install-Package ServiceStack.Azure.CosmosDB
```

Simply add the `AutoQueryDataFeature` in your `AppHost.Configure()` method:
```
public override void Configure(Container container)
{
    // Get Data Provider Settings from Configuration 
    var endpointUrl = AppSettings.Get<string>("CosmosDb.EndPointUrl");
    var authorizationKey = AppSettings.Get<string>("CosmosDb.AuthorizationKey");
    var databaseId = AppSettings.Get<string>("CosmosDb.DatabaseId");
    var collectionId = AppSettings.Get<string>("CosmosDb.CollectionId");

    // Create a Document Client 
    var docClient = new DocumentClient(
        new Uri(endpointUrl),
        authorizationKey);

    // Register the Document Client into the IOC
    container.Register<IDocumentClient>(c => docClient);

    var requestOptions = new RequestOptions { ConsistencyLevel = ConsistencyLevel.Session };

    // Add AuthQueryDataFeature plugin with a DataSource 
    // you will need to add a data source for each TDocument type
    Plugins.Add(new AutoQueryDataFeatureFeature()
        .AddDataSource(ctx => ctx.CosmosDBDataSource<TDocument>(docClient, databaseId, collectionId, requestOptions)));
}
```

# Using ServiceStack.MongoDB

Install from NuGet:
```
Install-Package ServiceStack.MongoDB
```

Simply add the `AutoQueryDataFeature` in your `AppHost.Configure()` method:
```
public override void Configure(Container container)
{
    // Get Data Provider Settings from Configuration 
    var connectionString = AppSettings.Get<string>("MongoDB.ConnectionString");
    var databaseId = AppSettings.Get<string>("MongoDB.DatabaseId");
    var collectionId = AppSettings.Get<string>("MongoDB.CollectionId");
            
    // Create a Mongo Client 
    var mongoClient = new MongoClient(
        new MongoUrl(connectionString));

    // Register the Document Client into the IOC
    container.Register<IMongoClient>(c => mongoClient);

    // Add AuthQueryDataFeature plugin with a DataSource
    Plugins.Add(new AutoQueryDataFeatureFeature()
        .AddDataSource(ctx => ctx.MongoDBDataSource<TDocument>(mongoClient, databaseId, collectionId)));
}
```
