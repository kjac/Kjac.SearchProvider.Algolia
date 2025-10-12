using Kjac.SearchProvider.Algolia.Constants;

namespace Kjac.SearchProvider.Algolia.Services;

internal abstract class AlgoliaServiceBase
{
    protected static string FieldName(string fieldName, string postfix)
        => $"{IndexConstants.FieldNames.Fields}.{fieldName}{postfix}";
}
