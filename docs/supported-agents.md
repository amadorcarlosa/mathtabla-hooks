# Supported Agents

This project is designed around a shared executable and thin agent adapters.

## VS Code / Copilot

Use `examples/vscode/hooks.json` or `examples/copilot/hooks.json` as a template for editor, CLI, or cloud-agent hook wiring that can invoke local commands.

GitHub Copilot hooks use `preToolUse` and should block by returning stdout JSON with `permissionDecision: "deny"` and a reason. Use `--host copilot`.

## Codex

Use `examples/codex/hooks.json` as a template for Codex hook wiring. Codex hook support has evolved quickly, so keep this adapter separate from the reusable policy. Use `--host codex`.

## Claude Code

Use `examples/claude/settings.json` as a template for Claude Code-style hook settings. Claude Code blocks `PreToolUse` by reading stderr and receiving exit code `2`. Use `--host claude`.

## Payload normalization

The C# app currently checks both snake_case and camelCase payloads:

- `hook_event_name` / `hookEventName`
- `tool_name` / `toolName`
- `tool_input.command` / `toolInput.command`
- `toolArgs.command`
- JSON-string `toolArgs` / `tool_args` containing `command`

The goal is agent and model agnostic policy: normalize the request once, evaluate deterministic C# rules once, and translate the result for the calling host at the edge.
