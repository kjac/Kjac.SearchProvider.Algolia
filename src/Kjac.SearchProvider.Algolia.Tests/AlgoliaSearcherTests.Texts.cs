using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Kjac.SearchProvider.Algolia.Tests;

// tests specifically related to the IndexValue.Texts collection
public partial class AlgoliaSearcherTests
{
    [Test]
    public async Task CanFilterSingleDocumentBySpecificText()
    {
        SearchResult result = await SearchAsync(
            filters: [new TextFilter(FieldMultipleValues, ["single12"], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[12]));
            }
        );
    }

    [Test]
    public async Task CanFilterMultipleDocumentsBySpecificText()
    {
        SearchResult result = await SearchAsync(
            filters: [new TextFilter(FieldMultipleValues, ["single11", "single22", "single33"], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(3));

                var documents = result.Documents.ToList();
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EquivalentTo(new[] { _documentIds[11], _documentIds[22], _documentIds[33] })
                );
            }
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanFilterMultipleDocumentsByCommonText(bool even)
    {
        SearchResult result = await SearchAsync(
            filters: [new TextFilter(FieldMultipleValues, [even ? "even" : "odd"], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(50));

                var documents = result.Documents.ToList();
                var expectedIds = OddOrEvenIds(even);
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EquivalentTo(expectedIds.Select(id => _documentIds[id]))
                );
            }
        );
    }

    [Test]
    [Ignore("Negated filters are not yet supported by the Algolia search provider")]
    public async Task CanFilterDocumentsBySpecificTextNegated()
    {
        SearchResult result = await SearchAsync(
            filters: [new TextFilter(FieldMultipleValues, ["single12"], true)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(99));
                Assert.That(
                    result.Documents.Select(d => d.Id),
                    Is.EquivalentTo(_documentIds.Values.Except([_documentIds[12]]))
                );
            }
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    [Ignore("Negated filters are not yet supported by the Algolia search provider")]
    public async Task CanFilterDocumentsByCommonTextNegated(bool even)
    {
        SearchResult result = await SearchAsync(
            filters: [new TextFilter(FieldMultipleValues, [even ? "even" : "odd"], true)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(50));

                var documents = result.Documents.ToList();
                var expectedIds = OddOrEvenIds(even is false);
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EquivalentTo(expectedIds.Select(id => _documentIds[id]))
                );
            }
        );
    }
}
