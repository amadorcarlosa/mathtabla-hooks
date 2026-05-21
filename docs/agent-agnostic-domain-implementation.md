# Agent-Agnostic Hook Domain Implementation

## Goal

Refactor `MathTabla.AgentHooks` so reusable hook policy lives in an agent-agnostic domain layer, while Claude Code, GitHub Copilot, Codex, and future agents are handled by thin input/output adapters.

The desired flow is:

```text
Agent-specific JSON payload
  -> normalize into one internal hook request model
  -> evaluate deterministic C# policy
  -> adapt the decision to the calling host contract
```

This should keep the project small, readable, and portfolio-ready.

## Current state

Most logic currently lives in `src/MathTabla.AgentHooks/Program.cs`.

It already supports:

- `pre-tool-policy`
- `--host generic|claude|copilot|codex`
- common payload shapes:
  - `tool_input.command`
  - `toolInput.command`
  - `toolArgs.command`
  - JSON-string `toolArgs`
- host-specific blocking:
  - Claude/generic/Codex: stderr plus exit code `2`
  - Copilot: stdout `permissionDecision` JSON plus exit code `0`

The next step is to split this into named modules without changing behavior.

## Target structure

```text
src/
  MathTabla.AgentHooks/
    Program.cs

    Domain/
      HookCommandNames.cs
      HookDecision.cs
      HookEvents.cs
      HookExitCodes.cs
      HookHosts.cs
      HookRequest.cs
      HookToolNames.cs

    Normalization/
      HookRequestNormalizer.cs
      JsonHookReader.cs

    Policies/
      DangerousCommandPatterns.cs
      PreToolCommandPolicy.cs

    Adapters/
      HookHostOptions.cs
      HookResponseWriter.cs
```

## Responsibilities

### `Domain/`

Contains reusable concepts that do not belong to any one agent.

`HookCommandNames.cs`

- Static class for executable command modes.
- Include:
  - `PreToolPolicy`
  - `SessionContext`
  - `PostToolReview`
  - `StopCheck`

`HookEvents.cs`

- Static class for canonical and known host event names.
- Include at least:
  - `PreToolUse`
  - `preToolUse`
  - `PostToolUse`
  - `postToolUse`
  - `Stop`
  - `agentStop`
  - `SessionStart`
  - `sessionStart`

`HookHosts.cs`

- Static class for supported host adapters.
- Include:
  - `generic`
  - `claude`
  - `copilot`
  - `codex`

`HookExitCodes.cs`

- Static class for process exit codes.
- Include:
  - `Allow = 0`
  - `InvalidInvocation = 1`
  - `Block = 2`

`HookToolNames.cs`

- Static class for common tool names across hosts.
- Include common shell names:
  - `bash`
  - `shell`
  - `powershell`
  - `Bash`

`HookRequest.cs`

- Internal normalized request model.
- Suggested shape:

```csharp
internal sealed record HookRequest(
    string? EventName,
    string? ToolName,
    string? Command,
    string? WorkingDirectory,
    string? SessionId);
```

`HookDecision.cs`

- Internal policy result model.
- Suggested shape:

```csharp
internal sealed record HookDecision(bool Allowed, string? Reason)
{
    public static HookDecision Allow() => new(true, null);
    public static HookDecision Block(string reason) => new(false, reason);
}
```

### `Normalization/`

Contains JSON parsing and host-shape normalization.

`JsonHookReader.cs`

- Move low-level `JsonElement` helpers here.
- Keep parsing defensive.
- Do not throw for missing optional fields.
- Support:
  - direct string properties
  - nested string properties
  - JSON-string object properties like Copilot `toolArgs`

`HookRequestNormalizer.cs`

- Converts raw stdin JSON into `HookRequest`.
- Support these aliases:
  - event: `hook_event_name`, `hookEventName`
  - session: `session_id`, `sessionId`
  - cwd: `cwd`
  - tool: `tool_name`, `toolName`
  - command:
    - `tool_input.command`
    - `toolInput.command`
    - `toolArgs.command`
    - JSON-string `toolArgs`
    - JSON-string `tool_args`
    - root `command`

### `Policies/`

Contains deterministic policy rules only.

`PreToolCommandPolicy.cs`

