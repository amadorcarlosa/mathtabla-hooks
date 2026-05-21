# TUnit Testing Guide

## Goal

Use TUnit as the test framework for `MathTabla.AgentHooks`.

TUnit is a good fit for this project because the hook app is small, deterministic, and policy-heavy. The tests should prove that normalization, policy decisions, and host-specific response behavior stay stable as we add support for more agents.

Official docs:

- TUnit: https://tunit.dev
- TUnit installation: https://tunit.dev/docs/getting-started/installation/
- TUnit first test: https://tunit.dev/docs/getting-started/writing-your-first-test/
- TUnit troubleshooting: https://tunit.dev/docs/troubleshooting/
- Microsoft test platforms overview: https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview

## Target test structure

```text
tests/
  MathTabla.AgentHooks.Tests/
    MathTabla.AgentHooks.Tests.csproj

    Normalization/
      HookRequestNormalizerTests.cs

    Policies/
      PreToolCommandPolicyTests.cs

    Adapters/
      HookHostOptionsTests.cs
      HookResponseWriterTests.cs
```

Add the test project to the root solution:

```powershell
dotnet sln MathTabla.AgentHooks.slnx add tests\MathTabla.AgentHooks.Tests\MathTabla.AgentHooks.Tests.csproj
```

## Project setup

Create the test project as a console app because TUnit test projects are executable Microsoft.Testing.Platform apps:

```powershell
dotnet new console --framework net10.0 --name MathTabla.AgentHooks.Tests --output tests\MathTabla.AgentHooks.Tests
dotnet add tests\MathTabla.AgentHooks.Tests package TUnit
dotnet add tests\MathTabla.AgentHooks.Tests reference src\MathTabla.AgentHooks\MathTabla.AgentHooks.csproj
```

Remove the generated `Program.cs`; TUnit supplies the test application entry point.

Recommended `MathTabla.AgentHooks.Tests.csproj` shape:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\MathTabla.AgentHooks\MathTabla.AgentHooks.csproj" />
  </ItemGroup>
</Project>
```

Do not add `Microsoft.NET.Test.Sdk`, `coverlet.collector`, or `coverlet.msbuild`. TUnit uses Microsoft.Testing.Platform, and its meta package includes compatible coverage/reporting extensions.

## Accessibility requirement for tests

The production app currently keeps most types `internal`. The preferred approach is to expose internals only to the test project.

Add this to the app project:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="MathTabla.AgentHooks.Tests" />
</ItemGroup>
```

If SDK support for that item is not available in this project shape, use an assembly attribute file instead:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MathTabla.AgentHooks.Tests")]
```

## Test style

TUnit tests should be plain public instance methods with `[Test]`.

Assertions must be awaited:

```csharp
namespace MathTabla.AgentHooks.Tests.Policies;

public sealed class PreToolCommandPolicyTests
{
    [Test]
    public async Task Evaluate_WhenCommandIsSafe_AllowsCommand()
    {
        var request = new HookRequest(
            EventName: HookEvents.PreToolUse,
            ToolName: HookToolNames.Shell,
            Command: "dotnet build",
            WorkingDirectory: null,
            SessionId: null);

        var decision = PreToolCommandPolicy.Evaluate(request);

        await Assert.That(decision.Allowed).IsTrue();
    }
}
```

The TUnit package provides global usings for common TUnit namespaces, so explicit `using TUnit.Core;`, `using TUnit.Assertions;`, and `using TUnit.Assertions.Extensions;` are optional.

## Priority tests

### Normalization

Cover every supported agent payload shape:

- `hook_event_name`
- `hookEventName`
- `tool_name`
- `toolName`
- `tool_input.command`
- `toolInput.command`
- object `toolArgs.command`
- JSON-string `toolArgs`
- JSON-string `tool_args`
- root `command`

Example cases:

```csharp
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
```

```csharp
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
```

### Policy

Cover allow/block decisions:

- allow `dotnet build`
- block `rm -rf .git`
- block `Remove-Item .git -Recurse -Force`
- block `DROP TABLE Students`
- block destructive command targeting `.git`
- block destructive command targeting `C:\Windows`
- allow non-destructive command mentioning `.git`, such as `git status`

Example:

```csharp
[Test]
public async Task Evaluate_WhenCommandDropsTable_BlocksCommand()
{
    var request = new HookRequest(
        EventName: HookEvents.PreToolUse,
        ToolName: HookToolNames.Shell,
        Command: "psql -c \"DROP TABLE Students\"",
        WorkingDirectory: null,
        SessionId: null);

    var decision = PreToolCommandPolicy.Evaluate(request);

    await Assert.That(decision.Allowed).IsFalse();
    await Assert.That(decision.Reason).Contains("DROP TABLE");
}
```

### Host options

Cover:

- no `--host` means `generic`
- `--host claude`
- `--host copilot`
- `--host codex`
- unknown host falls back to `generic`
- `--host` without a value falls back to `generic`

### Response writer

Response writer tests may need to capture `Console.Out` and `Console.Error`.

Cover:

- Claude block writes reason to stderr and returns `2`
- Generic block writes reason to stderr and returns `2`
- Codex block writes reason to stderr and returns `2`
- Copilot allow writes `{}` to stdout and returns `0`
- Copilot block writes `permissionDecision: deny` JSON to stdout and returns `0`

## Running tests

Run all tests:

```powershell
dotnet test
```

Run the test project directly:

```powershell
dotnet run --project tests\MathTabla.AgentHooks.Tests
```

Run with coverage:

```powershell
dotnet run --project tests\MathTabla.AgentHooks.Tests --configuration Release --coverage
```

Run with TRX reporting:

```powershell
dotnet run --project tests\MathTabla.AgentHooks.Tests --configuration Release --report-trx
```

## VS Code setup

For VS Code test discovery:

1. Install C# Dev Kit.
2. Open C# Dev Kit settings.
3. Enable `Dotnet > Test Window > Use Testing Platform Protocol`.
4. Reload VS Code.

## Acceptance criteria

- Test project targets `net10.0`.
- Test project references the app project.
- Test project uses `TUnit`.
- Test project does not reference `Microsoft.NET.Test.Sdk`.
- `dotnet build /nr:false` succeeds.
- `dotnet test` succeeds.
- Tests cover normalization, dangerous command policy, host option parsing, and host response writing.
- The command-line hook smoke tests in `docs/agent-agnostic-domain-implementation.md` still pass.
