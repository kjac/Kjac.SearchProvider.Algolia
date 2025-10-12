using Kjac.SearchProvider.Algolia.Configuration;
using Kjac.SearchProvider.Algolia.Services;
using Kjac.SearchProvider.Algolia.Services.Indexing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Search.Core.Services;

namespace Kjac.SearchProvider.Algolia.DependencyInjection;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAlgolia(this IServiceCollection services, IConfiguration configuration)
    {
        // register the Algolia searcher and indexer so they can be used explicitly for index registrations
        services.AddTransient<IAlgoliaIndexer, AlgoliaIndexer>();
        services.AddTransient<IAlgoliaSearcher, AlgoliaSearcher>();

        // register the Algolia searcher and indexer as the defaults
        services.AddTransient<IIndexer, AlgoliaIndexer>();
        services.AddTransient<ISearcher, AlgoliaSearcher>();

        // register supporting services
        services.AddSingleton<IAlgoliaClientFactory, AlgoliaClientFactory>();
        services.AddSingleton<IAlgoliaIndexManager, AlgoliaIndexManager>();
        services.AddSingleton<IIndexDocumentBuilder, IndexDocumentBuilder>();

        services.Configure<ClientOptions>(configuration.GetSection("AlgoliaSearchProvider:Client"));

        return services;
    }
}
