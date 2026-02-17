namespace Kjac.SearchProvider.Algolia.Constants;

internal static class IndexConstants
{
    public static class Variation
    {
        public const string InvariantCulture = "inv";

        public const string DefaultSegment = "def";
    }

    public static class FieldNames
    {
        public const string Id = "objectID";

        public const string PathKeys = "pathKeys";

        public const string ObjectType = "objectType";

        public const string Key = "key";

        public const string Culture = "culture";

        public const string AccessKeys = "accessKeys";

        public const string AllTexts = "allTexts";

        public const string AllTextsR1 = "allTextsR1";

        public const string AllTextsR2 = "allTextsR2";

        public const string AllTextsR3 = "allTextsR3";

        public const string Fields = "fields";
    }

    public static class FieldTypePostfix
    {
        public const string Texts = "_texts";

        public const string Keywords = "_keywords";

        public const string Integers = "_integers";

        public const string Decimals = "_decimals";

        public const string DateTimeOffsets = "_datetimeoffsets";
    }
}
