namespace MathTabla.AgentHooks.Knowledge.Domain;

internal sealed record KnowledgeDiscoveryResult(
    string Query,
    IReadOnlyList<string> QueryTerms,
    IReadOnlyList<KnowledgeSearchResult> Research,
    IReadOnlyList<KnowledgeSearchResult> Implementation,
    IReadOnlyList<KnowledgeSearchResult> Related,
    IReadOnlyList<string> RecommendedNextSearches);
