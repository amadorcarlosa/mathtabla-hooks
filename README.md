# MathTabla Agent Hooks

Centralized AI agent hooks for MathTabla development workflows.

## What hooks are

Hooks are small programs that run at important moments in an AI agent workflow. A pre-tool hook can inspect a pending shell command before the agent runs it. Post-tool and stop hooks can review output, add context, or block unsafe follow-up work.

## Why this project exists

VS Code/Copilot, Codex, and Claude Code can expose hook points in different shapes. This project keeps the policy logic in one typed C# console app and lets each agent use a small adapter or configuration file that calls the same executable.

The first hook is intentionally small: it reads a JSON payload from stdin, normalizes common agent field names, detects shell commands, and blocks obviously dangerous operations.

## Portfolio Purpose

This project demonstrates C#, .NET CLI tooling, JSON processing, agent workflow automation, security guardrails, and developer tooling design.

## Project layout

```text
mathtabla-hooks/
  src/
    MathTabla.AgentHooks/
      MathTabla.AgentHooks.csproj
      Program.cs
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
dotnet run --project src/MathTabla.AgentHooks -- pre-tool-policy
```

Planned extension points include:

- `session-context`
- `post-tool-review`
- `stop-check`

## Pre-tool policy

The `pre-tool-policy` command reads JSON from stdin and checks these common fields:

- `hook_event_name` or `hookEventName`
- `tool_name` or `toolName`
- `tool_input.command` or `toolInput.command`

It exits with:

- `0` when the command is allowed
- `1` when the hook invocation is invalid
- `2` when the command is blocked

Blocked examples include:

- `rm -rf`
- PowerShell `Remove-Item` with both `-Recurse` and `-Force`
- SQL `DROP TABLE`
- destructive commands targeting `.git`, a user profile root, or system folders

## Manual test

Safe command:

```powershell
'{"hook_event_name":"PreToolUse","tool_name":"shell","tool_input":{"command":"dotnet build"}}' |
  dotnet run --project src/MathTabla.AgentHooks -- pre-tool-policy
$LASTEXITCODE
```

Dangerous command:

```powershell
'{"hookEventName":"PreToolUse","toolName":"shell","toolInput":{"command":"rm -rf .git"}}' |
  dotnet run --project src/MathTabla.AgentHooks -- pre-tool-policy
$LASTEXITCODE
```

The dangerous example writes the block reason to stderr and exits `2`.

## Agent wiring

All examples call the same C# project:

```powershell
dotnet run --project C:\Users\amado\code\mathtabla-hooks\src\MathTabla.AgentHooks -- pre-tool-policy
```

See:

- `examples/vscode/hooks.json`
- `examples/codex/hooks.json`
- `examples/claude/settings.json`

Treat these as adapter examples. Exact hook configuration keys can differ by agent version, but the executable contract should stay stable: JSON in through stdin, exit code out.
