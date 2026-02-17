using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Kjac.SearchProvider.Algolia.Constants;
using Kjac.SearchProvider.Algolia.Extensions;
using Kjac.SearchProvider.Algolia.Models;
using Kjac.SearchProvider.Algolia.Services.Indexing;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;
using Action = Algolia.Search.Models.Search.Action;
using CoreConstants = Umbraco.Cms.Search.Core.Constants;

namespace Kjac.SearchProvider.Algolia.Services;

internal sealed class AlgoliaIndexer : AlgoliaIndexManagingServiceBase, IAlgoliaIndexer
{
    private readonly IAlgoliaClientFactory _clientFactory;
    private readonly IIndexDocumentBuilder _indexDocumentBuilder;
    private readonly IAppPolicyCache _appCache;
    private readonly ILogger<AlgoliaIndexer> _logger;

    private readonly Lock _locker = new();
    private readonly string _indicesCacheKey = $"{nameof(AlgoliaIndexer)}:{nameof(ListIndicesResponse)}";

    public AlgoliaIndexer(
        IServerRoleAccessor serverRoleAccessor,
        IAlgoliaClientFactory clientFactory,
        IIndexDocumentBuilder indexDocumentBuilder,
        AppCaches appCaches,
        ILogger<AlgoliaIndexer> logger)
        : base(serverRoleAccessor)
    {
        _clientFactory = clientFactory;
        _indexDocumentBuilder = indexDocumentBuilder;
        _logger = logger;
        _appCache = appCaches.RuntimeCache;
    }

    public async Task AddOrUpdateAsync(
        string indexAlias,
        Guid id,
        UmbracoObjectTypes objectType,
        IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields,
        ContentProtection? protection)
    {
        if (ShouldNotManipulateIndexes())
        {
            return;
        }

        IEnumerable<IndexField> fieldsAsArray = fields as IndexField[] ?? fields.ToArray();

        var pathIds = fieldsAsArray
                          .FirstOrDefault(field => field.FieldName == CoreConstants.FieldNames.PathIds)?
                          .Value
                          .Keywords?
                          .ToArray()
                      ?? [];
        if (pathIds.Length is 0)
        {
            _logger.LogWarning("Could not index document - no path IDs found for ID: {indexAlias}", id);
            return;
        }

        IndexDocumentBase[] documents = _indexDocumentBuilder
            .Build(id, objectType, variations, fieldsAsArray, protection)
            .ToArray();

        if (documents.Length is 0)
        {
            return;
        }

        foreach (IndexDocumentBase document in documents)
        {
            document.PathIds =  pathIds;
        }

        SearchClient client = _clientFactory.GetClient();
        var validIndexAlias = indexAlias.ValidIndexAlias();

        try
        {
            await client.ChunkedBatchAsync(
                validIndexAlias,
                documents,
                Action.UpdateObject
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to add/update documents in Algolia index: {indexAlias}.", validIndexAlias);
        }

        _appCache.ClearByKey(_indicesCacheKey);
    }

    public async Task DeleteAsync(string indexAlias, IEnumerable<Guid> ids)
    {
        if (ShouldNotManipulateIndexes())
        {
            return;
        }

        SearchClient client = _clientFactory.GetClient();

        var validIndexAlias = indexAlias.ValidIndexAlias();
        try
        {
            var deleteByParams = new DeleteByParams
            {
                FacetFilters = new FacetFilters(
                    [
                        new FacetFilters(
                            ids.Select(id =>
                                    new FacetFilters($"{IndexConstants.FieldNames.PathKeys}:{id.AsKeyword()}")
                                )
                                .ToList()
                        )
                    ]
                )
            };
            await client.DeleteByAsync(validIndexAlias, deleteByParams);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to delete documents from Algolia index: {indexAlias}.", validIndexAlias);
        }

        _appCache.ClearByKey(_indicesCacheKey);
    }

    public async Task ResetAsync(string indexAlias)
    {
        if (ShouldNotManipulateIndexes())
        {
            return;
        }

        SearchClient client = _clientFactory.GetClient();

        var validIndexAlias = indexAlias.ValidIndexAlias();
        var indexExists = await client.IndexExistsAsync(validIndexAlias);
        if (indexExists is false)
        {
            return;
        }

        try
        {
            await client.ClearObjectsAsync(validIndexAlias);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to clear all documents from Algolia index: {indexAlias}.", validIndexAlias);
        }

        _appCache.ClearByKey(_indicesCacheKey);
    }

    public Task<IndexMetadata> GetMetadataAsync(string indexAlias)
    {
        if (_locker.TryEnter(TimeSpan.FromSeconds(10)) is false)
        {
            return Task.FromResult(new IndexMetadata(0, HealthStatus.Unknown));
        }

        ListIndicesResponse? indexes = _appCache.GetCacheItem<ListIndicesResponse?>(
            _indicesCacheKey,
            () =>
            {
                SearchClient client = _clientFactory.GetClient();
                return client.ListIndices();
            },
            TimeSpan.FromMinutes(5)
        );

        _locker.Exit();

        var validIndexAlias = indexAlias.ValidIndexAlias();
        FetchedIndex? index = indexes?.Items.FirstOrDefault(i => i.Name == validIndexAlias);
        if (index is null)
        {
            return Task.FromResult(new IndexMetadata(0, HealthStatus.Unknown));
        }

        HealthStatus healthStatus = index.PendingTask
            ? HealthStatus.Rebuilding
            : index.Entries == 0
            ? HealthStatus.Empty
            :  HealthStatus.Healthy;

        return Task.FromResult(new IndexMetadata(index.Entries, healthStatus));
    }
}
