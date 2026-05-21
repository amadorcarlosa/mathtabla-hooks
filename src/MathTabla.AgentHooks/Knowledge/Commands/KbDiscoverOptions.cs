namespace MathTabla.AgentHooks.Knowledge.Commands;

internal sealed record KbDiscoverOptions(
    string Root,
    string Query,
    int Depth,
    int MaxResults)
{
    public static bool TryParse(string[] args, out KbDiscoverOptions options, out string? error)
    {
        var root = Environment.CurrentDirectory;
        string? query = null;
        var depth = 2;
        var maxResults = 10;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--root" when TryReadValue(args, ref i, out var value):
                    root = value;
                    break;
                case "--query" when TryReadValue(args, ref i, out var value):
                    query = value;
                    break;
                case "--depth" when TryReadValue(args, ref i, out var value) && int.TryParse(value, out var parsedDepth):
                    depth = Math.Clamp(parsedDepth, 0, 5);
                    break;
                case "--max-results" when TryReadValue(args, ref i, out var value) && int.TryParse(value, out var parsedMaxResults):
                    maxResults = Math.Clamp(parsedMaxResults, 1, 50);
                    break;
                default:
                    error = $"Unknown or invalid kb-discover argument: {args[i]}";
                    options = new KbDiscoverOptions(root, query ?? "", depth, maxResults);
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            error = "Missing required argument: --query <query>";
            options = new KbDiscoverOptions(root, "", depth, maxResults);
            return false;
        }

        error = null;
        options = new KbDiscoverOptions(root, query, depth, maxResults);
        return true;
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = "";
            return false;
        }

        value = args[++index];
        return true;
    }
}
