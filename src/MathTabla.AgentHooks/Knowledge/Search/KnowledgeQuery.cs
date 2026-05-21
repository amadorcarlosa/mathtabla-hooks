using System.Text.RegularExpressions;

namespace MathTabla.AgentHooks.Knowledge.Search;

internal static partial class KnowledgeQuery
{
    public static IReadOnlyList<string> Tokenize(string query)
    {
        return QueryTermRegex()
            .Matches(query.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(term => term.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [GeneratedRegex(@"[a-z0-9][a-z0-9\-_.]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex QueryTermRegex();
}
