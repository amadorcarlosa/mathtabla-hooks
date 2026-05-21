using MathTabla.AgentHooks.Knowledge.Domain;

namespace MathTabla.AgentHooks.Knowledge.Search;

internal static class KnowledgeSearchEngine
{
    public static KnowledgeDiscoveryResult Discover(KnowledgeIndex index, string query, int graphDepth, int maxResults)
    {
        var terms = KnowledgeQuery.Tokenize(query);
        var metadataByPath = index.Notes
            .GroupBy(note => NormalizePath(note.Path))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var scored = index.Notes
            .Select(note => ScoreNote(note, terms, graphDistance: null))
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var seedPaths = scored
            .Where(result => result.SourceKind == KnowledgeSourceKinds.Sources)
            .Take(maxResults)
            .Select(result => result.Path)
            .ToList();

        var neighborDistances = GetNeighborDistances(index.Edges, seedPaths, graphDepth);
        var neighborResults = neighborDistances
            .Where(pair => metadataByPath.ContainsKey(NormalizePath(pair.Key)))
            .Select(pair => ScoreNote(metadataByPath[NormalizePath(pair.Key)], terms, pair.Value))
            .Where(result => result.Score > 0 || result.GraphDistance is not null)
            .ToList();

        var allResults = scored
            .Concat(neighborResults)
            .GroupBy(result => NormalizePath(result.Path), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(result => result.Score).ThenBy(result => result.GraphDistance ?? int.MaxValue).First())
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.GraphDistance ?? int.MaxValue)
            .ThenBy(result => result.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var research = allResults
            .Where(result => result.SourceKind == KnowledgeSourceKinds.Sources)
            .Take(maxResults)
            .ToList();

        var implementation = allResults
            .Where(result => result.SourceKind == KnowledgeSourceKinds.Migrated)
            .Take(maxResults)
            .ToList();

        var selected = research
            .Select(result => NormalizePath(result.Path))
            .Concat(implementation.Select(result => NormalizePath(result.Path)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var related = allResults
            .Where(result => !selected.Contains(NormalizePath(result.Path)))
            .Take(maxResults)
            .ToList();

        var recommendedNextSearches = BuildRecommendedNextSearches(research, implementation, terms);

        return new KnowledgeDiscoveryResult(query, terms, research, implementation, related, recommendedNextSearches);
    }

    private static KnowledgeSearchResult ScoreNote(KnowledgeNoteMetadata note, IReadOnlyList<string> terms, int? graphDistance)
    {
        var matchedTags = note.Tags
            .Where(tag => terms.Any(term => Contains(tag, term)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedKeywords = note.Keywords
            .Where(keyword => terms.Any(term => Contains(keyword, term)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var searchableText = GetSearchableText(note);
        var matchedTerms = terms
            .Where(term => searchableText.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var titleMatches = terms.Count(term => Contains(note.Title, term));
        var score =
            matchedTags.Count * 10 +
            matchedKeywords.Count * 5 +
            titleMatches * 4 +
            matchedTerms.Count;

        if (graphDistance is not null)
        {
            score += Math.Max(1, 4 - graphDistance.Value);
        }

        return new KnowledgeSearchResult(
            note.Path,
            note.Title,
            note.Type,
            note.Subtype,
            note.Status,
            KnowledgeSourceClassifier.Classify(note),
            score,
            graphDistance,
            matchedTerms,
            matchedTags,
            matchedKeywords);
    }

    private static IReadOnlyDictionary<string, int> GetNeighborDistances(
        IReadOnlyList<KnowledgeGraphEdge> edges,
        IReadOnlyList<string> seedPaths,
        int depth)
    {
        var adjacency = edges
            .Where(edge => edge.Resolved)
            .SelectMany(edge => new[]
            {
                (From: NormalizePath(edge.SourcePath), To: NormalizePath(edge.TargetPath)),
                (From: NormalizePath(edge.TargetPath), To: NormalizePath(edge.SourcePath))
            })
            .GroupBy(edge => edge.From, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => edge.To).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var distances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(string Path, int Distance)>();

        foreach (var seed in seedPaths.Select(NormalizePath))
        {
            distances[seed] = 0;
            queue.Enqueue((seed, 0));
        }

        while (queue.Count > 0)
        {
            var (path, distance) = queue.Dequeue();
            if (distance >= depth || !adjacency.TryGetValue(path, out var neighbors))
            {
                continue;
            }

            foreach (var neighbor in neighbors)
            {
                if (distances.ContainsKey(neighbor))
                {
                    continue;
                }

                distances[neighbor] = distance + 1;
                queue.Enqueue((neighbor, distance + 1));
            }
        }

        return distances;
    }

    private static IReadOnlyList<string> BuildRecommendedNextSearches(
        IReadOnlyList<KnowledgeSearchResult> research,
        IReadOnlyList<KnowledgeSearchResult> implementation,
        IReadOnlyList<string> terms)
    {
        return research
            .Concat(implementation)
            .SelectMany(result => result.MatchedTags.Concat(result.MatchedKeywords))
            .Select(value => value.ToLowerInvariant())
            .Where(value => !terms.Contains(value, StringComparer.OrdinalIgnoreCase))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .Select(group => group.Key)
            .ToList();
    }

    private static string GetSearchableText(KnowledgeNoteMetadata note)
    {
        var parts = new List<string>
        {
            note.Title,
            note.DisplayPath,
            note.Type,
            note.Subtype ?? "",
            note.Status ?? ""
        };

        parts.AddRange(note.Tags);
        parts.AddRange(note.Keywords);
        return string.Join(" ", parts);
    }

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
