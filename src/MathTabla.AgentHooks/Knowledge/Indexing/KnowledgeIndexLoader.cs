using System.Text.Json;
using MathTabla.AgentHooks.Knowledge.Domain;

namespace MathTabla.AgentHooks.Knowledge.Indexing;

internal static class KnowledgeIndexLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<KnowledgeIndex> LoadAsync(string root)
    {
        var knowledgeRoot = ResolveKnowledgeRoot(root);
        var metadataPath = Path.Combine(knowledgeRoot, "metadata.json");
        var graphPath = Path.Combine(knowledgeRoot, "graph.json");

        if (!File.Exists(metadataPath))
        {
            throw new FileNotFoundException("Knowledge metadata.json was not found.", metadataPath);
        }

        await using var metadataStream = File.OpenRead(metadataPath);
        var notes = await JsonSerializer.DeserializeAsync<List<KnowledgeNoteMetadata>>(metadataStream, JsonOptions)
            ?? [];

        var edges = File.Exists(graphPath)
            ? await LoadGraphEdgesAsync(graphPath)
            : [];

        return new KnowledgeIndex(notes, edges);
    }

    private static string ResolveKnowledgeRoot(string root)
    {
        var fullRoot = Path.GetFullPath(root);
        var metadataAtRoot = Path.Combine(fullRoot, "metadata.json");
        if (File.Exists(metadataAtRoot))
        {
            return fullRoot;
        }

        return Path.Combine(fullRoot, "static", "knowledge");
    }

    private static async Task<IReadOnlyList<KnowledgeGraphEdge>> LoadGraphEdgesAsync(string graphPath)
    {
        await using var stream = File.OpenRead(graphPath);
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;

        var edgeElements = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray(),
            JsonValueKind.Object when root.TryGetProperty("edges", out var edges) && edges.ValueKind == JsonValueKind.Array => edges.EnumerateArray(),
            JsonValueKind.Object when root.TryGetProperty("relationships", out var relationships) && relationships.ValueKind == JsonValueKind.Array => relationships.EnumerateArray(),
            _ => []
        };

        return edgeElements
            .Select(ReadEdge)
            .Where(edge => !string.IsNullOrWhiteSpace(edge.SourcePath) && !string.IsNullOrWhiteSpace(edge.TargetPath))
            .ToList();
    }

    private static KnowledgeGraphEdge ReadEdge(JsonElement element)
    {
        var source = GetString(element, "sourcePath", "source", "from", "path") ?? "";
        var target = GetString(element, "targetPath", "target", "to", "href") ?? "";
        var type = GetString(element, "relationshipType", "type", "rel") ?? "relates_to";
        var resolved = GetBool(element, "resolved") ?? true;

        return new KnowledgeGraphEdge(source, target, type, resolved);
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static bool? GetBool(JsonElement element, string name)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(name, out var value) &&
            value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
    }
}
