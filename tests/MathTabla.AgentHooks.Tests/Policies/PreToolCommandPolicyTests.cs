using MathTabla.AgentHooks.Domain;
using MathTabla.AgentHooks.Policies;

namespace MathTabla.AgentHooks.Tests.Policies;

public sealed class PreToolCommandPolicyTests
{
    [Test]
    [Arguments("dotnet build")]
    [Arguments("git status")]
    public async Task Evaluate_WhenCommandIsSafe_AllowsCommand(string command)
    {
        var decision = PreToolCommandPolicy.Evaluate(CreateRequest(command));

        await Assert.That(decision.Allowed).IsTrue();
    }

    [Test]
    public async Task Evaluate_WhenCommandUsesRmRf_BlocksCommand()
    {
        var decision = PreToolCommandPolicy.Evaluate(CreateRequest("rm -rf .git"));

        await Assert.That(decision.Allowed).IsFalse();
        await Assert.That(decision.Reason).Contains("rm -rf");
    }

    [Test]
    public async Task Evaluate_WhenCommandUsesPowerShellForcedRecursiveDelete_BlocksCommand()
    {
        var decision = PreToolCommandPolicy.Evaluate(CreateRequest("Remove-Item .git -Recurse -Force"));

        await Assert.That(decision.Allowed).IsFalse();
        await Assert.That(decision.Reason).Contains("Remove-Item");
    }

    [Test]
    public async Task Evaluate_WhenCommandDropsTable_BlocksCommand()
    {
        var decision = PreToolCommandPolicy.Evaluate(CreateRequest("psql -c \"DROP TABLE Students\""));

        await Assert.That(decision.Allowed).IsFalse();
        await Assert.That(decision.Reason).Contains("DROP TABLE");
    }

    [Test]
    public async Task Evaluate_WhenDestructiveCommandTargetsGitInternals_BlocksCommand()
    {
        var decision = PreToolCommandPolicy.Evaluate(CreateRequest("rmdir .git"));

        await Assert.That(decision.Allowed).IsFalse();
        await Assert.That(decision.Reason).Contains(".git");
    }

    [Test]
    public async Task Evaluate_WhenDestructiveCommandTargetsProtectedRoot_BlocksCommand()
    {
        var decision = PreToolCommandPolicy.Evaluate(CreateRequest(@"Remove-Item C:\Windows -Recurse"));

        await Assert.That(decision.Allowed).IsFalse();
        await Assert.That(decision.Reason).Contains("system folder");
    }

    private static HookRequest CreateRequest(string command) =>
        new(
            EventName: HookEvents.PreToolUse,
            ToolName: HookToolNames.Shell,
            Command: command,
            WorkingDirectory: null,
            SessionId: null);
}
