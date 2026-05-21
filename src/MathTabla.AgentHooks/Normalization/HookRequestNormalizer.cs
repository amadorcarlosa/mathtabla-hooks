using System.Text.Json;
using MathTabla.AgentHooks.Domain;

namespace MathTabla.AgentHooks.Normalization;

internal static class HookRequestNormalizer
{
    public static HookRequest FromJson(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var eventName = JsonHookReader.GetString(root, "hook_event_name", "hookEventName");
        var sessionId = JsonHookReader.GetString(root, "session_id", "sessionId");
        var workingDirectory = JsonHookReader.GetString(root, "cwd");
        var toolName = JsonHookReader.GetString(root, "tool_name", "toolName");
        var command =
            JsonHookReader.GetNestedString(root, ["tool_input", "command"]) ??
            JsonHookReader.GetNestedString(root, ["toolInput", "command"]) ??
            JsonHookReader.GetNestedString(root, ["toolArgs", "command"]) ??
            JsonHookReader.GetCommandFromJsonString(root, "toolArgs") ??
            JsonHookReader.GetCommandFromJsonString(root, "tool_args") ??
            JsonHookReader.GetString(root, "command");

        return new HookRequest(eventName, toolName, command, workingDirectory, sessionId);
    }
}
