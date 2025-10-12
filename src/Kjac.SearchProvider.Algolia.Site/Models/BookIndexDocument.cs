using System.Text.Json.Serialization;
using Kjac.SearchProvider.Algolia.Models;

namespace Kjac.SearchProvider.Algolia.Site.Models;

public class BookIndexDocument : IndexDocumentBase
{
    [JsonPropertyName("title")]
    public required string? Title { get; init; }

    [JsonPropertyName("summary")]
    public required string? Summary { get; init; }

    [JsonPropertyName("author")]
    public required string? Author { get; init; }

    [JsonPropertyName("authorNationality")]
    public required string[]? AuthorNationality { get; init; }

    [JsonPropertyName("publishYear")]
    public required int? PublishYear { get; init; }

    [JsonPropertyName("publishYearRange")]
    public required string? PublishYearRange { get; init; }

    [JsonPropertyName("length")]
    public required string? Length { get; init; }
}
