namespace MathTabla.AgentHooks.Domain;

internal sealed record HookRequest(
    string? EventName,
    string? ToolName,
    string? Command,
    string? WorkingDirectory,
    string? SessionId);