- Evaluates `HookRequest.Command`.
- Returns `HookDecision`.
- Should not know about Claude, Copilot, Codex, stdout, stderr, or process exit behavior.

`DangerousCommandPatterns.cs`

- Own regexes and helper methods for dangerous command detection.
- Keep rules small and understandable.
- Cover current rules:
  - `rm -rf`
  - `Remove-Item` with `-Recurse` and `-Force`
  - SQL `DROP TABLE`
  - destructive commands targeting `.git`
  - destructive commands targeting user profile root or system folders

### `Adapters/`

Contains host-specific behavior at the process boundary.

`HookHostOptions.cs`

- Parses `--host`.
- Defaults to `generic`.
- Unknown host should fall back to `generic`.

`HookResponseWriter.cs`

- Converts `HookDecision` into host-specific process output.
- Required behavior:
  - `generic`, `claude`, `codex`
    - allow: exit `0`
    - block: write reason to stderr, exit `2`
  - `copilot`
    - allow: write `{}` to stdout, exit `0`
    - block: write this JSON to stdout, exit `0`:

```json
{
  "permissionDecision": "deny",
  "permissionDecisionReason": "reason"
}
```

## Program flow

`Program.cs` should become mostly orchestration:

1. Parse the command name.
2. Parse `--host`.
3. Read stdin.
4. Normalize JSON into `HookRequest`.
5. Evaluate `PreToolCommandPolicy`.
6. Write response through `HookResponseWriter`.

Keep `Program.cs` small enough that a reader can understand the executable flow quickly.

## Refactor constraints

- Do not change existing observable behavior.
- Do not add NuGet dependencies.
- Keep all new code in the existing `MathTabla.AgentHooks` project.
- Keep types `internal` unless there is a clear reason for `public`.
- Prefer static classes for constants.
- Prefer records for simple immutable domain models.
- Keep comments sparse and useful.
- Keep the first version intentionally small.

## Verification commands

Run from repository root.

Before adding or changing tests, follow `docs/tunit-testing-guide.md`.

Build:

```powershell
dotnet build /nr:false
```

Claude-style safe command:

```powershell
'{"hook_event_name":"PreToolUse","tool_name":"shell","tool_input":{"command":"dotnet build"}}' |
  dotnet run --no-build --project src\MathTabla.AgentHooks -- pre-tool-policy --host claude
$LASTEXITCODE
```

Expected exit code: `0`.

Claude-style dangerous command:

```powershell
'{"hookEventName":"PreToolUse","toolName":"shell","toolInput":{"command":"rm -rf .git"}}' |
  dotnet run --no-build --project src\MathTabla.AgentHooks -- pre-tool-policy --host claude
$LASTEXITCODE
```

Expected exit code: `2`.

Copilot-style dangerous command:

```powershell
'{"hookEventName":"preToolUse","toolName":"bash","toolArgs":"{\"command\":\"rm -rf .git\"}"}' |
  dotnet run --no-build --project src\MathTabla.AgentHooks -- pre-tool-policy --host copilot
$LASTEXITCODE
```

Expected stdout:

```json
{"permissionDecision":"deny","permissionDecisionReason":"Blocked command: recursive forced deletion with rm -rf is not allowed."}
```

Expected exit code: `0`.

Copilot-style safe command:

```powershell
'{"hookEventName":"preToolUse","toolName":"bash","toolArgs":"{\"command\":\"dotnet build\"}"}' |
  dotnet run --no-build --project src\MathTabla.AgentHooks -- pre-tool-policy --host copilot
$LASTEXITCODE
```

Expected stdout:

```json
{}
```

Expected exit code: `0`.

## Documentation updates

After the refactor:

- Update `README.md` if file paths or command examples change.
- Update `docs/hook-paradigm.md` to mention the new domain/adapters structure.
- Update `docs/supported-agents.md` only if host behavior changes.

## Acceptance criteria

- `Program.cs` no longer contains policy regexes or JSON helper methods.
- Domain classes are host-neutral.
- Policy evaluation does not write to stdout/stderr and does not return process exit codes.
- Host-specific output is isolated in `Adapters/HookResponseWriter.cs`.
- Existing examples still call the same executable command.
- `dotnet build /nr:false` succeeds.
- All four verification commands above produce the expected results.
