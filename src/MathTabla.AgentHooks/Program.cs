using System.Text.Json;
using System.Text.RegularExpressions;

return await AgentHooksApp.RunAsync(args);

internal static class AgentHooksApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Missing command. Supported commands: pre-tool-policy");
            return 1;
        }

        return args[0] switch
        {
            "pre-tool-policy" => await PreToolPolicyCommand.RunAsync(),
            "--help" or "-h" => WriteHelp(),
            _ => UnknownCommand(args[0])
        };
    }

    private static int WriteHelp()
    {
        Console.WriteLine("MathTabla.AgentHooks");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  pre-tool-policy    Read a tool-use JSON payload from stdin and block dangerous commands.");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'. Supported commands: pre-tool-policy");
        return 1;
    }
}

internal static class PreToolPolicyCommand
{
    public static async Task<int> RunAsync()
    {
        var payload = await Console.In.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(payload))
        {
            Console.Error.WriteLine("No JSON payload was provided on stdin.");
            return 1;
        }

        AgentHookRequest request;
        try
        {
            request = AgentHookRequest.FromJson(payload);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Invalid JSON payload: {ex.Message}");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return 0;
        }

        var decision = CommandPolicy.Evaluate(request.Command);
        if (decision.Allowed)
        {
            return 0;
        }

        Console.Error.WriteLine(decision.Reason);
        return 2;
    }
}

internal sealed record AgentHookRequest(
    string? HookEventName,
    string? ToolName,
    string? Command)
{
    public static AgentHookRequest FromJson(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var hookEventName = JsonReader.GetString(root, "hook_event_name", "hookEventName");
        var toolName = JsonReader.GetString(root, "tool_name", "toolName");
        var command =
            JsonReader.GetNestedString(root, ["tool_input", "command"]) ??
            JsonReader.GetNestedString(root, ["toolInput", "command"]) ??
            JsonReader.GetString(root, "command");

        return new AgentHookRequest(hookEventName, toolName, command);
    }
}

internal static class JsonReader
{
    public static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    public static string? GetNestedString(JsonElement element, IReadOnlyList<string> path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }
}

internal static partial class CommandPolicy
{
    private static readonly string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static PolicyDecision Evaluate(string command)
    {
        if (RmRfRegex().IsMatch(command))
        {
            return PolicyDecision.Block("Blocked command: recursive forced deletion with rm -rf is not allowed.");
        }

        if (PowerShellForcedRecursiveDeleteRegex().IsMatch(command))
        {
            return PolicyDecision.Block("Blocked command: Remove-Item with both -Recurse and -Force is not allowed.");
        }

        if (DropTableRegex().IsMatch(command))
        {
            return PolicyDecision.Block("Blocked command: DROP TABLE requires explicit review before execution.");
        }

        if (TargetsGitInternals(command) && IsDestructiveFileCommand(command))
        {
            return PolicyDecision.Block("Blocked command: destructive operations targeting .git are not allowed.");
        }

        if (TargetsProtectedRoot(command) && IsDestructiveFileCommand(command))
        {
            return PolicyDecision.Block("Blocked command: destructive operations targeting a user profile or system folder require explicit review.");
        }

        return PolicyDecision.Allow();
    }

    private static bool IsDestructiveFileCommand(string command) =>
        DestructiveFileCommandRegex().IsMatch(command);

    private static bool TargetsGitInternals(string command) =>
        command.Contains(".git", StringComparison.OrdinalIgnoreCase);

    private static bool TargetsProtectedRoot(string command)
    {
        var protectedTargets = new[]
        {
            UserProfile,
            "%USERPROFILE%",
            "$env:USERPROFILE",
            "~",
            @"C:\",
            @"C:\Windows",
            @"C:\Windows\System32",
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        return protectedTargets
            .Where(target => !string.IsNullOrWhiteSpace(target))
            .Any(target => ContainsPathTarget(command, target));
    }

    private static bool ContainsPathTarget(string command, string target)
    {
        var escaped = Regex.Escape(target.TrimEnd('\\', '/'));
        var pattern = $@"(^|[\s'""]){escaped}([\\/]?)([\s'""]|$)";
        return Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    [GeneratedRegex(@"\brm\s+.*-[A-Za-z]*r[A-Za-z]*f[A-Za-z]*\b|\brm\s+.*-[A-Za-z]*f[A-Za-z]*r[A-Za-z]*\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RmRfRegex();

    [GeneratedRegex(@"\b(remove-item|rm|del|erase)\b(?=.*\s-(recurse|r)\b)(?=.*\s-(force|f)\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PowerShellForcedRecursiveDeleteRegex();

    [GeneratedRegex(@"\bdrop\s+table\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DropTableRegex();

    [GeneratedRegex(@"\b(rm|remove-item|del|erase|rmdir|rd)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DestructiveFileCommandRegex();
}

internal sealed record PolicyDecision(bool Allowed, string? Reason)
{
    public static PolicyDecision Allow() => new(true, null);

    public static PolicyDecision Block(string reason) => new(false, reason);
}
