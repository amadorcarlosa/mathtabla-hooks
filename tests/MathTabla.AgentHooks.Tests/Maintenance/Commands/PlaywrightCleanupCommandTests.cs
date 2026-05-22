using MathTabla.AgentHooks.Maintenance.Commands;

namespace MathTabla.AgentHooks.Tests.Maintenance.Commands;

public sealed class PlaywrightCleanupCommandTests
{
    [Test]
    public async Task ResolveScriptPath_WhenScriptIsProvided_UsesExplicitScript()
    {
        var options = new PlaywrightCleanupOptions(@"C:\repo", Kill: false, ScriptPath: @"C:\tools\cleanup.ps1");

        var path = PlaywrightCleanupCommand.ResolveScriptPath(options);

        await Assert.That(path).IsEqualTo(Path.GetFullPath(@"C:\tools\cleanup.ps1"));
    }

    [Test]
    public async Task ResolveScriptPath_WhenScriptIsMissing_UsesRepoCleanupScript()
    {
        var options = new PlaywrightCleanupOptions(@"C:\repo", Kill: false, ScriptPath: null);

        var path = PlaywrightCleanupCommand.ResolveScriptPath(options);

        await Assert.That(path).IsEqualTo(Path.GetFullPath(@"C:\repo\scripts\Kill-OrphanMcpProcesses.ps1"));
    }
}
