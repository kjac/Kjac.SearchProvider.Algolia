using Kjac.SearchProvider.Algolia.NotificationHandlers;
using Kjac.SearchProvider.Algolia.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using CoreConstants = Umbraco.Cms.Search.Core.Constants;

namespace Kjac.SearchProvider.Algolia.DependencyInjection;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddAlgoliaSearchProvider(this IUmbracoBuilder builder)
    {
        builder.Services.AddAlgolia(builder.Config);

        builder.Services.Configure<IndexOptions>(
            options =>
            {
                // register Algolia indexes for draft and published content
                options.RegisterIndex<IAlgoliaIndexer, IAlgoliaSearcher, IDraftContentChangeStrategy>(
                    CoreConstants.IndexAliases.DraftContent,
                    UmbracoObjectTypes.Document
                );
                options.RegisterIndex<IAlgoliaIndexer, IAlgoliaSearcher, IPublishedContentChangeStrategy>(
                    CoreConstants.IndexAliases.PublishedContent,
                    UmbracoObjectTypes.Document
                );

                // register Algolia index for media
                options.RegisterIndex<IAlgoliaIndexer, IAlgoliaSearcher, IDraftContentChangeStrategy>(
                    CoreConstants.IndexAliases.DraftMedia,
                    UmbracoObjectTypes.Media
                );

                // register Algolia index for members
                options.RegisterIndex<IAlgoliaIndexer, IAlgoliaSearcher, IDraftContentChangeStrategy>(
                    CoreConstants.IndexAliases.DraftMembers,
                    UmbracoObjectTypes.Member
                );
            }
        );

        // ensure all indexes exist before Umbraco has finished start-up
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, EnsureIndexesNotificationHandler>();

        return builder;
    }
}
