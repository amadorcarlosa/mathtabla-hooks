using System.Text.Json.Serialization;

namespace MathTabla.AgentHooks.Knowledge.Domain;

internal sealed record KnowledgeNoteMetadata
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = "";

    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("subtype")]
    public string? Subtype { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("tags")]
    public string[] Tags { get; init; } = [];

    [JsonPropertyName("keywords")]
    public string[] Keywords { get; init; } = [];

    [JsonPropertyName("displayPath")]
    public string DisplayPath { get; init; } = "";

    [JsonPropertyName("outgoingRelationshipCount")]
    public int OutgoingRelationshipCount { get; init; }

    [JsonPropertyName("sectionCount")]
    public int SectionCount { get; init; }
}
