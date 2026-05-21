# Hook Paradigm

The hook system uses a single policy executable with small agent-specific adapters.

## Design goals

- Keep policy logic deterministic and fast.
- Normalize agent payload differences inside C#.
- Keep adapter files thin and easy to replace.
- Prefer explicit exit codes over parsing stdout.

## Executable contract

Agents call:

```powershell
dotnet run --project C:\Users\amado\code\mathtabla-hooks\src\MathTabla.AgentHooks -- pre-tool-policy
```

The hook receives a JSON payload on stdin. It writes block reasons to stderr.

Exit codes:

- `0`: allow
- `1`: invalid hook invocation
- `2`: block

## Why centralize hooks

Centralizing hook logic avoids copying policy rules across VS Code, Codex, Claude Code, and future agents. Each agent only needs enough configuration to call the shared executable.
