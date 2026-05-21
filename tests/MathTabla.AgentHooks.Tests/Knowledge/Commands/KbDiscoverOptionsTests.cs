using MathTabla.AgentHooks.Knowledge.Commands;

namespace MathTabla.AgentHooks.Tests.Knowledge.Commands;

public sealed class KbDiscoverOptionsTests
{
    [Test]
    public async Task TryParse_WhenQueryIsProvided_ReturnsOptions()
    {
        var parsed = KbDiscoverOptions.TryParse(
            ["--root", @"C:\repo", "--query", "mobile drag drop", "--depth", "3", "--max-results", "5"],
            out var options,
            out var error);

        await Assert.That(parsed).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(options.Root).IsEqualTo(@"C:\repo");
        await Assert.That(options.Query).IsEqualTo("mobile drag drop");
        await Assert.That(options.Depth).IsEqualTo(3);
        await Assert.That(options.MaxResults).IsEqualTo(5);
    }

    [Test]
    public async Task TryParse_WhenQueryIsMissing_Fails()
    {
        var parsed = KbDiscoverOptions.TryParse(["--root", @"C:\repo"], out _, out var error);

        await Assert.That(parsed).IsFalse();
        await Assert.That(error).Contains("--query");
    }

    [Test]
    public async Task TryParse_WhenNumbersAreOutOfRange_ClampsThem()
    {
        var parsed = KbDiscoverOptions.TryParse(
            ["--query", "drag", "--depth", "20", "--max-results", "200"],
            out var options,
            out _);

        await Assert.That(parsed).IsTrue();
        await Assert.That(options.Depth).IsEqualTo(5);
        await Assert.That(options.MaxResults).IsEqualTo(50);
    }
}
