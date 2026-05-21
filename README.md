# MathTabla Agent Hooks

Centralized AI agent hooks for MathTabla development workflows.

## What hooks are

Hooks are small programs that run at important moments in an AI agent workflow. A pre-tool hook can inspect a pending shell command before the agent runs it. Post-tool and stop hooks can review output, add context, or block unsafe follow-up work.

## Why this project exists

VS Code/Copilot, Codex, and Claude Code can expose hook points in different shapes. This project keeps the policy logic in one typed C# console app and lets each agent use a small adapter or configuration file that calls the same executable.

The first hook is intentionally small: it reads a JSON payload from stdin, normalizes common agent field names, detects shell commands, and blocks obviously dangerous operations.

The core policy is agent and model agnostic. Host-specific differences are handled at the edge with `--host`, so the same C# rules can be reused from Claude Code, GitHub Copilot, Codex, or another agent that can call a command hook.

Internally, payloads are normalized into a host-neutral `HookRequest`, evaluated by deterministic policy classes, then adapted back to the calling host contract by the response writer.

## Portfolio Purpose

This project demonstrates C#, .NET CLI tooling, JSON processing, agent workflow automation, security guardrails, and developer tooling design.

## Project layout

```text
mathtabla-hooks/
  src/
    MathTabla.AgentHooks/
      MathTabla.AgentHooks.csproj
      Program.cs
      Domain/
      Normalization/
      Policies/
      Adapters/
  tests/
    MathTabla.AgentHooks.Tests/
      MathTabla.AgentHooks.Tests.csproj
  docs/
    hook-paradigm.md
    supported-agents.md
  examples/
    vscode/
      hooks.json
    codex/
      hooks.json
    claude/
      settings.json
```

## Requirements

- .NET 10 SDK
- Git
- An agent or editor that can call a hook executable and pass JSON on stdin

This repository includes `global.json` so local `dotnet` commands use the installed .NET 10 SDK.

## Command modes

Current command:

```powershell
dotnet run --project src/MathTabla.AgentHooks -- pre-tool-policy --host generic
```

Planned extension points include:

- `session-context`
- `post-tool-review`
- `stop-check`

## Knowledge discovery

The `kb-discover` command searches MathTabla knowledge indexes and returns curated JSON context split into research, implementation, and related notes.

```powershell
dotnet run --project src/MathTabla.AgentHooks -- kb-discover `
  --root C:\Users\amado\code\MathTabla `
  --query "mobile drag and drop" `
  --depth 2 `
  --max-results 10
```

Inputs:

- `--root`: repository root containing `static/knowledge/metadata.json`, or the knowledge folder itself
- `--query`: user intent or search terms
- `--depth`: graph neighbor traversal depth, default `2`
- `--max-results`: max results per group, default `10`

Output groups:

- `research`: notes under `/knowledge/sources/`
- `implementation`: notes under `/knowledge/docs/` or docs-style display paths
- `related`: additional curated or graph-neighbor notes
- `recommendedNextSearches`: high-signal tags and keywords for follow-up searches

## Pre-tool policy

The `pre-tool-policy` command reads JSON from stdin and checks these common fields:

- `hook_event_name` or `hookEventName`
- `tool_name` or `toolName`
- `tool_input.command` or `toolInput.command`
- `toolArgs.command` when `toolArgs` is an object
- `toolArgs` or `tool_args` when it is a JSON string containing `command`

Supported hosts:

- `generic`: portable default, blocks with stderr and exit code `2`
- `claude`: Claude Code style, blocks with stderr and exit code `2`
- `copilot`: GitHub Copilot style, blocks with stdout JSON and exit code `0`
- `codex`: Codex-style adapter, currently treated like the generic/Claude blocking contract

Generic, Claude, and Codex modes exit with:

- `0` when the command is allowed
- `1` when the hook invocation is invalid
- `2` when the command is blocked

Copilot mode writes `{"permissionDecision":"deny","permissionDecisionReason":"..."}` to stdout and exits `0`, matching Copilot `preToolUse` decision control.

Blocked examples include:

- `rm -rf`
- PowerShell `Remove-Item` with both `-Recurse` and `-Force`
- SQL `DROP TABLE`
- destructive commands targeting `.git`, a user profile root, or system folders

## Manual test

Safe command:

```powershell
'{"hook_event_name":"PreToolUse","tool_name":"shell","tool_input":{"command":"dotnet build"}}' |
  dotnet run --project src/MathTabla.AgentHooks -- pre-tool-policy --host claude
$LASTEXITCODE
```

Dangerous command:

```powershell
'{"hookEventName":"PreToolUse","toolName":"shell","toolInput":{"command":"rm -rf .git"}}' |
  dotnet run --project src/MathTabla.AgentHooks -- pre-tool-policy --host claude
$LASTEXITCODE
```

The dangerous example writes the block reason to stderr and exits `2`.

Copilot-style input:

```powershell
'{"hookEventName":"preToolUse","toolName":"bash","toolArgs":"{\"command\":\"rm -rf .git\"}"}' |
  dotnet run --project src/MathTabla.AgentHooks -- pre-tool-policy --host copilot
$LASTEXITCODE
```

The Copilot example writes a `permissionDecision` response to stdout and exits `0`.

## Agent wiring

All examples call the same C# project:

```powershell
dotnet run --project C:\Users\amado\code\mathtabla-hooks\src\MathTabla.AgentHooks -- pre-tool-policy --host claude
```

See:

- `examples/vscode/hooks.json`
- `examples/copilot/hooks.json`
- `examples/codex/hooks.json`
- `examples/claude/settings.json`
- `docs/tunit-testing-guide.md`

Treat these as adapter examples. Exact hook configuration keys can differ by agent version, but the executable contract should stay stable: JSON in through stdin, exit code out.
