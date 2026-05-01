using Examine;
using Kjac.SearchProvider.Algolia.Extensions;
using Kjac.SearchProvider.Algolia.NotificationHandlers;
using Kjac.SearchProvider.Algolia.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Extensions;
using CoreConstants = Umbraco.Cms.Search.Core.Constants;
using IndexOptions = Umbraco.Cms.Search.Core.Configuration.IndexOptions;

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
                options.RegisterAlgoliaContentIndex<IDraftContentChangeStrategy>(
                    CoreConstants.IndexAliases.DraftContent,
                    UmbracoObjectTypes.Document
                );
                options.RegisterAlgoliaContentIndex<IPublishedContentChangeStrategy>(
                    CoreConstants.IndexAliases.PublishedContent,
                    UmbracoObjectTypes.Document
                );

                // register Algolia index for media
                options.RegisterAlgoliaContentIndex<IDraftContentChangeStrategy>(
                    CoreConstants.IndexAliases.DraftMedia,
                    UmbracoObjectTypes.Media
                );

                // register Algolia index for members
                options.RegisterAlgoliaContentIndex<IDraftContentChangeStrategy>(
                    CoreConstants.IndexAliases.DraftMembers,
                    UmbracoObjectTypes.Member
                );
            }
        );

        // ensure all indexes exist before Umbraco has finished start-up
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, EnsureIndexesNotificationHandler>();

        return builder;
    }

    public static IUmbracoBuilder DisableDefaultExamineIndexes(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ExamineManager>();
        builder.Services.AddUnique<IExamineManager, MaskedCoreIndexesExamineManager>();

        return builder;
    }
}
