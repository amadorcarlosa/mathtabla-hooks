using MathTabla.AgentHooks.Domain;
using MathTabla.AgentHooks.Normalization;

namespace MathTabla.AgentHooks.Tests.Normalization;

public sealed class HookRequestNormalizerTests
{
    [Test]
    public async Task FromJson_WhenClaudeStylePayload_ExtractsCommand()
    {
        const string json = """
        {
          "hook_event_name": "PreToolUse",
          "tool_name": "shell",
          "tool_input": {
            "command": "dotnet build"
          }
        }
        """;

        var request = HookRequestNormalizer.FromJson(json);

        await Assert.That(request.EventName).IsEqualTo(HookEvents.PreToolUse);
        await Assert.That(request.ToolName).IsEqualTo(HookToolNames.Shell);
        await Assert.That(request.Command).IsEqualTo("dotnet build");
    }

    [Test]
    public async Task FromJson_WhenCamelCaseToolInput_ExtractsCommand()
    {
        const string json = """
        {
          "hookEventName": "PreToolUse",
          "toolName": "shell",
          "toolInput": {
            "command": "git status"
          }
        }
        """;

        var request = HookRequestNormalizer.FromJson(json);

        await Assert.That(request.EventName).IsEqualTo(HookEvents.PreToolUse);
        await Assert.That(request.ToolName).IsEqualTo(HookToolNames.Shell);
        await Assert.That(request.Command).IsEqualTo("git status");
    }

    [Test]
    public async Task FromJson_WhenCopilotToolArgsIsObject_ExtractsCommand()
    {
        const string json = """
        {
          "hookEventName": "preToolUse",
          "toolName": "bash",
          "toolArgs": {
            "command": "dotnet test"
          }
        }
        """;

        var request = HookRequestNormalizer.FromJson(json);

        await Assert.That(request.EventName).IsEqualTo(HookEvents.PreToolUseCamelCase);
        await Assert.That(request.ToolName).IsEqualTo(HookToolNames.BashLower);
        await Assert.That(request.Command).IsEqualTo("dotnet test");
    }

    [Test]
    public async Task FromJson_WhenCopilotToolArgsIsJsonString_ExtractsCommand()
    {
        const string json = """
        {
          "hookEventName": "preToolUse",
          "toolName": "bash",
          "toolArgs": "{\"command\":\"rm -rf .git\"}"
        }
        """;

        var request = HookRequestNormalizer.FromJson(json);

        await Assert.That(request.EventName).IsEqualTo(HookEvents.PreToolUseCamelCase);
        await Assert.That(request.ToolName).IsEqualTo(HookToolNames.BashLower);
        await Assert.That(request.Command).IsEqualTo("rm -rf .git");
    }

    [Test]
    public async Task FromJson_WhenToolArgsSnakeCaseIsJsonString_ExtractsCommand()
    {
        const string json = """
        {
          "tool_args": "{\"command\":\"pwsh -Command Get-ChildItem\"}"
        }
        """;

        var request = HookRequestNormalizer.FromJson(json);

        await Assert.That(request.Command).IsEqualTo("pwsh -Command Get-ChildItem");
    }

    [Test]
    public async Task FromJson_WhenCommandIsRootLevel_ExtractsCommand()
    {
        const string json = """
        {
          "command": "dotnet build",
          "cwd": "C:\\repo",
          "session_id": "abc"
        }
        """;

        var request = HookRequestNormalizer.FromJson(json);

        await Assert.That(request.Command).IsEqualTo("dotnet build");
        await Assert.That(request.WorkingDirectory).IsEqualTo(@"C:\repo");
        await Assert.That(request.SessionId).IsEqualTo("abc");
    }
}
