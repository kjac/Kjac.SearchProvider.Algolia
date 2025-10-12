using Kjac.SearchProvider.Algolia.Configuration;
using Kjac.SearchProvider.Algolia.DependencyInjection;
using Kjac.SearchProvider.Algolia.Site.Indexing;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Kjac.SearchProvider.Algolia.Site.DependencyInjection;

public class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder
            // add core services for search abstractions
            .AddSearchCore()
            // use the Algolia search provider
            .AddAlgoliaSearchProvider()
            // force rebuild indexes after startup
            .RebuildIndexesAfterStartup();

        // configure System.Text.Json to allow serializing output models
        builder.ConfigureJsonOptions();

        // register the content indexer to enrich book documents
        builder.Services.AddTransient<IContentIndexer, BookContentIndexer>();

        builder.Services.Configure<ClientOptions>(options =>
            {
                options.ApiKey = "[your API key]";
                options.AppId = "[your app ID]";
            }
        );
    }
}
