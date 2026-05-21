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

## Internal structure

The executable flow is intentionally layered:

- `Domain/`: host-neutral hook concepts such as requests, decisions, command names, host names, tool names, events, and exit codes.
- `Normalization/`: defensive JSON readers that convert agent-specific payload shapes into one `HookRequest`.
- `Policies/`: deterministic command policy and dangerous command pattern checks.
- `Adapters/`: host option parsing and response writing for generic, Claude, Copilot, and Codex contracts.

## Adapter boundary

The reusable C# policy should answer one question: should this normalized tool request be allowed? Host adapters then translate that decision into the contract expected by the caller.

- Claude Code: block with stderr and exit code `2`.
- GitHub Copilot: block with stdout JSON and exit code `0`.
- Codex: keep isolated behind `--host codex` while its hook surface continues to evolve.
- Generic agents: use the simple stderr plus exit code `2` contract.

## Knowledge discovery command

The app also exposes deterministic knowledge-base discovery through `kb-discover`.

This command stays model agnostic. It does not write summaries or make implementation decisions. Instead, it reads `metadata.json` and `graph.json`, scores notes against a query, traverses graph neighbors, and returns structured JSON that any agent can use as curated context.

```powershell
dotnet run --project src\MathTabla.AgentHooks -- kb-discover --root C:\Users\amado\code\MathTabla --query "mobile drag and drop"
```

The intended division of labor is:

- C#: load indexes, classify sources, score metadata, traverse graph, return JSON
- Agent/model: interpret the JSON, compare research against implementation, and propose changes
