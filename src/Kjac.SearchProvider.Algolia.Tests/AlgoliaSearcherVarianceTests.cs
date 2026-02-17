using Kjac.SearchProvider.Algolia.Constants;
using Kjac.SearchProvider.Algolia.Extensions;
using Kjac.SearchProvider.Algolia.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;

namespace Kjac.SearchProvider.Algolia.Tests;

public partial class AlgoliaSearcherVarianceTests : AlgoliaTestBase
{
    private const string FieldInvariance = "FieldOne";
    private const string FieldCultureVariance = "FieldTwo";
    private const string FieldMixedVariance = "FieldThree";
    private const string FieldSegmentVariance = "FieldFour";

    protected override string IndexAlias => nameof(AlgoliaSearcherVarianceTests);

    private Dictionary<int, Guid> _variantDocumentIds = [];
    private  Dictionary<int, Guid> _invariantDocumentIds = [];

    [SetUp]
    public void SetUpTest()
    {
        _variantDocumentIds = GenerateVariantDocumentIds();
        _invariantDocumentIds =  GenerateInvariantDocumentIds();
    }

    [Test]
    [Ignore("Invoke this to rebuild the test index")]
    public async Task RebuildIndex()
    {
        await EnsureIndex();

        IAlgoliaIndexer indexer = GetRequiredService<IAlgoliaIndexer>();

        for (var i = 1; i <= 100; i++)
        {
            Guid id = _variantDocumentIds[i];

            await indexer.AddOrUpdateAsync(
                IndexAlias,
                id,
                UmbracoObjectTypes.Document,
                [
                    new Variation(Culture: "en-US", Segment: null),
                    new Variation(Culture: "da-DK", Segment: null),
                ],
                [
                    new IndexField(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        new IndexValue { Keywords = [id.AsKeyword()], },
                        Culture: null,
                        Segment: null
                    ),
                    new IndexField(
                        FieldInvariance,
                        new IndexValue
                        {
                            Texts = ["invariant", $"invariant{i}", "commoninvariant"]
                        },
                        Culture: null,
                        Segment: null
                    ),
                    new IndexField(
                        FieldCultureVariance,
                        new IndexValue
                        {
                            Texts = ["english", $"english{i}"]
                        },
                        Culture: "en-US",
                        Segment: null
                    ),
                    new IndexField(
                        FieldCultureVariance,
                        new IndexValue
                        {
                            Texts = ["danish", $"danish{i}"]
                        },
                        Culture: "da-DK",
                        Segment: null
                    ),
                    new IndexField(
                        FieldMixedVariance,
                        new IndexValue
                        {
                            Texts = ["mixedinvariant", $"mixedinvariant{i}"]
                        },
                        Culture: null,
                        Segment: null
                    ),
                    new IndexField(
                        FieldMixedVariance,
                        new IndexValue
                        {
                            Texts = ["mixedenglish",  $"mixedenglish{i}"]
                        },
                        Culture: "en-US",
                        Segment: null
                    ),
                    new IndexField(
                        FieldMixedVariance,
                        new IndexValue
                        {
                            Texts = ["mixeddanish",   $"mixeddanish{i}"]
                        },
                        Culture: "da-DK",
                        Segment: null
                    ),
                    new IndexField(
                        FieldSegmentVariance,
                        new IndexValue
                        {
                            Texts = ["defaultenglish",   $"defaultenglish{i}"]
                        },
                        Culture: "en-US",
                        Segment: null
                    ),
                    new IndexField(
                        FieldSegmentVariance,
                        new IndexValue
                        {
                            Texts = ["seg1english",   $"seg1english{i}"]
                        },
                        Culture: "en-US",
                        Segment: "seg1"
                    ),
                    new IndexField(
                        FieldSegmentVariance,
                        new IndexValue
                        {
                            Texts = ["defaultdanish",   $"defaultdanish{i}"]
                        },
                        Culture: "da-DK",
                        Segment: null
                    ),
                    new IndexField(
                        FieldSegmentVariance,
                        new IndexValue
                        {
                            Texts = ["seg1danish",   $"seg1danish{i}"]
                        },
                        Culture: "da-DK",
                        Segment: "seg1"
                    ),
                ],
                null
            );

            id = _invariantDocumentIds[i];

            await indexer.AddOrUpdateAsync(
                IndexAlias,
                id,
                UmbracoObjectTypes.Document,
                [
                    new Variation(Culture: null, Segment: null)
                ],
                [
                    new IndexField(
                        Umbraco.Cms.Search.Core.Constants.FieldNames.PathIds,
                        new IndexValue { Keywords = [id.AsKeyword()], },
                        Culture: null,
                        Segment: null
                    ),
                    new IndexField(
                        FieldInvariance,
                        new IndexValue
                        {
                            Texts = ["commoninvariant", $"commoninvariant{i}"]
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
            SearchResult result = await SearchAsync("commoninvariant");
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
                $"filterOnly({IndexConstants.FieldNames.Fields}.{FieldCultureVariance}{IndexConstants.FieldTypePostfix.Texts})",
                $"filterOnly({IndexConstants.FieldNames.Fields}.{FieldInvariance}{IndexConstants.FieldTypePostfix.Texts})",
                $"filterOnly({IndexConstants.FieldNames.Fields}.{FieldMixedVariance}{IndexConstants.FieldTypePostfix.Texts})"
            ]
        );

    private Dictionary<int, Guid> GenerateVariantDocumentIds()
        => new()
        {
            { 1, Guid.Parse("42290d24-5183-4292-90dd-5a3b0fef9808") },
            { 2, Guid.Parse("b40b337c-c623-4b05-8c53-90e6e4e4bd9a") },
            { 3, Guid.Parse("d1f93806-2cc7-4333-bc56-8f26ff0a55cb") },
            { 4, Guid.Parse("b2701b6e-a4b3-4d6f-9843-0691e6fad266") },
            { 5, Guid.Parse("7070e0f9-6695-4d16-bc70-48dbfd06fd3e") },
            { 6, Guid.Parse("32a8f6f9-a9f9-47ff-8d27-529cdd2859f2") },
            { 7, Guid.Parse("b43766ec-fe2f-4a2b-ac3e-d0f9285829ed") },
            { 8, Guid.Parse("dfebfd79-976b-460e-a297-bde3264f99f4") },
            { 9, Guid.Parse("94f1e090-42de-48c7-ad68-e91b6c8e7f1c") },
            { 10, Guid.Parse("a67f7ec1-b1d6-4c61-a7aa-105003f4265e") },
            { 11, Guid.Parse("97475db9-549b-4447-894f-908ca7568f8f") },
            { 12, Guid.Parse("6ceadaab-6166-411a-8bdb-2734cfca4fc1") },
            { 13, Guid.Parse("3d8e67e4-5d25-46e2-b3f9-357d3a1f4772") },
            { 14, Guid.Parse("24c2bb6b-161e-448b-ab15-ec2141a61aa9") },
            { 15, Guid.Parse("d87039ab-18b1-4a16-999f-fa2426d564e2") },
            { 16, Guid.Parse("bc5e6ae5-59fb-48dc-82ed-1ad71f78614c") },
            { 17, Guid.Parse("15be0d1d-2c58-4098-9e7f-a78eefac7a43") },
            { 18, Guid.Parse("c2763426-cb0f-4b28-ab71-44689cf612c2") },
            { 19, Guid.Parse("b517d415-3995-4602-9824-6c91d64f6e3c") },
            { 20, Guid.Parse("4d0dd982-bc3d-40e6-b909-9af2fffae1e4") },
            { 21, Guid.Parse("c62ab41f-d088-4d4f-a18f-9a53b398da14") },
            { 22, Guid.Parse("b9316738-e61c-40f9-9e3a-020ecb41050e") },
            { 23, Guid.Parse("87d5807c-06e3-4489-ad3c-451439576dd2") },
            { 24, Guid.Parse("066b0a60-272f-4dda-af6a-b87ad68a32a1") },
            { 25, Guid.Parse("b63378d8-6b52-4b2d-bc5b-1454bd870937") },
            { 26, Guid.Parse("db9d16da-6f20-41d5-8ade-3f43846582c1") },
            { 27, Guid.Parse("f1ab1a19-67e1-4ad9-a87d-b7b28d3d10bd") },
            { 28, Guid.Parse("421befee-e789-42a0-9663-2ab493540e69") },
            { 29, Guid.Parse("ce8d467a-4be8-4f49-adc1-773b5a477a39") },
            { 30, Guid.Parse("ecc0aa32-dfd2-45ce-9fdc-c08a9f5113ba") },
            { 31, Guid.Parse("0c511abc-c02e-454c-9f55-2d4e81c0a062") },
            { 32, Guid.Parse("2b08ed5e-8250-44f4-86be-96e60af8c743") },
            { 33, Guid.Parse("cfa1d883-4f63-4981-90a4-70ec423990df") },
            { 34, Guid.Parse("d65eb4ae-ec35-48c6-b0a4-061182f93e53") },
            { 35, Guid.Parse("bd24e8d4-ce11-43c3-80f3-95da5d15d565") },
            { 36, Guid.Parse("7c6cedc7-596d-486f-975a-a4d93eb66a1c") },
            { 37, Guid.Parse("ac4dae83-1855-43b7-b203-5c11aa9ecad0") },
            { 38, Guid.Parse("cc9a487b-57b1-463d-8dcf-b91e04e6159f") },
            { 39, Guid.Parse("ed573ccc-f2de-4ce1-b984-80963f7ef145") },
            { 40, Guid.Parse("0b44a98e-6481-4359-bdec-b4eb16c4a890") },
            { 41, Guid.Parse("eae7d8f7-e152-4f74-892e-2b9fe6fb8141") },
            { 42, Guid.Parse("21e0d108-5795-4fa4-945f-84796c609d3e") },
            { 43, Guid.Parse("55d4d750-f232-45fd-8030-8e7dd75a8d3f") },
            { 44, Guid.Parse("7cc8128d-8374-480a-a77c-48a54207b340") },
            { 45, Guid.Parse("3d86ad0f-1a07-43bc-a2a9-af633f3d6b76") },
            { 46, Guid.Parse("d922ef7f-02f6-469c-b18c-74c595aeb9a3") },
            { 47, Guid.Parse("fbfe05dc-88ad-42b2-8eb9-c244cb4030e1") },
            { 48, Guid.Parse("7cc9b006-cc96-4bab-8c7a-bbda9ee92e32") },
            { 49, Guid.Parse("49177b99-8062-464f-9260-8b6f6bd30c24") },
            { 50, Guid.Parse("30608393-9173-428d-badf-7886c4671ec9") },
            { 51, Guid.Parse("0ccd9650-16bb-415a-859f-7cf905466f77") },
            { 52, Guid.Parse("cb695aba-ca3d-4f69-af78-faf8a8e44447") },
            { 53, Guid.Parse("80f7c036-fb31-4d8e-85d8-9b7a25ca37c1") },
            { 54, Guid.Parse("fa46a8da-b7ff-4d4f-9294-ea18c8e0e572") },
            { 55, Guid.Parse("da5b3e1f-22e3-4585-a739-68127703c32f") },
            { 56, Guid.Parse("03895e3c-0096-44d8-900b-fbbf673703a6") },
            { 57, Guid.Parse("8a47b5ae-1b88-439f-8364-957a36c67e3b") },
            { 58, Guid.Parse("d2c47abe-9b21-41a4-bb57-b47e962cf96e") },
            { 59, Guid.Parse("16469977-dc91-47b9-87b9-63a0a7dcd5c1") },
            { 60, Guid.Parse("06e4d9fd-c20c-4a06-b93b-3d1aa93af9a0") },
            { 61, Guid.Parse("4c516d4f-8ecc-42c0-94aa-2cf35a2ced4e") },
            { 62, Guid.Parse("c4609a57-4413-4051-bdd0-2bc5b275714e") },
            { 63, Guid.Parse("c282e4ea-7d44-4e78-8b25-7eec737ad7c7") },
            { 64, Guid.Parse("8ee93ed0-8e06-4a16-9aca-9a8f4a1258f9") },
            { 65, Guid.Parse("5961cb99-dd9b-471d-8430-b5a829ea4e99") },
            { 66, Guid.Parse("cf91770b-ce4f-4c6f-8922-72d664cb8788") },
            { 67, Guid.Parse("8874c0f9-ee08-4779-a639-571859a716f9") },
            { 68, Guid.Parse("0516dc19-b31c-447f-9fc6-44195c49e241") },
            { 69, Guid.Parse("efdb2cb6-055e-47ad-ab23-1cb11dd42872") },
            { 70, Guid.Parse("c6dbf730-72fd-4fbe-9583-3b936b924d3f") },
            { 71, Guid.Parse("e086710a-abe7-4e89-97d4-ecf6164c54f9") },
            { 72, Guid.Parse("09da9d05-e95e-4597-81b4-2d7ba4d04608") },
            { 73, Guid.Parse("5078dcf7-cc4e-487a-9c0e-962bce202fac") },
            { 74, Guid.Parse("c5a6b2a4-8be0-4be5-81b8-8082243d940a") },
            { 75, Guid.Parse("842f8d06-2c40-4334-ad5e-c0c621af942e") },
            { 76, Guid.Parse("648667ee-435e-45de-baf1-3d3642076af2") },
            { 77, Guid.Parse("9665d0e4-51bb-4cd3-9bdb-89d9d1360817") },
            { 78, Guid.Parse("b38c2b37-8fac-4eb0-9e54-c0bc18b93c80") },
            { 79, Guid.Parse("c740cd27-7bb1-4169-a945-38a825a9a1f9") },
            { 80, Guid.Parse("a2de1c7b-9cc7-4110-895b-726982f456bf") },
            { 81, Guid.Parse("9c7ef485-16d6-4951-ac77-5c8701264cdd") },
            { 82, Guid.Parse("314f5ba2-0bf3-438c-b82d-a4d914e9a3d0") },
            { 83, Guid.Parse("42e1d8a9-daf3-4faf-9d7b-2cff27e2f24d") },
            { 84, Guid.Parse("c10e8435-fa3a-431d-8aea-bc9e8b76b766") },
            { 85, Guid.Parse("e5045022-2fe4-4229-ac7e-670f9c77aa54") },
            { 86, Guid.Parse("94956142-19e2-48d0-9031-724aeda0c308") },
            { 87, Guid.Parse("02f62fa0-2fea-4e51-989f-3b87899543e6") },
            { 88, Guid.Parse("41cc68e1-c8d1-4143-ba9b-4ba36b52b08a") },
            { 89, Guid.Parse("8ca110af-7f14-4587-8bc9-60aa2a82f7a0") },
            { 90, Guid.Parse("1517b572-6017-40e1-b850-92a8bb5ecbea") },
            { 91, Guid.Parse("bc9a5ec1-2461-48aa-b79b-bf754dc94643") },
            { 92, Guid.Parse("38e076e6-7e08-4169-805c-605a98def2b8") },
            { 93, Guid.Parse("04d88619-8f7a-4401-b6df-11960563e9cc") },
            { 94, Guid.Parse("dc9460db-a0ba-48cf-a81a-279ff7bb52dd") },
            { 95, Guid.Parse("ad31be80-bc61-4c3f-842f-5270946e119c") },
            { 96, Guid.Parse("a6b74a80-a579-44ce-ba72-8afd4b34e60c") },
            { 97, Guid.Parse("401cd789-cf8a-4bbb-9073-db181ea3e0ff") },
            { 98, Guid.Parse("a636d89e-d5d3-4c10-93ff-bbc529dba176") },
            { 99, Guid.Parse("478a7db2-b36f-4d5b-ac57-fdc0a50a8392") },
            { 100, Guid.Parse("705bbd6a-1a66-4f57-a6ce-e541e452b0a9") }
        };

    private Dictionary<int, Guid> GenerateInvariantDocumentIds()
        => new()
        {
            { 1, Guid.Parse("918dea1d-3c70-4827-8cd0-9e8901ab5b0a") },
            { 2, Guid.Parse("ef0c8229-fcca-4db9-917d-1fc130bc1ba9") },
            { 3, Guid.Parse("3d22dc21-dc1c-41a7-9de4-e03a89626c4a") },
            { 4, Guid.Parse("3dcb354c-b6be-4213-b540-bddcc0377d8a") },
            { 5, Guid.Parse("98c45f37-d3cd-4c59-97a1-8ea8378a58cb") },
            { 6, Guid.Parse("70ae0c84-0a69-404e-8719-32034648e809") },
            { 7, Guid.Parse("df16240c-a554-4ed6-8485-cdb7f4884468") },
            { 8, Guid.Parse("9a1edd59-d5fa-4ef3-9141-b842a4f068f3") },
            { 9, Guid.Parse("cd644ce2-2373-4999-90d5-78a006d516b1") },
            { 10, Guid.Parse("55f03a5d-72b2-4af3-9e26-e7be34b6438c") },
            { 11, Guid.Parse("f458a679-efb8-4e65-822c-fbdabc6995cd") },
            { 12, Guid.Parse("85408238-516b-4e2a-b5e9-ed812340f27f") },
            { 13, Guid.Parse("f0ce5c69-0019-4465-a3b5-99807563e333") },
            { 14, Guid.Parse("78a0b480-c4ad-4c00-8ce6-68c3ac12a22f") },
            { 15, Guid.Parse("91d9376f-82cf-4b2a-9316-e36bd93701ad") },
            { 16, Guid.Parse("bc6b5403-a8bd-4055-a3f9-2f47d7dcfed8") },
            { 17, Guid.Parse("9e19c37b-b2a3-4b5e-a063-1f5d9bf8c56c") },
            { 18, Guid.Parse("d40ae5ae-e019-4469-b9a7-75165d5599a0") },
            { 19, Guid.Parse("fa27e0a4-e7a6-45f0-b011-77af2c83f9bb") },
            { 20, Guid.Parse("06209034-1c8a-4f10-bf9f-0c16df57b0dc") },
            { 21, Guid.Parse("06e49014-2635-4baf-b4bb-8b6f65d37652") },
            { 22, Guid.Parse("86bcb916-3dd8-4b0a-8f65-821e8e5a97b9") },
            { 23, Guid.Parse("f3a754dd-f3a1-4ca1-af64-7cd95db7b659") },
            { 24, Guid.Parse("b7f2156b-682f-41b6-b4f7-148e46fbd8f1") },
            { 25, Guid.Parse("a2406bb5-96b2-45e3-90d7-4966f6b5ca88") },
            { 26, Guid.Parse("67d97aa5-9d48-401e-9ab6-80e5e494d511") },
            { 27, Guid.Parse("fbe4bef4-698a-4dde-b9fa-635c82d9e0a6") },
            { 28, Guid.Parse("40f8c415-2970-48d9-8f42-0b3028456375") },
            { 29, Guid.Parse("a8e0b173-eee4-416d-bd12-f4cf3cddf19c") },
            { 30, Guid.Parse("d433fb17-3a2f-4517-a46e-051b14793c98") },
            { 31, Guid.Parse("d378d0f1-93d7-4d8e-a0a2-3a8025fd2b20") },
            { 32, Guid.Parse("ca4dc290-3afd-4069-8455-53fb676f2d65") },
            { 33, Guid.Parse("b3004bc7-8a13-481b-88eb-f32dc93980de") },
            { 34, Guid.Parse("a783e44d-be9f-418c-a9fc-865e1234136f") },
            { 35, Guid.Parse("cce9520b-36dc-4c6f-a1aa-843ddb840463") },
            { 36, Guid.Parse("33f3da3c-06b2-4da6-8f94-63d326bff09d") },
            { 37, Guid.Parse("235f31c7-3e01-4dd9-8601-8aacedd700d1") },
            { 38, Guid.Parse("4822124c-9d64-40fd-b55e-05b88ee0c527") },
            { 39, Guid.Parse("1265f3bf-2d43-4e14-8081-17ecb6aac98f") },
            { 40, Guid.Parse("9af1c2b4-d338-4fb5-980b-af86d49a67bb") },
            { 41, Guid.Parse("5f079479-6797-423a-8ed5-cc9cbfb9a458") },
            { 42, Guid.Parse("5a79c977-3bd7-4c9d-ae2e-521221eeb966") },
            { 43, Guid.Parse("2d8dd165-71e7-4653-b5c6-92c682a81a95") },
            { 44, Guid.Parse("ae9591ca-9f3a-4b5e-a9e2-8e4b703fbac3") },
            { 45, Guid.Parse("cba809da-4aaf-49a6-88c2-93b6e7a5a3e1") },
            { 46, Guid.Parse("baf9d3b2-6bc6-46df-a2f7-701f3fb93946") },
            { 47, Guid.Parse("430cc261-5e7d-4e97-ba9b-c2606f917142") },
            { 48, Guid.Parse("5990d3ad-41aa-4c98-ad2a-e1d8006a2eee") },
            { 49, Guid.Parse("302a3acc-61c0-4e37-a169-9ad00e59860c") },
            { 50, Guid.Parse("956010a1-07bf-48b4-8be8-6ac31a9a2678") },
            { 51, Guid.Parse("97825a2f-9a9a-4fd0-95ba-61581f289fb6") },
            { 52, Guid.Parse("d7380488-760d-46e8-ae6b-a037e0eeb395") },
            { 53, Guid.Parse("0a86593c-4cd0-4640-97d5-6b9ad1d8f2ef") },
            { 54, Guid.Parse("04554ad6-0b02-418c-a03e-e0360707e904") },
            { 55, Guid.Parse("744f847f-7301-470a-9e17-a7c8f6841bec") },
            { 56, Guid.Parse("087ca30f-806f-4347-970c-5d02baf342ad") },
            { 57, Guid.Parse("bcb30bba-0da3-43b7-9249-3ae7c9225af2") },
            { 58, Guid.Parse("50e83aa0-e3db-4197-9dda-f6d4a665e660") },
            { 59, Guid.Parse("bbb800d4-75e3-4e2f-87c7-4bc1d633b109") },
            { 60, Guid.Parse("0a9f3bb2-54e9-4a1e-b932-d183a24990a1") },
            { 61, Guid.Parse("d0263866-af2f-4112-b434-6c260dacf326") },
            { 62, Guid.Parse("62951f0f-3c59-4d25-ae98-02d3dc2f7204") },
            { 63, Guid.Parse("47998d96-b676-4f50-bbaa-9d7cc8fe6091") },
            { 64, Guid.Parse("098e2695-e6a2-4e83-aec4-0f57a3572566") },
            { 65, Guid.Parse("044fd578-0790-494b-9291-e772aedac637") },
            { 66, Guid.Parse("790e6c47-8a63-46e0-b0ab-83792a8730df") },
            { 67, Guid.Parse("25ed8bb5-acea-42d1-9cf2-f89ed0431640") },
            { 68, Guid.Parse("6e297c93-8fb1-486d-a55c-cc4cce3135a0") },
            { 69, Guid.Parse("56d7cf18-6529-4ed0-ae33-a43169cbc608") },
            { 70, Guid.Parse("4be3146a-3ba6-4d73-be39-c9c4ec6079d8") },
            { 71, Guid.Parse("1a3400c7-2674-462c-b446-acb1be833dfe") },
            { 72, Guid.Parse("d63f372c-0cff-4591-b337-1a26ea4d87d3") },
            { 73, Guid.Parse("4daf3b3c-09fe-4e9f-a0a4-8d3295322ced") },
            { 74, Guid.Parse("26905026-6cd3-4681-886b-2f32362cc63c") },
            { 75, Guid.Parse("acfc4187-1922-442d-a1e2-1917f89c9c34") },
            { 76, Guid.Parse("fd8b109d-20f1-45e3-a67f-4b8ad63953bb") },
            { 77, Guid.Parse("b33efea5-9671-4b70-bcfc-113cb0b091ed") },
            { 78, Guid.Parse("ee269096-56df-4e0a-b419-ef71b2b3e566") },
            { 79, Guid.Parse("aa5a137d-da45-4469-a387-7de16b840538") },
            { 80, Guid.Parse("4625ab27-4c70-4a95-acb7-92dbf243cbb0") },
            { 81, Guid.Parse("ecebd0eb-afdc-4a8b-97f5-9613d07ac95b") },
            { 82, Guid.Parse("48609ccc-54d4-4842-baa2-114000b85a8e") },
            { 83, Guid.Parse("3995e97d-3299-4346-875c-d647597b31f1") },
            { 84, Guid.Parse("576669be-47ff-4ff0-bd8e-d1e6d096c247") },
            { 85, Guid.Parse("40866802-4899-48e9-b58a-10734c6f5c68") },
            { 86, Guid.Parse("3e8167d9-980a-4fb0-89d8-8e93323d8786") },
            { 87, Guid.Parse("f6510b84-7801-4232-b56c-b991fa9196a0") },
            { 88, Guid.Parse("e003d6e8-86cd-4648-8793-bc2936e4c342") },
            { 89, Guid.Parse("3ec85d25-5a6f-4b46-be5a-716396870d97") },
            { 90, Guid.Parse("d69e2055-e814-4df1-ab5d-22dfe67b9d86") },
            { 91, Guid.Parse("9d70f7ad-3849-450c-b592-40399ed87aec") },
            { 92, Guid.Parse("9ea1c556-04e8-4f8f-ae54-a6eb8a2df32d") },
            { 93, Guid.Parse("2e640614-0eae-4013-a844-d98871071447") },
            { 94, Guid.Parse("36214d72-0971-44c5-add8-d54c8293fdbb") },
            { 95, Guid.Parse("f22de2b7-d540-4d96-b223-a7a87d72bc94") },
            { 96, Guid.Parse("fef893e7-955d-46d9-9158-00e46c4737cd") },
            { 97, Guid.Parse("69528cca-67fb-422f-bb6e-32fcf43e8b33") },
            { 98, Guid.Parse("4624018c-443a-4b3e-80eb-7492ad25ea14") },
            { 99, Guid.Parse("4e7a1db8-1386-47cf-a194-5e64a28e3699") },
            { 100, Guid.Parse("7dd68a1b-c38d-4330-bfd6-c03a4edf1263") }
        };
}
