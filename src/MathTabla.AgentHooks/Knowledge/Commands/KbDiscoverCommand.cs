using System.Text.Json;
using MathTabla.AgentHooks.Domain;
using MathTabla.AgentHooks.Knowledge.Indexing;
using MathTabla.AgentHooks.Knowledge.Search;

namespace MathTabla.AgentHooks.Knowledge.Commands;

internal static class KbDiscoverCommand
{
    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<int> RunAsync(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (!KbDiscoverOptions.TryParse(args, out var options, out var error))
        {
            stderr.WriteLine(error);
            return HookExitCodes.InvalidInvocation;
        }

        try
        {
            var index = await KnowledgeIndexLoader.LoadAsync(options.Root);
            var result = KnowledgeSearchEngine.Discover(index, options.Query, options.Depth, options.MaxResults);
            await stdout.WriteLineAsync(JsonSerializer.Serialize(result, OutputOptions));
            return HookExitCodes.Allow;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            stderr.WriteLine($"Knowledge discovery failed: {ex.Message}");
            return HookExitCodes.InvalidInvocation;
        }
    }
}
