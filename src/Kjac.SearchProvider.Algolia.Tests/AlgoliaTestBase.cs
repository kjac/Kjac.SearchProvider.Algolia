using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Kjac.SearchProvider.Algolia.DependencyInjection;
using Kjac.SearchProvider.Algolia.Extensions;
using Kjac.SearchProvider.Algolia.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;

namespace Kjac.SearchProvider.Algolia.Tests;

[TestFixture]
public abstract class AlgoliaTestBase
{
    private ServiceProvider _serviceProvider;

    protected abstract string IndexAlias { get; }

    [OneTimeSetUp]
    public void SetUp()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddAlgolia(configuration)
            .AddLogging();

        serviceCollection.AddSingleton<IServerRoleAccessor, SingleServerRoleAccessor>();

        PerformAdditionalConfiguration(serviceCollection);

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }

    protected virtual void PerformAdditionalConfiguration(ServiceCollection serviceCollection)
    {
    }

    protected T GetRequiredService<T>() where T : notnull
        => _serviceProvider.GetRequiredService<T>();

    protected async Task DeleteIndex(string indexAlias)
    {
        SearchClient client = GetRequiredService<IAlgoliaClientFactory>().GetClient();

        var validIndexAlias = indexAlias.ValidIndexAlias();
        var indexExists = await client.IndexExistsAsync(validIndexAlias);
        if (indexExists is false)
        {
            return;
        }

        DeletedAtResponse? response = await client.DeleteIndexAsync(validIndexAlias);
        if (response is null)
        {
            return;
        }

        await client.WaitForTaskAsync(validIndexAlias, response.TaskID);
    }

    protected async Task<SearchResult> SearchAsync(
        string? query = null,
        IEnumerable<Filter>? filters = null,
        IEnumerable<Facet>? facets = null,
        IEnumerable<Sorter>? sorters = null,
        string? culture = null,
        string? segment = null,
        AccessContext? accessContext = null,
        int skip = 0,
        int take = 100)
    {
        IAlgoliaSearcher searcher = GetRequiredService<IAlgoliaSearcher>();
        SearchResult result = await searcher.SearchAsync(
            IndexAlias,
            query,
            filters,
            facets,
            sorters,
            culture,
            segment,
            accessContext,
            skip,
            take
        );

        Assert.That(result, Is.Not.Null);
        return result;
    }

    protected async Task EnsureIndex(IEnumerable<string> additionalAttributesForFaceting)
    {
        await DeleteIndex(IndexAlias);

        await GetRequiredService<IAlgoliaIndexManager>().EnsureAsync(IndexAlias);

        // configure the index to support the various test cases (filtering and faceting)
        var validIndexAlias = IndexAlias.ValidIndexAlias();
        SearchClient client = GetRequiredService<IAlgoliaClientFactory>().GetClient();
        SettingsResponse? settings = await client.GetSettingsAsync(validIndexAlias);
        if (settings is null)
        {
            throw new ApplicationException($"Could not fetch settings from Algolia index with alias: {validIndexAlias}");
        }

        List<string> attributesForFaceting = settings.AttributesForFaceting
                                             ?? throw new ApplicationException($"No facet-able fields found in Algolia index with alias: {validIndexAlias}");

        attributesForFaceting.AddRange(additionalAttributesForFaceting);

        UpdatedAtResponse? updatedAtResponse = await client.SetSettingsAsync(validIndexAlias, new IndexSettings
        {
            SearchableAttributes = settings.SearchableAttributes,
            AttributesForFaceting = attributesForFaceting,
            // must disable typo tolerance to get deterministic search results
            TypoTolerance = new TypoTolerance(TypoToleranceEnum.False),
            MaxValuesPerFacet = 500
        });

        if (updatedAtResponse is null)
        {
            throw new ApplicationException($"Could not initiate settings update for Algolia index with alias: {validIndexAlias}");
        }

        await client.WaitForTaskAsync(validIndexAlias, updatedAtResponse.TaskID);
    }
}
