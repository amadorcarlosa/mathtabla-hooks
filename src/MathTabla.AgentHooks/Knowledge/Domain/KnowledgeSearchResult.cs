namespace MathTabla.AgentHooks.Knowledge.Domain;

internal sealed record KnowledgeSearchResult(
    string Path,
    string Title,
    string Type,
    string? Subtype,
    string? Status,
    string SourceKind,
    int Score,
    int? GraphDistance,
    IReadOnlyList<string> MatchedTerms,
    IReadOnlyList<string> MatchedTags,
    IReadOnlyList<string> MatchedKeywords);
