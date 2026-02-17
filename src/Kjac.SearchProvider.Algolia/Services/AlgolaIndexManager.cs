using Algolia.Search.Clients;
using Algolia.Search.Exceptions;
using Algolia.Search.Models.Search;
using Kjac.SearchProvider.Algolia.Constants;
using Kjac.SearchProvider.Algolia.Extensions;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Sync;
using CoreConstants = Umbraco.Cms.Search.Core.Constants;

namespace Kjac.SearchProvider.Algolia.Services;

internal sealed class AlgoliaIndexManager : AlgoliaIndexManagingServiceBase, IAlgoliaIndexManager
{
    private readonly IAlgoliaClientFactory _clientFactory;
    private readonly ILogger<AlgoliaIndexManager> _logger;

    public AlgoliaIndexManager(
        IServerRoleAccessor serverRoleAccessor,
        IAlgoliaClientFactory clientFactory,
        ILogger<AlgoliaIndexManager> logger)
        : base(serverRoleAccessor)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task EnsureAsync(string indexAlias)
    {
        if (ShouldNotManipulateIndexes())
        {
            return;
        }

        indexAlias = indexAlias.ValidIndexAlias();

        SearchClient client = _clientFactory.GetClient();
        var indexExists = await client.IndexExistsAsync(indexAlias);
        if (indexExists)
        {
            return;
        }

        _logger.LogInformation("Creating index {indexAlias}...", indexAlias);

        UpdatedAtResponse? response = await client.SetSettingsAsync(
            indexAlias,
            new()
            {
                // enable full text search for the "all texts" fields
                // NOTE: the order influences the applied relevance ranking
                SearchableAttributes =
                [
                    IndexConstants.FieldNames.AllTextsR1,
                    IndexConstants.FieldNames.AllTextsR2,
                    IndexConstants.FieldNames.AllTextsR3,
                    IndexConstants.FieldNames.AllTexts,
                ],
                // enable filtering for culture, segment and content type ID
                AttributesForFaceting =
                [
                    $"filterOnly({IndexConstants.FieldNames.Culture})",
                    $"filterOnly({IndexConstants.FieldNames.PathKeys})",
                    $"filterOnly({FieldName(CoreConstants.FieldNames.ContentTypeId, IndexConstants.FieldTypePostfix.Keywords)})"
                ]
            }
        );

        if (response is not null)
        {
            await client.WaitForTaskAsync(indexAlias, response.TaskID);
            _logger.LogInformation("Index {indexAlias} has been created.", indexAlias);
        }
        else
        {
            _logger.LogError(
                "Index {indexAlias} could not be created. There might be logs available in Algolia for troubleshooting",
                indexAlias
            );
        }
    }
}
