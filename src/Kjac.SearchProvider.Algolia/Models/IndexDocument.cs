using System.Text.Json.Serialization;
using Kjac.SearchProvider.Algolia.Constants;

namespace Kjac.SearchProvider.Algolia.Models;

public class IndexDocument : IndexDocumentBase
{
    [JsonPropertyName(IndexConstants.FieldNames.ObjectType)]
    public required string? ObjectType { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.Key)]
    public required Guid Key { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.Culture)]
    public required string Culture { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.Segment)]
    public required string Segment { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.AccessKeys)]
    public required Guid[] AccessKeys { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.AllTexts)]
    public required string[] AllTexts { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.AllTextsR1)]
    public required string[] AllTextsR1 { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.AllTextsR2)]
    public required string[] AllTextsR2 { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.AllTextsR3)]
    public required string[] AllTextsR3 { get; init; }

    [JsonPropertyName(IndexConstants.FieldNames.Fields)]
    public required IDictionary<string, object[]> Fields { get; init; }
}
