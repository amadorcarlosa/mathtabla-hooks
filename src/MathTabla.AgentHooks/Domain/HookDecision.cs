namespace MathTabla.AgentHooks.Domain;

internal sealed record HookDecision(bool Allowed, string? Reason)
{
    public static HookDecision Allow() => new(true, null);

    public static HookDecision Block(string reason) => new(false, reason);
}
