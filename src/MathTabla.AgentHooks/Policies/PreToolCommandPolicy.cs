using MathTabla.AgentHooks.Domain;

namespace MathTabla.AgentHooks.Policies;

internal static class PreToolCommandPolicy
{
    public static HookDecision Evaluate(HookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return HookDecision.Allow();
        }

        var command = request.Command;

        if (DangerousCommandPatterns.IsRmRf(command))
        {
            return HookDecision.Block("Blocked command: recursive forced deletion with rm -rf is not allowed.");
        }

        if (DangerousCommandPatterns.IsPowerShellForcedRecursiveDelete(command))
        {
            return HookDecision.Block("Blocked command: Remove-Item with both -Recurse and -Force is not allowed.");
        }

        if (DangerousCommandPatterns.IsDropTable(command))
        {
            return HookDecision.Block("Blocked command: DROP TABLE requires explicit review before execution.");
        }

        if (DangerousCommandPatterns.TargetsGitInternals(command) &&
            DangerousCommandPatterns.IsDestructiveFileCommand(command))
        {
            return HookDecision.Block("Blocked command: destructive operations targeting .git are not allowed.");
        }

        if (DangerousCommandPatterns.TargetsProtectedRoot(command) &&
            DangerousCommandPatterns.IsDestructiveFileCommand(command))
        {
            return HookDecision.Block("Blocked command: destructive operations targeting a user profile or system folder require explicit review.");
        }

        return HookDecision.Allow();
    }
}
