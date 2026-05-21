using MathTabla.AgentHooks.Adapters;
using MathTabla.AgentHooks.Domain;

namespace MathTabla.AgentHooks.Tests.Adapters;

public sealed class HookResponseWriterTests
{
    [Test]
    [Arguments(HookHosts.Generic)]
    [Arguments(HookHosts.Claude)]
    [Arguments(HookHosts.Codex)]
    public async Task Block_WhenHostUsesStderr_WritesReasonAndReturnsBlockExitCode(string host)
    {
        var result = CaptureOutput((stdout, stderr) => HookResponseWriter.Block(host, "blocked", stdout, stderr));

        await Assert.That(result.ExitCode).IsEqualTo(HookExitCodes.Block);
        await Assert.That(result.StdErr).Contains("blocked");
        await Assert.That(result.StdOut).IsEmpty();
    }

    [Test]
    public async Task Allow_WhenHostIsCopilot_WritesEmptyJsonAndReturnsAllowExitCode()
    {
        var result = CaptureOutput((stdout, _) => HookResponseWriter.Allow(HookHosts.Copilot, stdout));

        await Assert.That(result.ExitCode).IsEqualTo(HookExitCodes.Allow);
        await Assert.That(result.StdOut.Trim()).IsEqualTo("{}");
        await Assert.That(result.StdErr).IsEmpty();
    }

    [Test]
    public async Task Block_WhenHostIsCopilot_WritesPermissionDecisionJsonAndReturnsAllowExitCode()
    {
        var result = CaptureOutput((stdout, stderr) => HookResponseWriter.Block(HookHosts.Copilot, "blocked", stdout, stderr));

        await Assert.That(result.ExitCode).IsEqualTo(HookExitCodes.Allow);
        await Assert.That(result.StdOut).Contains("\"permissionDecision\":\"deny\"");
        await Assert.That(result.StdOut).Contains("\"permissionDecisionReason\":\"blocked\"");
        await Assert.That(result.StdErr).IsEmpty();
    }

    private static (int ExitCode, string StdOut, string StdErr) CaptureOutput(Func<TextWriter, TextWriter, int> action)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = action(stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }
}
