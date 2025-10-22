using Kjac.SearchProvider.Algolia.Constants;
using Kjac.SearchProvider.Algolia.Extensions;
using Kjac.SearchProvider.Algolia.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;

namespace Kjac.SearchProvider.Algolia.Tests;

public partial class AlgoliaSearcherTests : AlgoliaTestBase
{
    private const string FieldMultipleValues = "FieldOne";
    private const string FieldSingleValue = "FieldTwo";
    private const string FieldTextRelevance = "FieldFour";

    protected override string IndexAlias => nameof(AlgoliaSearcherTests);

    private Dictionary<int, Guid> _documentIds = [];

    [SetUp]
    public void SetUpTest() => _documentIds = GenerateDocumentIds();

    [Test]
    [Ignore("Invoke this to rebuild the test index")]
    public async Task RebuildIndex()
    {
        await EnsureIndex();

        IAlgoliaIndexer indexer = GetRequiredService<IAlgoliaIndexer>();

        for (var i = 1; i <= 100; i++)
        {
            Guid id = _documentIds[i];

            await indexer.AddOrUpdateAsync(
                IndexAlias,
                id,
                i <= 25
                    ? UmbracoObjectTypes.Document
                    : i <= 50
                        ? UmbracoObjectTypes.Media
                        : i <= 75
                            ? UmbracoObjectTypes.Member
                            : UmbracoObjectTypes.Unknown,
                [new Variation(Culture: null, Segment: null)],
                [
                    new IndexField(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        new IndexValue { Keywords = [id.AsKeyword()], },
                        Culture: null,
                        Segment: null
                    ),
                    new IndexField(
                        FieldMultipleValues,
                        new IndexValue
                        {
                            Decimals = [i * 1.1m, i * 1.5m, i * -1.1m, i * -1.5m],
                            Integers = [i, i * 10, i * -1, i * -10],
                            Keywords = ["all", i % 2 == 0 ? "even" : "odd", $"single{i}"],
                            DateTimeOffsets =
                            [
                                Date(2025, 01, 01),
                                StartDate().AddDays(i),
                                StartDate().AddDays(i * 2)
                            ],
                            Texts = ["all", i % 2 == 0 ? "even" : "odd", $"single{i}", $"phrase search single{i}"]
                        },
                        Culture: null,
                        Segment: null
                    ),
                    new IndexField(
                        FieldSingleValue,
                        new IndexValue
                        {
                            Decimals = [i * 0.01m],
                            Integers = [i],
                            Keywords = [$"single{i}"],
                            DateTimeOffsets = [StartDate().AddDays(i)],
                            Texts = [$"single{i}"]
                        },
                        Culture: null,
                        Segment: null
                    ),
                    new IndexField(
                        FieldTextRelevance,
                        new IndexValue
                        {
                            Texts = [$"texts_{i}", i == 10 ? "special" : "common"],
                            TextsR1 = [$"texts_r1_{i}", i == 30 ? "special" : "common"],
                            TextsR2 = [$"texts_r2_{i}", i == 20 ? "special" : "common"],
                            TextsR3 = [$"texts_r3_{i}", i == 40 ? "special" : "common"]
                        },
                        Culture: null,
                        Segment: null
                    ),
                ],
                null
            );
        }

        var count = 0;
        while (count++ < 10)
        {
            Thread.Sleep(1000);
            SearchResult result = await SearchAsync("all");
            if (result.Total == 100)
            {
                return;
            }
        }

        throw new ApplicationException($"Could not rebuild Algolia index with alias: {IndexAlias.ValidIndexAlias()}");
    }

