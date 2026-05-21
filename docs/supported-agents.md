# Supported Agents

This project is designed around a shared executable and thin agent adapters.

## VS Code / Copilot

Use `examples/vscode/hooks.json` as a template for editor or extension-level hook wiring that can invoke local commands.

## Codex

Use `examples/codex/hooks.json` as a template for Codex hook wiring. The hook expects the agent payload on stdin and uses exit code `2` to block dangerous commands.

## Claude Code

Use `examples/claude/settings.json` as a template for Claude Code-style hook settings. The command delegates to the same .NET project as the other adapters.

## Payload normalization

The C# app currently checks both snake_case and camelCase payloads:

- `hook_event_name` / `hookEventName`
- `tool_name` / `toolName`
- `tool_input.command` / `toolInput.command`
