using Algolia.Search.Clients;
using Kjac.SearchProvider.Algolia.Constants;
using Kjac.SearchProvider.Algolia.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Models.Searching.Sorting;

namespace Kjac.SearchProvider.Algolia.Tests;

[Ignore("These tests might be expensive to run. Also might be volatile due to timing issues. Use with caution.")]
public class AlgoliaIndexerTests : AlgoliaTestBase
{
    protected override string IndexAlias => nameof(AlgoliaIndexerTests);

    [Test]
    public async Task CanCreateAndResetIndex()
    {
        Dictionary<string, Guid> ids = await CreateIndexStructure();

        SearchResult result = await SearchAsync(
            filters: [
                new KeywordFilter(
                    Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                    [ids.First().Value.AsKeyword()],
                    false
                )
            ]
        );
        Assert.That(result.Total, Is.Not.Zero);

        SearchClient client = GetRequiredService<IAlgoliaClientFactory>().GetClient();

        var exists = await client.IndexExistsAsync(IndexAlias, CancellationToken.None);
        Assert.That(exists, Is.True);

        await Indexer.ResetAsync(IndexAlias);

        WaitForIndexOperationsToComplete();

        exists = await client.IndexExistsAsync(IndexAlias, CancellationToken.None);
        Assert.That(exists, Is.True);

        result = await SearchAsync(
            filters: [
                new KeywordFilter(
                    Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                    [ids.First().Value.AsKeyword()],
                    false
                )
            ]
        );
        Assert.That(result.Total, Is.Zero);
    }

    [Test]
    public async Task CanDeleteRootDocuments()
    {
        Dictionary<string, Guid> ids = await CreateIndexStructure();

        await Indexer.DeleteAsync(IndexAlias, [ids["0:root"], ids["2:root"]]);

        WaitForIndexOperationsToComplete();

        for (var i = 0; i < 3; i++)
        {
            SearchResult result = await SearchAsync(
                filters: [
                    new KeywordFilter(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        [ids[$"{i}:root"].AsKeyword()],
                        false
                    )
                ]
            );

            Assert.That(result.Total, Is.EqualTo(i == 1 ? 3 : 0));
        }
    }

    [Test]
    public async Task CanDeleteDescendantDocuments()
    {
        Dictionary<string, Guid> ids = await CreateIndexStructure();

        await Indexer.DeleteAsync(IndexAlias, [ids["1:child"], ids["2:grandchild"]]);

        WaitForIndexOperationsToComplete();

        for (var i = 0; i < 3; i++)
        {
            SearchResult result = await SearchAsync(
                filters: [
                    new KeywordFilter(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        [ids[$"{i}:root"].AsKeyword()],
                        false
                    )
                ]
            );

            Assert.That(
                result.Total,
                Is.EqualTo(
                    i switch
                    {
                        0 => 3, // all documents should still be there
                        1 => 1, // child and grandchild should be deleted
                        2 => 2  // grandchild should be deleted
                    }
                )
            );
        }
    }

    [Test]
    public async Task CanGetIndexMetadata()
    {
        Dictionary<string, Guid> ids = await CreateIndexStructure();

        IndexMetadata metadata = await Indexer.GetMetadataAsync(IndexAlias);
        Assert.Multiple(() =>
        {
            Assert.That(metadata.DocumentCount, Is.EqualTo(ids.Count));
            Assert.That(metadata.HealthStatus, Is.EqualTo(HealthStatus.Healthy));
        });

        await Indexer.ResetAsync(IndexAlias);

        WaitForIndexOperationsToComplete();

        metadata = await Indexer.GetMetadataAsync(IndexAlias);
        Assert.Multiple(() =>
        {
            Assert.That(metadata.DocumentCount, Is.Zero);
            Assert.That(metadata.HealthStatus, Is.EqualTo(HealthStatus.Empty));
        });
    }

    private async Task<Dictionary<string, Guid>> CreateIndexStructure()
    {
        await EnsureIndex([$"{IndexConstants.FieldNames.Fields}.{Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds}{IndexConstants.FieldTypePostfix.Keywords}"]);

        var ids = new Dictionary<string, Guid>();
        for (var i = 0; i < 3; i++)
        {
            var rootId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var grandchildId = Guid.NewGuid();
            ids.Add($"{i}:root", rootId);
            ids.Add($"{i}:child", childId);
            ids.Add($"{i}:grandchild", grandchildId);

            await Indexer.AddOrUpdateAsync(
                IndexAlias,
                rootId,
                UmbracoObjectTypes.Unknown,
                [new Variation(Culture: null, Segment: null)],
                [
                    new IndexField(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        new IndexValue { Keywords = [rootId.AsKeyword()] },
                        Culture: null,
                        Segment: null
                    )
                ],
                null
            );

            await Indexer.AddOrUpdateAsync(
                IndexAlias,
                childId,
                UmbracoObjectTypes.Unknown,
                [new Variation(Culture: null, Segment: null)],
                [
                    new IndexField(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        new IndexValue { Keywords = [rootId.AsKeyword(), childId.AsKeyword()] },
                        Culture: null,
                        Segment: null
                    )
                ],
                null
            );

            await Indexer.AddOrUpdateAsync(
                IndexAlias,
                grandchildId,
                UmbracoObjectTypes.Unknown,
                [new Variation(Culture: null, Segment: null)],
                [
                    new IndexField(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        new IndexValue { Keywords = [rootId.AsKeyword(), childId.AsKeyword(), grandchildId.AsKeyword()] },
                        Culture: null,
                        Segment: null
                    )
                ],
                null
            );
        }

        WaitForIndexOperationsToComplete();

        for (var i = 0; i < 3; i++)
        {
            SearchResult result = await SearchAsync(
                filters: [
                    new KeywordFilter(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        [ids[$"{i}:root"].AsKeyword()],
                        false
                    )
                ]
            );

            Assert.That(result.Total, Is.EqualTo(3));
        }

        return ids;
    }

    private IAlgoliaIndexManager IndexManager => GetRequiredService<IAlgoliaIndexManager>();

    private IAlgoliaIndexer Indexer => GetRequiredService<IAlgoliaIndexer>();

    private IAlgoliaSearcher Searcher => GetRequiredService<IAlgoliaSearcher>();

    private async Task<SearchResult> SearchAsync(
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
        SearchResult result = await Searcher.SearchAsync(
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

    private void WaitForIndexOperationsToComplete()
        => Thread.Sleep(2000);
}
