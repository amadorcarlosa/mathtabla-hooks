using MathTabla.AgentHooks.Adapters;
using MathTabla.AgentHooks.Domain;

namespace MathTabla.AgentHooks.Tests.Adapters;

public sealed class HookHostOptionsTests
{
    [Test]
    public async Task Parse_WhenHostIsMissing_DefaultsToGeneric()
    {
        var host = HookHostOptions.Parse([]);

        await Assert.That(host).IsEqualTo(HookHosts.Generic);
    }

    [Test]
    [Arguments("claude", HookHosts.Claude)]
    [Arguments("copilot", HookHosts.Copilot)]
    [Arguments("codex", HookHosts.Codex)]
    public async Task Parse_WhenHostIsKnown_ReturnsHost(string value, string expected)
    {
        var host = HookHostOptions.Parse(["--host", value]);

        await Assert.That(host).IsEqualTo(expected);
    }

    [Test]
    [Arguments("unknown")]
    [Arguments("")]
    public async Task Parse_WhenHostIsUnknown_FallsBackToGeneric(string value)
    {
        var host = HookHostOptions.Parse(["--host", value]);

        await Assert.That(host).IsEqualTo(HookHosts.Generic);
    }

    [Test]
    public async Task Parse_WhenHostHasNoValue_FallsBackToGeneric()
    {
        var host = HookHostOptions.Parse(["--host"]);

        await Assert.That(host).IsEqualTo(HookHosts.Generic);
    }
}
