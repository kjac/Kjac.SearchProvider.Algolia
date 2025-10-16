using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Extensions;

namespace Kjac.SearchProvider.Algolia.Tests;

// tests specifically related to the IndexValue.Decimals collection
public partial class AlgoliaSearcherTests
{
    [Test]
    public async Task CanFilterSingleDocumentByDecimalExact()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalExactFilter(FieldMultipleValues, [1.5m], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [Test]
    public async Task CanFilterSingleDocumentByNegativeDecimalExact()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalExactFilter(FieldMultipleValues, [-1.5m], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [Test]
    public async Task CanFilterSingleDocumentByDecimalRange()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalRangeFilter(FieldMultipleValues, [new DecimalRangeFilterRange(1m, 2m)], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [Test]
    public async Task CanFilterSingleDocumentByNegativeDecimalRange()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalRangeFilter(FieldMultipleValues, [new DecimalRangeFilterRange(-1.9m, -1.1m)], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_documentIds[1]));
            }
        );
    }

    [Test]
    public async Task CanFilterMultipleDocumentsByDecimalExact()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalExactFilter(FieldMultipleValues, [11m, 30m, 46.2m], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(3));

                var documents = result.Documents.ToList();
                // expecting 10 (11), 20 (30), 42 (46.2)
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EquivalentTo(
                        new[]
                        {
                            _documentIds[10],
                            _documentIds[20],
                            _documentIds[42]
                        }
                    )
                );
            }
        );
    }

    [Test]
    public async Task CanFilterMultipleDocumentsByDecimalRange()
    {
        SearchResult result = await SearchAsync(
            filters:
            [
                new DecimalRangeFilter(
                    FieldMultipleValues,
                    [
                        new DecimalRangeFilterRange(1m, 5m),
                        new DecimalRangeFilterRange(20m, 25m),
                        new DecimalRangeFilterRange(100m, 101m)
                    ],
                    false
                )
            ]
        );

        Assert.Multiple(
            () =>
            {
                // expecting
                // - first range: 1, 2, 3, 4
                // - second range: 14 (21), 15 (22.5), 16 (24), 19 (20.9), 20, 21, 22
                // - third range: 67 (100.5), 91 (100.1)
                Assert.That(result.Total, Is.EqualTo(13));

                var documents = result.Documents.ToList();
                Assert.That(
                    documents.Select(d => d.Id),
                    Is.EquivalentTo(
                        new[]
                        {
                            _documentIds[1],
                            _documentIds[2],
                            _documentIds[3],
                            _documentIds[4],
                            _documentIds[14],
                            _documentIds[15],
                            _documentIds[16],
                            _documentIds[19],
                            _documentIds[20],
                            _documentIds[21],
                            _documentIds[22],
                            _documentIds[67],
                            _documentIds[91],
                        }
                    )
                );
            }
        );
    }

    [Test]
    [Ignore("Negated filters are not yet supported by the Algolia search provider")]
    public async Task CanFilterDocumentsByDecimalExactNegated()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalExactFilter(FieldMultipleValues, [1.5m], true)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(99));
                Assert.That(result.Documents.Select(d => d.Id), Is.EqualTo(_documentIds.Values.Skip(1)).AsCollection);
            }
        );
    }

    [Test]
    [Ignore("Negated filters are not yet supported by the Algolia search provider")]
    public async Task CanFilterDocumentsByDecimalRangeNegated()
    {
        SearchResult result = await SearchAsync(
            filters: [new DecimalRangeFilter(FieldMultipleValues, [new DecimalRangeFilterRange(1m, 2m)], true)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(99));
                Assert.That(result.Documents.Select(d => d.Id), Is.EqualTo(_documentIds.Values.Skip(1)).AsCollection);
            }
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanFacetDocumentsByDecimalExact(bool filtered)
    {
        SearchResult result = await SearchAsync(
            facets: [new DecimalExactFacet(FieldMultipleValues)],
            filters: filtered ? [new DecimalExactFilter(FieldMultipleValues, [1.1m, 2.2m, 3.3m], false)] : []
        );

        // expecting the same facets whether filtering is enabled or not, because
        // both faceting and filtering is applied to the same field
        var expectedFacetValues = Enumerable
            .Range(1, 100)
            .SelectMany(i => new[] { i * 1.1m, i * 1.5m, i * -1.1m, i * -1.5m })
            .GroupBy(i => i)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToArray();

        // expecting
        // - when filtered: 1, 2 and 3
        // - when not filtered: all of them
        Assert.That(result.Total, Is.EqualTo(filtered ? 3 : 100));

        FacetResult[] facets = result.Facets.ToArray();
        Assert.That(facets, Has.Length.EqualTo(1));

        FacetResult facet = facets.First();
        Assert.That(facet.FieldName, Is.EqualTo(FieldMultipleValues));

        DecimalExactFacetValue[] facetValues = facet.Values.OfType<DecimalExactFacetValue>().ToArray();
        Assert.That(facetValues, Has.Length.EqualTo(expectedFacetValues.Length));
        foreach (var expectedFacetValue in expectedFacetValues)
        {
            DecimalExactFacetValue? facetValue = facetValues.FirstOrDefault(f => f.Key == expectedFacetValue.Key);
            Assert.That(facetValue, Is.Not.Null);
            Assert.That(facetValue.Count, Is.EqualTo(expectedFacetValue.Count));
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CanHandleDecimalRangeFacet(bool filtered)
    {
        SearchResult result = await SearchAsync(
            facets:
            [
                new DecimalRangeFacet(
                    FieldMultipleValues,
                    [
                        new DecimalRangeFacetRange("One", 1m, 25m),
                        new DecimalRangeFacetRange("Two", 25m, 50m),
                        new DecimalRangeFacetRange("Three", 50m, 75m),
                        new DecimalRangeFacetRange("Four", 75m, 100m)
                    ]
                )
            ],
            filters: filtered ? [new DecimalExactFilter(FieldMultipleValues, [1.1m, 2.2m, 3.3m], false)] : []
        );

        Assert.Multiple(() =>
        {
            // expecting
            // - when filtered: 1, 2 and 3
            // - when not filtered: all of them
            Assert.That(result.Total, Is.EqualTo(filtered ? 3 : 100));

            // expecting no facets - range facets should be explicitly ignored by the Algolia search provider
            Assert.That(result.Facets, Is.Empty);
        });
    }
}
