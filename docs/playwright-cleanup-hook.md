# Playwright Cleanup Hook

## Problem

On Windows, Playwright MCP and browser automation processes can survive after the agent or tool that launched them exits. These leftover processes consume memory and may keep browser automation state alive longer than intended.

MathTabla already has targeted cleanup logic in:

```text
C:\Users\amado\code\MathTabla\scripts\Kill-OrphanMcpProcesses.ps1
```

`MathTabla.AgentHooks` wraps that script through `playwright-cleanup` so Claude Code, Codex, GitHub Copilot, or a manual terminal command can call the same cleanup behavior.

## Command

Dry run:

```powershell
dotnet run --project C:\Users\amado\code\mathtabla-hooks\src\MathTabla.AgentHooks -- playwright-cleanup `
  --repo C:\Users\amado\code\MathTabla `
  --dry-run
```

Cleanup:

```powershell
dotnet run --project C:\Users\amado\code\mathtabla-hooks\src\MathTabla.AgentHooks -- playwright-cleanup `
  --repo C:\Users\amado\code\MathTabla `
  --kill
```

## Safety boundary

The command does not kill all browser or Node processes.

It delegates to the existing MathTabla script, which targets known MCP offenders such as:

- `@playwright/mcp`
- `playwright-mcp`
- `playwright test-server`
- `aspire mcp start`

This matters because normal `msedgewebview2.exe` processes may belong to Outlook, LinkedIn, Widgets, WhatsApp, SearchHost, VS Code, or other applications.

## Hook usage

Use this as a post-tool cleanup hook after browser or Playwright activity. For aggressive cleanup, use `--kill`. For observability-only mode, use `--dry-run`.

The safest rollout is:

1. Start with `--dry-run`.
2. Confirm the reported processes are Playwright MCP or other known automation leftovers.
3. Switch the post-tool hook to `--kill`.
