using Kjac.SearchProvider.Algolia.Extensions;
using Kjac.SearchProvider.Algolia.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;

namespace Kjac.SearchProvider.Algolia.Tests;

public class AlgoliaSearcherAccessContextTests : AlgoliaTestBase
{
    protected override string IndexAlias => nameof(AlgoliaSearcherAccessContextTests);

    private Dictionary<int, Guid> _documentIds = [];

    private readonly Guid _principalId = Guid.Parse("76a649e8-0a12-4b32-9228-428ac336d3b7");
    private readonly Guid _groupId = Guid.Parse("c95d0218-9737-43cd-863c-28610677ceb0");

    [SetUp]
    public void SetUpTest() => _documentIds = GenerateDocumentIds();

    [Test]
    [Ignore("Invoke this to rebuild the test index")]
    public async Task RebuildIndex()
    {
        await EnsureIndex([]);

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
                        "theField",
                        new IndexValue { Texts = ["search"] },
                        Culture: null,
                        Segment: null
                    ),
                ],
                i % 2 == 0
                    ? null
                    : i > 50
                        ? new ContentProtection([_groupId])
                        : new ContentProtection([_principalId])
            );
        }

        var count = 0;
        while (count++ < 10)
        {
            Thread.Sleep(1000);
            SearchResult result = await SearchAsync("search");
            if (result.Total == 50)
            {
                return;
            }
        }

        throw new ApplicationException($"Could not rebuild Algolia index with alias: {IndexAlias.ValidIndexAlias()}");
    }

    [Test]
    public async Task CanSearchAsAnonymous()
    {
        SearchResult result = await SearchAsync(null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(50));
            Assert.That(
                _documentIds.Where(d => d.Key % 2 == 0).Select(d => d.Value),
                Is.EquivalentTo(result.Documents.Select(d => d.Id)));
        });
    }

    [Test]
    public async Task CanSearchAsSpecificPrincipal()
    {
        SearchResult result = await SearchAsync(new AccessContext(_principalId, null));
        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(75));
            Assert.That(
                _documentIds.Where(d => d.Key <= 50 || d.Key % 2 == 0).Select(d => d.Value),
                Is.EquivalentTo(result.Documents.Select(d => d.Id)));
        });
    }

    [Test]
    public async Task CanSearchAsGroup()
    {
        SearchResult result = await SearchAsync(new AccessContext(Guid.NewGuid(), [_groupId]));
        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(75));
            Assert.That(
                _documentIds.Where(d => d.Key > 50 || d.Key % 2 == 0).Select(d => d.Value),
                Is.EquivalentTo(result.Documents.Select(d => d.Id)));
        });
    }

    [Test]
    public async Task CanBypassProtection()
    {
        SearchResult result = await SearchAsync(AccessContext.BypassProtection());
        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(100));
            Assert.That(_documentIds.Values, Is.EquivalentTo(result.Documents.Select(d => d.Id)));
        });
    }

    private async Task<SearchResult> SearchAsync(AccessContext? accessContext)
        => await SearchAsync("search", accessContext: accessContext);

    private Dictionary<int, Guid> GenerateDocumentIds()
        => new()
        {
            { 1, Guid.Parse("98d9e0d2-0530-4d67-8a67-776a0819a5bd") },
            { 2, Guid.Parse("30233d58-d712-44e3-8cc8-bd84fa7b6bc8") },
            { 3, Guid.Parse("1931e850-50a1-48ab-b0de-958a18c5fd18") },
            { 4, Guid.Parse("3003b287-8e59-4b9c-acdc-d8cfbdec8a1e") },
            { 5, Guid.Parse("59a461b5-7cd0-48b4-9273-86e0c4db8283") },
            { 6, Guid.Parse("ec5d230d-c7bd-4380-b6d9-7f84c4d2c97b") },
            { 7, Guid.Parse("0a31e4f0-09c0-45df-ae39-ce6034ead506") },
            { 8, Guid.Parse("4d63ac76-d036-4c38-912e-4e09a1204b75") },
            { 9, Guid.Parse("0949ea02-67d7-46ad-9f81-70965990772c") },
            { 10, Guid.Parse("08db2672-abfc-44d1-9bb4-24be23bfdce2") },
            { 11, Guid.Parse("cb79a1a9-83c8-40a0-908e-84c56a5d3534") },
            { 12, Guid.Parse("41b87460-4056-4504-9658-e2721dd9dba9") },
            { 13, Guid.Parse("e8a5aaf5-5151-4ced-922b-5fc7835df5c3") },
            { 14, Guid.Parse("dea6357c-1a2d-475e-bebb-b456b48b22c2") },
            { 15, Guid.Parse("9557adc6-82f0-4428-a989-22dea5239918") },
            { 16, Guid.Parse("ed4fa6c7-d5c5-4da8-bbed-4f2522306a75") },
            { 17, Guid.Parse("8677251a-82a7-42b2-a4bb-d06797df5744") },
            { 18, Guid.Parse("1c7db580-7c7f-40f0-b342-a2d27d23159d") },
            { 19, Guid.Parse("9e552df3-754e-4da1-9fc1-4685cca7b5a5") },
            { 20, Guid.Parse("133108c5-4778-4ea5-8742-0ca12ad7af33") },
            { 21, Guid.Parse("df98241b-21b0-448a-9955-0b2033c371a9") },
            { 22, Guid.Parse("3dbfebdb-d370-43fc-ba1f-47cf13d71d94") },
            { 23, Guid.Parse("4da59e96-61b8-4178-8c77-9389f5015183") },
            { 24, Guid.Parse("63246a77-ff64-4f68-b22c-6283bc759b86") },
            { 25, Guid.Parse("46bc6a5a-2c3b-4705-861d-32072407cf0f") },
            { 26, Guid.Parse("8287d247-196e-4439-ba17-d3378ccf0af4") },
            { 27, Guid.Parse("ff3c5d75-d303-40c7-ab2c-1501339dc823") },
            { 28, Guid.Parse("cd8e2a0c-162f-4919-b1f8-4d9984f689d8") },
            { 29, Guid.Parse("9edf5c48-cea1-462b-af58-62c19b37a656") },
            { 30, Guid.Parse("11125273-1f20-4211-8a9d-caf116b2525d") },
            { 31, Guid.Parse("d958bf75-4873-4823-b0d0-1ab9a0017ea8") },
            { 32, Guid.Parse("5afcf92d-ef52-4e1c-8cf4-d92ff5651060") },
            { 33, Guid.Parse("979d9f81-38e7-4c5d-8fd3-e05a37e9b777") },
            { 34, Guid.Parse("5e91b795-d1be-4c1a-984f-21bbe527c8e6") },
            { 35, Guid.Parse("7b9faaee-9f3b-4c3d-b4df-fb99c2187f29") },
            { 36, Guid.Parse("a0fdd5b4-363d-46c5-9df2-c864132081b3") },
            { 37, Guid.Parse("290c83bb-709c-432e-adea-88ca6a843b2b") },
            { 38, Guid.Parse("7713eb65-74e5-469e-8df4-78999e11bb69") },
            { 39, Guid.Parse("357ee765-a995-49c6-913d-28b7a4e68b73") },
            { 40, Guid.Parse("45e92d59-3a0b-43e1-8910-7818de2a9c47") },
            { 41, Guid.Parse("c16bcf6a-9a82-41bf-870e-3238bf846ec4") },
            { 42, Guid.Parse("f7769c6b-d986-40d5-b666-12062a49c422") },
            { 43, Guid.Parse("cf25ab1f-08ed-4089-9c32-60c4b335cc98") },
            { 44, Guid.Parse("b82ea23f-71b2-41f6-bb33-726fc2474359") },
            { 45, Guid.Parse("ed959577-2187-47a7-a111-aa39992b5075") },
            { 46, Guid.Parse("a45c5995-c4eb-4a55-aaa5-f26447d7f457") },
            { 47, Guid.Parse("817e5679-3206-4959-893e-57f023f4a42c") },
            { 48, Guid.Parse("2f2b79f8-9182-4ee2-8c86-e23222232393") },
            { 49, Guid.Parse("1bdfc601-50bd-43b3-81c9-8f50487f3a13") },
            { 50, Guid.Parse("4fe953ed-7ea8-4a1a-af12-a83c2b75e16a") },
            { 51, Guid.Parse("128ddda7-258d-46d5-8e2e-a96529bbef53") },
            { 52, Guid.Parse("23aeaaa8-fedd-4259-b47d-7fe26ecaa587") },
            { 53, Guid.Parse("27cfb8b7-febc-4fc3-abbe-a02a8631b0e2") },
            { 54, Guid.Parse("a7e4e9d1-191a-48ac-ae17-abd597faa375") },
            { 55, Guid.Parse("cc8079ec-f182-4ff6-ae4b-286cf3d5b57b") },
            { 56, Guid.Parse("35fe0445-fe71-42ab-9daa-bed1a5cf02eb") },
            { 57, Guid.Parse("7cd37a3a-00c4-47f4-af13-f5f308d4fe19") },
            { 58, Guid.Parse("f842168a-c47f-4cc9-a25a-5ec2f479d24f") },
            { 59, Guid.Parse("0d1d7fdb-e5cd-49fc-a896-4d318567ef0c") },
            { 60, Guid.Parse("ad96c403-b262-4597-a321-a59405e9bf13") },
            { 61, Guid.Parse("4d49e7d0-81ff-4a55-b7cd-9fec001cde42") },
            { 62, Guid.Parse("2307a559-4311-4d97-aabe-b0bb26895924") },
            { 63, Guid.Parse("c3702f09-a86f-4eef-b787-cc6fd709de50") },
            { 64, Guid.Parse("27238fb2-6fe1-4a23-84db-d9cd9bcf500e") },
            { 65, Guid.Parse("dd9b25cd-89f1-42c8-b211-60656ae26997") },
            { 66, Guid.Parse("edeb0793-9f4a-49a8-a0e3-8bf60c10d4b0") },
            { 67, Guid.Parse("49810fe2-6ca1-46b2-92ab-c355583caa6b") },
            { 68, Guid.Parse("6b2f8e64-7210-4bf4-9064-505ff38fe2dc") },
            { 69, Guid.Parse("224af45c-7911-432a-b5fd-7901acdc1a88") },
            { 70, Guid.Parse("3430ef7a-5ee3-437e-9d55-dc7c024fe463") },
            { 71, Guid.Parse("bcf58e28-d97b-44bb-a0a8-4f7f1a4e3032") },
            { 72, Guid.Parse("e87366b4-08ed-48ff-8980-9c7c8a72a9b9") },
            { 73, Guid.Parse("2f32456a-8d80-43bf-88fb-c2c5e77c64f5") },
            { 74, Guid.Parse("a963632b-ccd8-4d41-a16c-a76d24cba032") },
            { 75, Guid.Parse("dec89f2c-0440-493c-aca5-541eb10b4a44") },
            { 76, Guid.Parse("e022ccd7-66dd-4145-a937-7122117ed979") },
            { 77, Guid.Parse("8ee4633d-f252-40b7-a5bd-df7eaf6b778d") },
            { 78, Guid.Parse("b1babd3e-e361-4f01-81a4-f00349d1253c") },
            { 79, Guid.Parse("a3826374-abbe-4a08-8b0a-a91c40456a58") },
            { 80, Guid.Parse("eb390083-4c1f-4c73-acad-460b5ecac19b") },
            { 81, Guid.Parse("54cf014e-a412-4bd1-b237-930881d52f62") },
            { 82, Guid.Parse("28629204-a9d6-4b35-8f32-42e2009f73a7") },
            { 83, Guid.Parse("91a86612-6727-4592-866e-c3ff9ad9cf3b") },
            { 84, Guid.Parse("450ff134-e766-45ec-8831-bd420f1c5434") },
            { 85, Guid.Parse("5c46a86b-1cd2-480c-8715-0b6f55eb2f3b") },
            { 86, Guid.Parse("e8cb99e0-6ecb-4306-bef0-0336be8438e0") },
            { 87, Guid.Parse("ee79df12-68c8-46bf-9ba3-692906a25c2b") },
            { 88, Guid.Parse("727ca3c9-46d4-4234-a4ff-d6d584c2588f") },
            { 89, Guid.Parse("4e8bef57-07eb-434f-8836-f25d21cd275d") },
            { 90, Guid.Parse("636c2acb-6d34-44c4-b178-7927f36b7171") },
            { 91, Guid.Parse("fef39900-8dbf-4667-95ac-954bd7ef7ec9") },
            { 92, Guid.Parse("a7f4cf44-6a6e-49db-b990-2a8fe44c4fa8") },
            { 93, Guid.Parse("03a62610-b080-4c39-b5b9-6486d60cd945") },
            { 94, Guid.Parse("b6c774aa-b86d-43a1-966d-8b7786c2a8c7") },
            { 95, Guid.Parse("1e2e1745-0c22-43da-9932-54334145ed6b") },
            { 96, Guid.Parse("00163956-f93f-4b08-bb45-3563ac6fcc0c") },
            { 97, Guid.Parse("12812c4a-4b27-4b54-943f-c0e5f45aeefe") },
            { 98, Guid.Parse("b120070c-fe10-46b7-8a2f-fcf3582d483a") },
            { 99, Guid.Parse("03c7b505-3f28-49ba-85a7-54e9fb6955d6") },
            { 100, Guid.Parse("253c13cc-18d0-4cfe-9187-0e0d0ce385a6") }
        };
}
