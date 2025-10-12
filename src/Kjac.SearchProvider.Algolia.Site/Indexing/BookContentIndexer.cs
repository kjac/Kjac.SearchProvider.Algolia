using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Kjac.SearchProvider.Algolia.Site.Indexing;

public class BookContentIndexer : IContentIndexer
{
    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(
        IContentBase content,
        string?[] cultures,
        bool published,
        CancellationToken cancellationToken)
    {
        // this content indexer only applies to books
        if (content.ContentType.Alias is not "book")
        {
            return Task.FromResult<IEnumerable<IndexField>>([]);
        }

        // since Algolia does not support range facets, we need to calculate the ranges up front,
        // and store them as a regular keyword value instead (because keyword facets work).
        var publishYear = content.GetValue<int>("publishYear");
        var publishYearRange = publishYear switch
        {
            >= 1500 and < 1600 => "16th Century",
            >= 1600 and < 1700 => "17th Century",
            >= 1700 and < 1800 => "18th Century",
            >= 1800 and < 1900 => "19th Century",
            >= 1900 and < 2000 => "20th Century",
            >= 2000 and < 2100 => "21st Century",
            _ => null
        };

        return Task.FromResult<IEnumerable<IndexField>>(
            publishYearRange is not null
                ? [new IndexField("publishYearRange", new IndexValue { Keywords = [publishYearRange] }, null, null)]
                : []
        );
    }
}
