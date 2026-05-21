using System.Text.Json;
using MathTabla.AgentHooks.Adapters;
using MathTabla.AgentHooks.Domain;
using MathTabla.AgentHooks.Normalization;
using MathTabla.AgentHooks.Policies;

namespace MathTabla.AgentHooks;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Missing command. Supported commands: pre-tool-policy");
            return HookExitCodes.InvalidInvocation;
        }

        return args[0] switch
        {
            HookCommandNames.PreToolPolicy => await RunPreToolPolicyAsync(args[1..]),
            "--help" or "-h" => WriteHelp(),
            _ => UnknownCommand(args[0])
        };
    }

    private static async Task<int> RunPreToolPolicyAsync(string[] args)
    {
        var host = HookHostOptions.Parse(args);
        var payload = await Console.In.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(payload))
        {
            Console.Error.WriteLine("No JSON payload was provided on stdin.");
            return HookExitCodes.InvalidInvocation;
        }

        HookRequest request;
        try
        {
            request = HookRequestNormalizer.FromJson(payload);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Invalid JSON payload: {ex.Message}");
            return HookExitCodes.InvalidInvocation;
        }

        var decision = PreToolCommandPolicy.Evaluate(request);
        return HookResponseWriter.Write(host, decision);
    }

    private static int WriteHelp()
    {
        Console.WriteLine("MathTabla.AgentHooks");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  pre-tool-policy [--host generic|claude|copilot|codex]");
        Console.WriteLine("      Read a tool-use JSON payload from stdin and block dangerous commands.");
        return HookExitCodes.Allow;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'. Supported commands: pre-tool-policy");
        return HookExitCodes.InvalidInvocation;
    }
}
