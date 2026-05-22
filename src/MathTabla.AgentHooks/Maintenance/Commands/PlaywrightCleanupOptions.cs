namespace MathTabla.AgentHooks.Maintenance.Commands;

internal sealed record PlaywrightCleanupOptions(
    string RepositoryRoot,
    bool Kill,
    string? ScriptPath)
{
    public bool DryRun => !Kill;

    public static bool TryParse(string[] args, out PlaywrightCleanupOptions options, out string? error)
    {
        var repositoryRoot = Environment.CurrentDirectory;
        var kill = false;
        string? scriptPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--repo" when TryReadValue(args, ref i, out var value):
                    repositoryRoot = value;
                    break;
                case "--script" when TryReadValue(args, ref i, out var value):
                    scriptPath = value;
                    break;
                case "--dry-run":
                    kill = false;
                    break;
                case "--kill":
                    kill = true;
                    break;
                default:
                    options = new PlaywrightCleanupOptions(repositoryRoot, kill, scriptPath);
                    error = $"Unknown or invalid playwright-cleanup argument: {args[i]}";
                    return false;
            }
        }

        options = new PlaywrightCleanupOptions(repositoryRoot, kill, scriptPath);
        error = null;
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
