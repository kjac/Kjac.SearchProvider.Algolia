using Kjac.SearchProvider.Algolia.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Kjac.SearchProvider.Algolia.Extensions;

public static class IndexOptionsExtensions
{
    public static IndexOptions RegisterAlgoliaContentIndex<TContentChangeStrategy>(
        this IndexOptions indexOptions,
        string indexAlias,
        params UmbracoObjectTypes[] containedObjectTypes)
        where TContentChangeStrategy : class, IContentChangeStrategy
    {
        indexOptions.RegisterContentIndex<IAlgoliaIndexer, IAlgoliaSearcher, TContentChangeStrategy>(
            indexAlias,
            sameOriginOnly: true,
            containedObjectTypes
        );
        return indexOptions;
    }
}