    private async Task EnsureIndex()
        => await EnsureIndex(
            [
                $"{IndexConstants.FieldNames.Fields}.{FieldMultipleValues}{IndexConstants.FieldTypePostfix.Texts}",
                $"{IndexConstants.FieldNames.Fields}.{FieldMultipleValues}{IndexConstants.FieldTypePostfix.Keywords}",
                $"{IndexConstants.FieldNames.Fields}.{FieldMultipleValues}{IndexConstants.FieldTypePostfix.Integers}",
                $"{IndexConstants.FieldNames.Fields}.{FieldMultipleValues}{IndexConstants.FieldTypePostfix.Decimals}",
                $"{IndexConstants.FieldNames.Fields}.{FieldMultipleValues}{IndexConstants.FieldTypePostfix.DateTimeOffsets}",
                $"{IndexConstants.FieldNames.Fields}.{FieldSingleValue}{IndexConstants.FieldTypePostfix.Keywords}",
                $"{IndexConstants.FieldNames.Fields}.{FieldSingleValue}{IndexConstants.FieldTypePostfix.Integers}",
            ]
        );

    private DateTimeOffset StartDate()
        => Date(2025, 01, 01);

    private DateTimeOffset Date(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        => new(year, month, day, hour, minute, second, TimeSpan.Zero);

    private int[] OddOrEvenIds(bool even)
        => Enumerable
            .Range(1, 50)
            .Select(i => i * 2)
            .Select(i => even ? i : i - 1)
            .ToArray();

    private Dictionary<int, Guid> GenerateDocumentIds()
        => new()
        {
            { 1, Guid.Parse("2a452af3-313e-41c2-9ea7-3f3b74048f2f") },
            { 2, Guid.Parse("0cf7b1dd-f0ff-483f-934d-cc233ea6cebc") },
            { 3, Guid.Parse("03098c96-60bc-4c05-bf2e-2851f60576ee") },
            { 4, Guid.Parse("167c5b2d-a6ef-4c0c-b35d-8409f73f755e") },
            { 5, Guid.Parse("f9b5938e-9ef2-4196-973a-566026f69173") },
            { 6, Guid.Parse("25f1ca65-670f-43fe-ab04-59cfba846e9a") },
            { 7, Guid.Parse("0be22465-06fb-44c7-9eb5-b7685fbbc0f8") },
            { 8, Guid.Parse("6ed44b60-a08d-4590-af58-f5f74d7fe117") },
            { 9, Guid.Parse("3ed05ea7-9a57-4527-bc69-075704c0c5cc") },
            { 10, Guid.Parse("a69f1488-044b-4d85-a028-a0795e869361") },
            { 11, Guid.Parse("31f10f86-3767-4241-8a5c-4f998fb8b6cd") },
            { 12, Guid.Parse("aa6ffe34-d2f0-482f-b6d7-20f9714d6867") },
            { 13, Guid.Parse("3bcfa290-d630-44e2-ba06-734079ad9b09") },
            { 14, Guid.Parse("dbb08704-7a31-4862-bae7-99f31172b4c3") },
            { 15, Guid.Parse("efe655bd-7e83-48d8-9f8f-bcd4617f0c3b") },
            { 16, Guid.Parse("e1578335-809a-4e00-affc-4a54b015f1f8") },
            { 17, Guid.Parse("9cb25635-4d6b-44aa-b2e2-39b1e81e0b5c") },
            { 18, Guid.Parse("0de1624e-5980-4348-9c8f-c9ed2aaba362") },
            { 19, Guid.Parse("8c71c16a-6098-4058-9836-3003f8cbeb47") },
            { 20, Guid.Parse("5423361d-d2c9-4256-a63b-fd7a71d5b6d8") },
            { 21, Guid.Parse("7c4f6f9f-96a8-4489-a1e9-dd3202584a62") },
            { 22, Guid.Parse("f78878a3-17f0-47dd-8387-03083643521f") },
            { 23, Guid.Parse("fc6e539e-6aac-4786-9891-13506e507a19") },
            { 24, Guid.Parse("801f4986-fb81-410f-a900-b05608eacd03") },
            { 25, Guid.Parse("599cca55-1435-4923-815b-47c536970032") },
            { 26, Guid.Parse("4831a4ac-39c7-4352-bdfd-f6c2645dab6c") },
            { 27, Guid.Parse("ebc3cf1b-6a1b-4a93-adad-a18dde16b00e") },
            { 28, Guid.Parse("3290339c-eb38-4d20-83e2-faecc3c1f77f") },
            { 29, Guid.Parse("09935f99-9c29-479c-a788-e4302122fc3f") },
            { 30, Guid.Parse("81ac1f50-7a39-4c3a-95d1-6ed3255fd84d") },
            { 31, Guid.Parse("7557bfbf-c85d-4409-a472-2e0de998b348") },
            { 32, Guid.Parse("a20e8d46-b4f0-4fd2-ab76-968c5ef94e2a") },
            { 33, Guid.Parse("c772f55c-f446-4a21-a81d-f810bc44ac1b") },
            { 34, Guid.Parse("75aa28cf-568e-421e-a6f9-c0579bb698b3") },
            { 35, Guid.Parse("3a22d438-b8a5-462c-bb85-7bb092866860") },
            { 36, Guid.Parse("1be1cd2c-2689-4d9d-a3b0-7bc47a3686a3") },
            { 37, Guid.Parse("6fdab522-e703-4415-a166-67288933a983") },
            { 38, Guid.Parse("9cb0bdf8-67c5-46e8-add1-c0d823696ce4") },
            { 39, Guid.Parse("2ad3eebf-6f3f-4b4b-9520-3ea625a6d418") },
            { 40, Guid.Parse("a2cee039-3cca-455d-8183-d9c99a6b301c") },
            { 41, Guid.Parse("751de66a-1b9d-4392-8dd7-ec8fa758e262") },
            { 42, Guid.Parse("60a61c10-d3c8-43b3-85de-8c017bc0c1c8") },
            { 43, Guid.Parse("6d8330fc-5d73-402b-8f2d-47fc074ee11c") },
            { 44, Guid.Parse("faa97102-d6a9-4c0f-b534-1fa9671dad05") },
            { 45, Guid.Parse("cfc92131-888a-45ce-bec3-26fe8e9d3d4a") },
            { 46, Guid.Parse("dd809324-c486-4892-9d6b-52745f9805b7") },
            { 47, Guid.Parse("a2994268-1827-41a1-a83d-a4e0382e1d38") },
            { 48, Guid.Parse("d4225a95-f679-4ebb-b2ab-c755a1d71e5b") },
            { 49, Guid.Parse("db8475e0-ee96-4b11-b7b8-82d7ef4d6c80") },
            { 50, Guid.Parse("f10c17e9-58c8-489e-bc6c-c6068e1fe82b") },
            { 51, Guid.Parse("d9a43a13-1f1c-4341-8d88-6cafed56768f") },
            { 52, Guid.Parse("bc325f72-9bd6-48fb-9c84-6be5059fc368") },
            { 53, Guid.Parse("0c9dbe61-2727-4764-b33a-2cd3a9e95f70") },
            { 54, Guid.Parse("419fe0d5-3e66-4753-9fe5-4494e400f96a") },
            { 55, Guid.Parse("f925848c-2964-4bdf-bddd-840200f2b722") },
            { 56, Guid.Parse("096b68b8-5425-4f25-98c9-fea3442713fa") },
            { 57, Guid.Parse("ef49e8e4-563e-44fb-99f8-48dabfc507bf") },
            { 58, Guid.Parse("2dd540b5-d949-4065-9c49-ecbc7aa346cf") },
            { 59, Guid.Parse("4863433f-2fa5-4bbd-97ab-29efb378f102") },
            { 60, Guid.Parse("55cfede3-2d67-49da-8818-4a98f2ff3610") },
            { 61, Guid.Parse("a464c74a-d015-4872-ba22-234db5566d9e") },
            { 62, Guid.Parse("049a23cf-b37b-48d3-8a05-ac6dafb0a46d") },
            { 63, Guid.Parse("91b2aa96-c30c-4847-9ca6-7c36c78b5a0e") },
            { 64, Guid.Parse("ea116675-52f0-4fd2-ab2d-eaf60e03addf") },
            { 65, Guid.Parse("a5f77772-d2ba-4adb-a040-5c7756506cf3") },
            { 66, Guid.Parse("c399a8c3-7b1e-41fc-86a9-cb63766ce7c9") },
            { 67, Guid.Parse("f27ce3de-1c0a-47c7-97a6-eda2cdba991f") },
            { 68, Guid.Parse("d1d1a8f4-d764-4fd6-9a7a-e4ea020144ca") },
            { 69, Guid.Parse("19621b9a-6478-43a1-a13a-aa3c63a719d0") },
            { 70, Guid.Parse("f3b0b504-a610-4e02-a304-179096a45e6f") },
            { 71, Guid.Parse("db6df3f4-810c-41c6-8562-bfddc9c13a4f") },
            { 72, Guid.Parse("275cd97e-6f59-4ddb-bc97-15dcb5a56f19") },
            { 73, Guid.Parse("f3937110-bb3e-4588-9696-bf657ce11264") },
            { 74, Guid.Parse("7427ca1d-f314-4e6e-a5ff-98a27a591ff9") },
            { 75, Guid.Parse("b1f5b788-35f8-420c-8953-918bb297aaca") },
            { 76, Guid.Parse("3c6575ac-d222-45db-a06c-933953a88fa5") },
            { 77, Guid.Parse("104380df-5c06-49bc-8deb-ea77740dfeed") },
            { 78, Guid.Parse("e59cda2d-e845-45d2-8bc0-3f0d6a330ee2") },
            { 79, Guid.Parse("1c3f9420-3f93-4b26-8479-d068dc74ae4a") },
            { 80, Guid.Parse("fafbd2bf-dc9d-4e55-9b5a-225d98c99837") },
            { 81, Guid.Parse("c3296248-96d6-4ce0-8c94-3fe86b4a00a3") },
            { 82, Guid.Parse("cc854d00-181c-4391-b5b8-b6a38e181e04") },
            { 83, Guid.Parse("cee4b465-c548-436d-bdd2-7216995976ae") },
            { 84, Guid.Parse("63fb180d-1fb6-46fc-94d0-49abcb840dbf") },
            { 85, Guid.Parse("b3ea19e6-a468-42ee-8b8a-edab98f0c9ad") },
            { 86, Guid.Parse("984e9588-cc01-487f-8cb0-950e9b7fea27") },
            { 87, Guid.Parse("21b7bed2-c417-490c-b8e8-2db01eccd41a") },
            { 88, Guid.Parse("70e7e537-4c7e-4b3f-88c1-868b7dbe1dd7") },
            { 89, Guid.Parse("941bad13-d215-4319-86d7-6c14ca640990") },
            { 90, Guid.Parse("fade4d4e-054c-4ade-a29b-793f548158ce") },
            { 91, Guid.Parse("eac62aa4-7213-40c0-94c4-f8c195e22ca0") },
            { 92, Guid.Parse("1dfd1d37-8728-4199-ab95-a0c84990a8e1") },
            { 93, Guid.Parse("ad2efc70-f7cf-410c-b1ec-36be25027f2b") },
            { 94, Guid.Parse("4a022af1-7f30-4c57-af44-83c09e6665e1") },
            { 95, Guid.Parse("966f9c83-e025-4947-a14d-87f5a4c7e19d") },
            { 96, Guid.Parse("0cef741a-f440-48da-951f-ced7b827b263") },
            { 97, Guid.Parse("0beaed4b-04aa-4e3e-bdee-83540d083123") },
            { 98, Guid.Parse("667252ae-e28b-4630-866b-de0fd43937e5") },
            { 99, Guid.Parse("9580ff9f-223f-4653-93ff-02bf689fbfaf") },
            { 100, Guid.Parse("3870d869-a97c-432b-90f6-422256cf17ce") }
        };
}
