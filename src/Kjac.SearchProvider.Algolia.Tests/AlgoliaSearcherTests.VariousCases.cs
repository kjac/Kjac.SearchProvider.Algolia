using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Kjac.SearchProvider.Algolia.Tests;

// various tests unrelated to specific IndexValue collections or spanning multiple IndexValue collections
public partial class AlgoliaSearcherTests
{
    [Test]
    public async Task SearchingWithoutParametersYieldsNoResults()
    {
        SearchResult result = await SearchAsync();
        Assert.That(result.Total, Is.Zero);
    }

    [Test]
    public async Task FilteringWithoutFacetsYieldsNoFacetValues()
    {
        SearchResult result = await SearchAsync(
            filters: [new IntegerExactFilter(FieldSingleValue, [1, 2, 3], false)]
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(3));
                Assert.That(result.Facets, Is.Empty);
            }
        );
    }

    [Test]
    public async Task CanRetrieveObjectTypes()
    {
        SearchResult result = await SearchAsync(
            filters: [new IntegerExactFilter(FieldSingleValue, [1, 26, 51, 76], false)]
        );

        Assert.That(result.Total, Is.EqualTo(4));

        Assert.Multiple(
            () =>
            {
                UmbracoObjectTypes[] objectTypes = result.Documents.Select(document => document.ObjectType).ToArray();
                Assert.That(
                    objectTypes,
                    Is.EquivalentTo(
                        new[]
                        {
                            UmbracoObjectTypes.Document,
                            UmbracoObjectTypes.Media,
                            UmbracoObjectTypes.Member,
                            UmbracoObjectTypes.Unknown
                        }
                    )
                );
            }
        );
    }


    [Test]
    public async Task CanCombineFacetsWithinFields()
    {
        SearchResult result = await SearchAsync(
            facets:
            [
                new IntegerExactFacet(FieldSingleValue),
                new KeywordFacet(FieldSingleValue)
            ]
        );

        Assert.That(result.Total, Is.EqualTo(100));

        FacetResult[] facets = result.Facets.ToArray();
        Assert.That(facets, Has.Length.EqualTo(2));
        Assert.Multiple(
            () =>
            {
                Assert.That(facets[0].FieldName, Is.EqualTo(FieldSingleValue));
                Assert.That(facets[1].FieldName, Is.EqualTo(FieldSingleValue));
            }
        );

        IntegerExactFacetValue[] integerFacetValues = facets[0].Values
            .OfType<IntegerExactFacetValue>()
            .OrderBy(facet => facet.Key)
            .ToArray();
        KeywordFacetValue[] keywordFacetValues = facets[1].Values.OfType<KeywordFacetValue>().ToArray();
        Assert.Multiple(
            () =>
            {
                Assert.That(integerFacetValues, Has.Length.EqualTo(100));
                Assert.That(keywordFacetValues, Has.Length.EqualTo(100));
            }
        );

        for (var i = 0; i < 100; i++)
        {
            Assert.Multiple(
                () =>
                {
                    Assert.That(integerFacetValues[i].Key, Is.EqualTo(i + 1));
                    Assert.That(integerFacetValues[i].Count, Is.EqualTo(1));

                    KeywordFacetValue? keywordFacetValue = keywordFacetValues
                        .FirstOrDefault(v => v.Key == $"single{i + 1}");
                    Assert.That(keywordFacetValue?.Count, Is.EqualTo(1));
                }
            );
        }
    }

    [Test]
    public async Task CanCombineFacetsAcrossFields()
    {
        SearchResult result = await SearchAsync(
            facets:
            [
                new IntegerExactFacet(FieldSingleValue),
                new KeywordFacet(FieldMultipleValues)
            ]
        );

        Assert.That(result.Total, Is.EqualTo(100));

        FacetResult[] facets = result.Facets.ToArray();
        Assert.That(facets, Has.Length.EqualTo(2));
        Assert.Multiple(
            () =>
            {
                Assert.That(facets[0].FieldName, Is.EqualTo(FieldSingleValue));
                Assert.That(facets[1].FieldName, Is.EqualTo(FieldMultipleValues));
            }
        );

        IntegerExactFacetValue[] integerFacetValues = facets[0].Values
            .OfType<IntegerExactFacetValue>()
            .OrderBy(facet => facet.Key)
            .ToArray();
        KeywordFacetValue[] keywordFacetValues = facets[1].Values.OfType<KeywordFacetValue>().ToArray();
        Assert.Multiple(
            () =>
            {
                Assert.That(integerFacetValues, Has.Length.EqualTo(100));
                Assert.That(keywordFacetValues, Has.Length.EqualTo(103));
            }
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "all")?.Count, Is.EqualTo(100));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "odd")?.Count, Is.EqualTo(50));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "even")?.Count, Is.EqualTo(50));
            }
        );

        for (var i = 0; i < 100; i++)
        {
            Assert.Multiple(
                () =>
                {
                    Assert.That(integerFacetValues[i].Key, Is.EqualTo(i + 1));
                    Assert.That(integerFacetValues[i].Count, Is.EqualTo(1));

                    KeywordFacetValue? keywordFacetValue = keywordFacetValues
                        .FirstOrDefault(v => v.Key == $"single{i + 1}");
                    Assert.That(keywordFacetValue?.Count, Is.EqualTo(1));
                }
            );
        }
    }

    [Test]
    public async Task CanCombineFacetsWithFilteringAcrossFields()
    {
        SearchResult result = await SearchAsync(
            filters: [new IntegerExactFilter(FieldSingleValue, [1, 10, 25, 50, 100], false)],
            facets:
            [
                new IntegerExactFacet(FieldSingleValue),
                new KeywordFacet(FieldMultipleValues)
            ]
        );

        Assert.That(result.Total, Is.EqualTo(5));

        FacetResult[] facets = result.Facets.ToArray();
        Assert.That(facets, Has.Length.EqualTo(2));
        Assert.Multiple(
            () =>
            {
                Assert.That(facets[0].FieldName, Is.EqualTo(FieldSingleValue));
                Assert.That(facets[1].FieldName, Is.EqualTo(FieldMultipleValues));
            }
        );

        IntegerExactFacetValue[] integerFacetValues = facets[0].Values
            .OfType<IntegerExactFacetValue>()
            .OrderBy(facet => facet.Key)
            .ToArray();
        KeywordFacetValue[] keywordFacetValues = facets[1].Values.OfType<KeywordFacetValue>().ToArray();
        Assert.Multiple(
            () =>
            {
                Assert.That(integerFacetValues, Has.Length.EqualTo(100));
                Assert.That(keywordFacetValues, Has.Length.EqualTo(8));
            }
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "all")?.Count, Is.EqualTo(5));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "odd")?.Count, Is.EqualTo(2));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "even")?.Count, Is.EqualTo(3));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "single1")?.Count, Is.EqualTo(1));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "single10")?.Count, Is.EqualTo(1));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "single25")?.Count, Is.EqualTo(1));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "single50")?.Count, Is.EqualTo(1));
                Assert.That(keywordFacetValues.FirstOrDefault(v => v.Key == "single100")?.Count, Is.EqualTo(1));
            }
        );

        for (var i = 0; i < 100; i++)
        {
            Assert.Multiple(
                () =>
                {
                    Assert.That(integerFacetValues[i].Key, Is.EqualTo(i + 1));
                    Assert.That(integerFacetValues[i].Count, Is.EqualTo(1));
                }
            );
        }
    }

    [Test]
    public async Task FilteringOneFieldLimitsFacetCountForAnotherField()
    {
        SearchResult result = await SearchAsync(
            filters: [new IntegerExactFilter(FieldSingleValue, [1, 10, 25, 50, 100], false)],
            facets: [new IntegerExactFacet(FieldMultipleValues)]
        );

        Assert.That(result.Total, Is.EqualTo(5));

        FacetResult[] facets = result.Facets.ToArray();
        Assert.That(facets, Has.Length.EqualTo(1));

        var expectedFacets = new[]
        {
            new { Key = 1000, Count = 1 }, // 100
            new { Key = 500, Count = 1 }, // 50
            new { Key = 100, Count = 2 }, // 10, 100
            new { Key = 100, Count = 2 }, // 10, 100
            new { Key = 50, Count = 1 }, // 50
            new { Key = 25, Count = 1 }, // 25
            new { Key = 10, Count = 2 }, // 1, 10
            new { Key = 1, Count = 1 }, // 1
        };

        IntegerExactFacetValue[] facetValues = facets[0].Values.OfType<IntegerExactFacetValue>().ToArray();
        foreach (var expectedFacet in expectedFacets)
        {
            Assert.Multiple(
                () =>
                {
                    // the integer values are mirrored around 0 (negative and positive values)
                    Assert.That(
                        facetValues.SingleOrDefault(v => v.Key == expectedFacet.Key)?.Count,
                        Is.EqualTo(expectedFacet.Count)
                    );
                    Assert.That(
                        facetValues.SingleOrDefault(v => v.Key == -1 * expectedFacet.Key)?.Count,
                        Is.EqualTo(expectedFacet.Count)
                    );
                }
            );
        }
    }

    [Test]
    [Ignore("Negated filters are not yet supported by the Algolia search provider")]
    public async Task CanMixRegularAndNegatedFilters()
    {
        SearchResult result = await SearchAsync(
            filters:
            [
                new IntegerExactFilter(FieldSingleValue, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], false),
                new DecimalExactFilter(FieldSingleValue, [0.01m, 0.02m, 0.03m, 0.04m, 0.05m], true)
            ]
        );

        Assert.That(result.Total, Is.EqualTo(5));

        Assert.Multiple(
            () =>
            {
                // expecting 6, 7, 8, 9 and 10
                Guid[] documentIds = result.Documents.Select(document => document.Id).ToArray();
                Assert.That(
                    documentIds,
                    Is.EquivalentTo(
                        new[] { _documentIds[6], _documentIds[7], _documentIds[8], _documentIds[9], _documentIds[10] }
                    )
                );
            }
        );
    }

    [Test]
    public async Task CanMixFiltersAcrossFields()
    {
        SearchResult result = await SearchAsync(
            filters:
            [
                new IntegerExactFilter(FieldSingleValue, [1, 2, 3, 4, 5, 6], false),
                new IntegerExactFilter(FieldMultipleValues, [30, 50, 70, 100], false)
            ]
        );

        Assert.That(result.Total, Is.EqualTo(2));

        Assert.Multiple(
            () =>
            {
                // expecting 3 (30) and 5 (50)
                Guid[] documentIds = result.Documents.Select(document => document.Id).ToArray();
                Assert.That(
                    documentIds,
                    Is.EquivalentTo(
                        new[] { _documentIds[3], _documentIds[5] }
                    )
                );
            }
        );
    }
}
