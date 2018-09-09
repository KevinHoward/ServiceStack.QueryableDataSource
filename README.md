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
* [Microsoft Azure CosmosDb](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.linq.documentqueryable.asdocumentquery?view=azure-dotnet)
* [MongoDb](https://mongodb.github.io/mongo-csharp-driver/2.4/apidocs/html/M_MongoDB_Driver_Linq_LinqExtensionMethods_AsQueryable__1.htm)
* [RavenDb](https://ravendb.net/docs/article-page/4.1/csharp/indexes/querying/query-vs-document-queryhttps://ravendb.net/learn/docs-guide)   
    * **NOTE:** *DocumentQuery doesn't implement IQueryable, but Query does and it's translated into a DocumentQuery.* 


# How it works

The secret sauce to making this work is the use of the [System.Linq.Dynamic.Core](https://github.com/StefH/System.Linq.Dynamic.Core) library which dynamic transforms of string based queries to LINQ expressions. This allows the library to construct the query without knowing type details. 

# Using ServiceStack.Azure.CosmosDb

Install from NuGet:
```
Install-Package ServiceStack.Azure.CosmosDb
```

Simply add the `AutoQueryFeature` in your `AppHost.Configure()` method:

```
public override void Configure(Container container)
{
    // Add ValidationFeature and AuthFeature plugins first

    Plugins.Add(new WebhookFeature());
}
```
