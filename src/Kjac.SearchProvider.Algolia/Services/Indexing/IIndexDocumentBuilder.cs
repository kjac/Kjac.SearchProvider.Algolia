using Kjac.SearchProvider.Algolia.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Kjac.SearchProvider.Algolia.Services.Indexing;

public interface IIndexDocumentBuilder
{
    public IEnumerable<IndexDocumentBase> Build(
        Guid id,
        UmbracoObjectTypes objectType,
        IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields,
        ContentProtection? protection);
}
