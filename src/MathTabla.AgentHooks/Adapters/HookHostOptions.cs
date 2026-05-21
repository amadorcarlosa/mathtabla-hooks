using MathTabla.AgentHooks.Domain;

namespace MathTabla.AgentHooks.Adapters;

internal static class HookHostOptions
{
    public static string Parse(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "--host", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 >= args.Length)
            {
                return HookHosts.Generic;
            }

            var host = args[i + 1].Trim().ToLowerInvariant();
            return host switch
            {
                HookHosts.Claude => HookHosts.Claude,
                HookHosts.Copilot => HookHosts.Copilot,
                HookHosts.Codex => HookHosts.Codex,
                _ => HookHosts.Generic
            };
        }

        return HookHosts.Generic;
    }
}
