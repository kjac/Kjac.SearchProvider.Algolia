using Kjac.SearchProvider.Algolia.Models;
using Kjac.SearchProvider.Algolia.Services.Indexing;
using Kjac.SearchProvider.Algolia.Site.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using CoreConstants = Umbraco.Cms.Search.Core.Constants;

namespace Kjac.SearchProvider.Algolia.Site.Services.Indexing;

public class BookIndexDocumentBuilder : IIndexDocumentBuilder
{
    private const string BookContentTypeId = "3acd95a1-b9bd-4392-be67-0281dbbe125f";

    public IEnumerable<IndexDocumentBase> Build(
        Guid id,
        UmbracoObjectTypes objectType,
        IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields,
        ContentProtection? protection)
    {
        IndexField[] fieldsAsArray = fields as IndexField[] ?? fields.ToArray();

        // only index content of type "book"
        var contentTypeId = GetIndexValue(fieldsAsArray, CoreConstants.FieldNames.ContentTypeId)?
            .Keywords?.FirstOrDefault();

        return contentTypeId is BookContentTypeId
            ?
            [
                new BookIndexDocument
                {
                    Id = id.AsKeyword(),
                    Title = GetIndexValue(fieldsAsArray, CoreConstants.FieldNames.Name)?.TextsR1?.FirstOrDefault(),
                    Summary = GetIndexValue(fieldsAsArray, "summary")?.Texts?.FirstOrDefault(),
                    Author = GetIndexValue(fieldsAsArray, "author")?.Texts?.FirstOrDefault(),
                    AuthorNationality = GetIndexValue(fieldsAsArray, "authorNationality")?.Keywords?.ToArray(),
                    PublishYear = GetIndexValue(fieldsAsArray, "publishYear")?.Integers?.FirstOrDefault(),
                    PublishYearRange = GetIndexValue(fieldsAsArray, "publishYearRange")?.Keywords?.FirstOrDefault(),
                    Length = GetIndexValue(fieldsAsArray, "length")?.Keywords?.FirstOrDefault()
                }
            ]
            : [];
    }

    // NOTE: this is only works because the content model happens to be invariant for this site.
    //       variant content models might contain multiple IndexField entries for the same field name.
    private IndexValue? GetIndexValue(IndexField[] fields, string fieldName)
        => fields.FirstOrDefault(field => field.FieldName.InvariantEquals(fieldName))?.Value;
}
