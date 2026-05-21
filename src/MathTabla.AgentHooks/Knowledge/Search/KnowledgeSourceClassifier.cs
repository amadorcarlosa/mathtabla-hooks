using MathTabla.AgentHooks.Knowledge.Domain;

namespace MathTabla.AgentHooks.Knowledge.Search;

internal static class KnowledgeSourceClassifier
{
    public static string Classify(KnowledgeNoteMetadata note)
    {
        var path = Normalize(note.Path);
        var displayPath = Normalize(note.DisplayPath);

        if (path.StartsWith("/knowledge/sources/", StringComparison.OrdinalIgnoreCase) ||
            displayPath.StartsWith("sources/", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeSourceKinds.Sources;
        }

        if (path.StartsWith("/knowledge/docs/", StringComparison.OrdinalIgnoreCase) ||
            displayPath.StartsWith("docs/", StringComparison.OrdinalIgnoreCase) ||
            displayPath.StartsWith("frontend/", StringComparison.OrdinalIgnoreCase))
        {
            return KnowledgeSourceKinds.Migrated;
        }

        return KnowledgeSourceKinds.Curated;
    }

    private static string Normalize(string value) => value.Replace('\\', '/');
}
