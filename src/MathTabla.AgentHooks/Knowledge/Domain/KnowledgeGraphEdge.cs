namespace MathTabla.AgentHooks.Knowledge.Domain;

internal sealed record KnowledgeGraphEdge(
    string SourcePath,
    string TargetPath,
    string RelationshipType,
    bool Resolved);
