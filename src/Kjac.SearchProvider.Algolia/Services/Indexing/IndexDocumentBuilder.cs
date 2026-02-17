using Kjac.SearchProvider.Algolia.Constants;
using Kjac.SearchProvider.Algolia.Extensions;
using Kjac.SearchProvider.Algolia.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Extensions;

namespace Kjac.SearchProvider.Algolia.Services.Indexing;

public class IndexDocumentBuilder : IIndexDocumentBuilder
{
    public IEnumerable<IndexDocumentBase> Build(
        Guid id,
        UmbracoObjectTypes objectType,
        IEnumerable<Variation> variations,
        IEnumerable<IndexField> fields,
        ContentProtection? protection)
    {
        IEnumerable<IGrouping<string, IndexField>> fieldsByFieldName = fields.GroupBy(field => field.FieldName);
        return variations
            .Where(variation => variation.Segment.IsNullOrWhiteSpace())
            .Select(variation =>
                {
                    // document variation
                    var culture = variation.Culture.IndexCulture();

                    // document access (no access maps to an empty key for querying)
                    Guid[] accessKeys = protection?.AccessIds.Any() is true
                        ? protection.AccessIds.ToArray()
                        : [Guid.Empty];

                    // relevant field values for this variation (including invariant fields)
                    IndexField[] variationFields = fieldsByFieldName.Select(g =>
                            {
                                IndexField[] applicableFields = g.Where(f =>
                                    (variation.Culture is not null
                                     && variation.Segment is not null
                                     && f.Culture == variation.Culture
                                     && f.Segment == variation.Segment)
                                    || (variation.Culture is not null
                                        && f.Culture == variation.Culture
                                        && f.Segment is null)
                                    || (variation.Segment is not null
                                        && f.Culture is null
                                        && f.Segment == variation.Segment)
                                    || (f.Culture is null && f.Segment is null)
                                ).ToArray();

                                return applicableFields.Any()
                                    ? new IndexField(
                                        g.Key,
                                        new IndexValue
                                        {
                                            DateTimeOffsets = applicableFields.SelectMany(f => f.Value.DateTimeOffsets ?? []).NullIfEmpty(),
                                            Decimals = applicableFields.SelectMany(f => f.Value.Decimals ?? []).NullIfEmpty(),
                                            Integers = applicableFields.SelectMany(f => f.Value.Integers ?? []).NullIfEmpty(),
                                            Keywords = applicableFields.SelectMany(f => f.Value.Keywords ?? []).NullIfEmpty(),
                                            Texts = applicableFields.SelectMany(f => f.Value.Texts ?? []).NullIfEmpty(),
                                            TextsR1 = applicableFields.SelectMany(f => f.Value.TextsR1 ?? []).NullIfEmpty(),
                                            TextsR2 = applicableFields.SelectMany(f => f.Value.TextsR2 ?? []).NullIfEmpty(),
                                            TextsR3 = applicableFields.SelectMany(f => f.Value.TextsR3 ?? []).NullIfEmpty(),
                                        },
                                        variation.Culture,
                                        variation.Segment
                                    )
                                    : null;
                            }
                        )
                        .WhereNotNull()
                        .ToArray();

                    // all text fields for "free text query on all fields"
                    var allTexts = variationFields
                        .SelectMany(field => field.Value.Texts ?? [])
                        .ToArray();
                    var allTextsR1 = variationFields
                        .SelectMany(field => field.Value.TextsR1 ?? [])
                        .ToArray();
                    var allTextsR2 = variationFields
                        .SelectMany(field => field.Value.TextsR2 ?? [])
                        .ToArray();
                    var allTextsR3 = variationFields
                        .SelectMany(field => field.Value.TextsR3 ?? [])
                        .ToArray();

                    // explicit document field values
                    var fieldValues = variationFields
                        .SelectMany(field =>
                            {
                                // Algolia is very much in control of relevance scoring and sorting, so it adds little
                                // value to have separate textual relevance fields at field level.
                                // The "all texts" collections still retain the textual relevance division, but those
                                // are also meant to be used in a broader sense.
                                IEnumerable<object>? texts = (field.Value.TextsR1?.OfType<object>().ToArray() ?? [])
                                    .Union(field.Value.TextsR2?.OfType<object>().ToArray() ?? [])
                                    .Union(field.Value.TextsR3?.OfType<object>().ToArray() ?? [])
                                    .Union(field.Value.Texts?.OfType<object>().ToArray() ?? [])
                                    .NullIfEmpty();

                                return new (string FieldName, string Postfix, object[]? Values)[]
                                {
                                    (
                                        field.FieldName,
                                        IndexConstants.FieldTypePostfix.Texts,
                                        texts?.ToArray()
                                    ),
                                    (
                                        field.FieldName,
                                        IndexConstants.FieldTypePostfix.Integers,
                                        field.Value.Integers?.OfType<object>().ToArray()
                                    ),
                                    (
                                        field.FieldName,
                                        IndexConstants.FieldTypePostfix.Decimals,
                                        field.Value.Decimals?.OfType<object>().ToArray()
                                    ),
                                    (
                                        field.FieldName,
                                        IndexConstants.FieldTypePostfix.DateTimeOffsets,
                                        // Algolia expects unix timestamps for dates
                                        field.Value.DateTimeOffsets?
                                            .Select(dt => dt.ToUnixTimeSeconds()).OfType<object>()
                                            .ToArray()
                                    ),
                                    (
                                        field.FieldName,
                                        IndexConstants.FieldTypePostfix.Keywords,
                                        field.Value.Keywords?.OfType<object>().ToArray()
                                    )
                                };
                            }
                        )
                        .Where(f => f.Values?.Any() is true)
                        .ToDictionary(f => $"{f.FieldName}{f.Postfix}", f => f.Values!);

                    return new IndexDocument
                    {
                        Id = $"{id:D}.{culture}",
                        ObjectType = objectType.ToString(),
                        Key = id,
                        Culture = culture,
                        AccessKeys = accessKeys,
                        AllTexts = allTexts,
                        AllTextsR1 = allTextsR1,
                        AllTextsR2 = allTextsR2,
                        AllTextsR3 = allTextsR3,
                        Fields = fieldValues
                    };
                }
            );
    }
}
