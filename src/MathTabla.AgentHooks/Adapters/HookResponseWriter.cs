using System.Text.Json;
using MathTabla.AgentHooks.Domain;

namespace MathTabla.AgentHooks.Adapters;

internal static class HookResponseWriter
{
    public static int Write(string host, HookDecision decision) =>
        Write(host, decision, Console.Out, Console.Error);

    public static int Allow(string host) => Allow(host, Console.Out);

    public static int Block(string host, string reason) => Block(host, reason, Console.Out, Console.Error);

    internal static int Write(string host, HookDecision decision, TextWriter stdout, TextWriter stderr) =>
        decision.Allowed
            ? Allow(host, stdout)
            : Block(host, decision.Reason ?? "Blocked by pre-tool policy.", stdout, stderr);

    internal static int Allow(string host, TextWriter stdout)
    {
        if (host == HookHosts.Copilot)
        {
            stdout.WriteLine("{}");
        }

        return HookExitCodes.Allow;
    }

    internal static int Block(string host, string reason, TextWriter stdout, TextWriter stderr)
    {
        if (host == HookHosts.Copilot)
        {
            var output = new
            {
                permissionDecision = "deny",
                permissionDecisionReason = reason
            };

            stdout.WriteLine(JsonSerializer.Serialize(output));
            return HookExitCodes.Allow;
        }

        stderr.WriteLine(reason);
        return HookExitCodes.Block;
    }
}
