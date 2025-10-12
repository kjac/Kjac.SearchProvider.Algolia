using Kjac.SearchProvider.Algolia.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Models.Configuration;
using Umbraco.Cms.Search.Core.Services;

namespace Kjac.SearchProvider.Algolia.NotificationHandlers;

internal sealed class EnsureIndexesNotificationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    private readonly IAlgoliaIndexManager _indexManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IndexOptions _indexOptions;

    public EnsureIndexesNotificationHandler(
        IAlgoliaIndexManager indexManager,
        IServiceProvider serviceProvider,
        IOptions<IndexOptions> indexOptions)
    {
        _indexManager = indexManager;
        _serviceProvider = serviceProvider;
        _indexOptions = indexOptions.Value;
    }

    public async Task HandleAsync(
        UmbracoApplicationStartingNotification notification,
        CancellationToken cancellationToken)
    {
        Type implicitIndexServiceType = typeof(IIndexer);
        Type defaultIndexServiceType = _serviceProvider.GetRequiredService<IIndexer>().GetType();
        Type elasticIndexServiceType = typeof(IAlgoliaIndexer);

        foreach (IndexRegistration indexRegistration in _indexOptions.GetIndexRegistrations())
        {
            var shouldEnsureIndex = indexRegistration.Indexer == elasticIndexServiceType
                                    || (indexRegistration.Indexer == implicitIndexServiceType &&
                                        defaultIndexServiceType == elasticIndexServiceType);

            if (shouldEnsureIndex)
            {
                await _indexManager.EnsureAsync(indexRegistration.IndexAlias);
            }
        }
    }
}
