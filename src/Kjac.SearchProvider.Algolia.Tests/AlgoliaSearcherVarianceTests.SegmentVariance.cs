using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;

namespace Kjac.SearchProvider.Algolia.Tests;

public partial class AlgoliaSearcherVarianceTests
{
    [TestCase("en-US", "english")]
    [TestCase("da-DK", "danish")]
    public async Task CanQuerySingleDocumentByInvariantValueOfSegmentVariantField(string culture, string query)
    {
        SearchResult result = await SearchAsync(
            query: $"default{query}45",
            culture: culture
        );

        Assert.Multiple(
            () =>
            {
                Assert.That(result.Total, Is.EqualTo(1));
                Assert.That(result.Documents.First().Id, Is.EqualTo(_variantDocumentIds[45]));
            }
        );
    }
}
