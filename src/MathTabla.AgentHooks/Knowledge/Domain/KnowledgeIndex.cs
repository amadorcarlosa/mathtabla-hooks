namespace MathTabla.AgentHooks.Knowledge.Domain;

internal sealed record KnowledgeIndex(
    IReadOnlyList<KnowledgeNoteMetadata> Notes,
    IReadOnlyList<KnowledgeGraphEdge> Edges);
