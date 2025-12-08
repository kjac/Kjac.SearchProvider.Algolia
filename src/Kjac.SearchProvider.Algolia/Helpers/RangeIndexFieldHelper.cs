using Umbraco.Cms.Search.Core.Models.Indexing;

namespace Kjac.SearchProvider.Algolia.Helpers;

public static class RangeIndexFieldHelper
{
    public static IndexField Create(string fieldName, string rangeValue, string? culture = null, string? segment = null)
        => Create(fieldName, [rangeValue], culture, segment);

    public static IndexField Create(string fieldName, string[] rangeValues, string? culture = null, string? segment = null)
        => new (FieldName(fieldName), new IndexValue { Keywords = rangeValues }, culture, segment);

    internal static string FieldName(string fieldName)
        => $"__range_{fieldName}";
}
