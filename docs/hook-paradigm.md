# Hook Paradigm

The hook system uses a single policy executable with small agent-specific adapters.

## Design goals

- Keep policy logic deterministic and fast.
- Normalize agent payload differences inside C#.
- Keep adapter files thin and easy to replace.
- Prefer explicit exit codes over parsing stdout.
- Keep the policy model independent from any specific agent or model provider.

## Executable contract

Agents call:

```powershell
dotnet run --project C:\Users\amado\code\mathtabla-hooks\src\MathTabla.AgentHooks -- pre-tool-policy --host claude
```

The hook receives a JSON payload on stdin. It writes block reasons to stderr.

Generic, Claude, and Codex-style exit codes:

- `0`: allow
- `1`: invalid hook invocation
- `2`: block

GitHub Copilot `preToolUse` uses stdout decision JSON instead of exit code `2` for blocking:

```json
{
  "permissionDecision": "deny",
  "permissionDecisionReason": "Blocked command: ..."
}
```

## Why centralize hooks

Centralizing hook logic avoids copying policy rules across VS Code, Codex, Claude Code, and future agents. Each agent only needs enough configuration to call the shared executable.

## Adapter boundary

The reusable C# policy should answer one question: should this normalized tool request be allowed? Host adapters then translate that decision into the contract expected by the caller.

- Claude Code: block with stderr and exit code `2`.
- GitHub Copilot: block with stdout JSON and exit code `0`.
- Codex: keep isolated behind `--host codex` while its hook surface continues to evolve.
- Generic agents: use the simple stderr plus exit code `2` contract.
