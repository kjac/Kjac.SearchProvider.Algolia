using System.Text.Json.Serialization;
using Kjac.SearchProvider.Algolia.Constants;

namespace Kjac.SearchProvider.Algolia.Models;

public abstract class IndexDocumentBase
{
    [JsonPropertyName(IndexConstants.FieldNames.Id)]
    public required string Id { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.PathKeys)]
    public string[] PathIds { get; set; } = [];
}
