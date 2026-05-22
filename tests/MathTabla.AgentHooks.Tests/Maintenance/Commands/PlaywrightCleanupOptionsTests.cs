using MathTabla.AgentHooks.Maintenance.Commands;

namespace MathTabla.AgentHooks.Tests.Maintenance.Commands;

public sealed class PlaywrightCleanupOptionsTests
{
    [Test]
    public async Task TryParse_WhenNoModeIsProvided_DefaultsToDryRun()
    {
        var parsed = PlaywrightCleanupOptions.TryParse(["--repo", @"C:\repo"], out var options, out var error);

        await Assert.That(parsed).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(options.RepositoryRoot).IsEqualTo(@"C:\repo");
        await Assert.That(options.Kill).IsFalse();
        await Assert.That(options.DryRun).IsTrue();
    }

    [Test]
    public async Task TryParse_WhenKillIsProvided_DisablesDryRun()
    {
        var parsed = PlaywrightCleanupOptions.TryParse(["--repo", @"C:\repo", "--kill"], out var options, out _);

        await Assert.That(parsed).IsTrue();
        await Assert.That(options.Kill).IsTrue();
        await Assert.That(options.DryRun).IsFalse();
    }

    [Test]
    public async Task TryParse_WhenScriptIsProvided_StoresScriptPath()
    {
        var parsed = PlaywrightCleanupOptions.TryParse(
            ["--repo", @"C:\repo", "--script", @"C:\cleanup.ps1"],
            out var options,
            out _);

        await Assert.That(parsed).IsTrue();
        await Assert.That(options.ScriptPath).IsEqualTo(@"C:\cleanup.ps1");
    }

    [Test]
    public async Task TryParse_WhenArgumentIsUnknown_Fails()
    {
        var parsed = PlaywrightCleanupOptions.TryParse(["--repo", @"C:\repo", "--wat"], out _, out var error);

        await Assert.That(parsed).IsFalse();
        await Assert.That(error).Contains("--wat");
    }
}
