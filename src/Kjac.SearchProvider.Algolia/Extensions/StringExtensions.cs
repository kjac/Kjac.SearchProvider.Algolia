using Kjac.SearchProvider.Algolia.Constants;

namespace Kjac.SearchProvider.Algolia.Extensions;

internal static class StringExtensions
{
    public static string IndexCulture(this string? culture)
        => culture?.ToLowerInvariant() ?? IndexConstants.Variation.InvariantCulture;

    public static string IndexSegment(this string? segment)
        => segment?.ToLowerInvariant() ?? IndexConstants.Variation.DefaultSegment;

    public static string ValidIndexAlias(this string indexAlias)
        => indexAlias;

    // TODO: escape quotes here? spaces seem to work just fine.
    //       see https://www.algolia.com/doc/api-reference/api-parameters/filters/?client=csharp#examples
    public static string EscapedFilterValue(this string value)
        => value;
}
