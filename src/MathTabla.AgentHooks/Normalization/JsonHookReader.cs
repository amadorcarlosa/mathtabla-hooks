using System.Text.Json;

namespace MathTabla.AgentHooks.Normalization;

internal static class JsonHookReader
{
    public static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    public static string? GetNestedString(JsonElement element, IReadOnlyList<string> path)
    {
        var current = element;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(segment, out current))
            {
                return null;
            }
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    public static string? GetCommandFromJsonString(JsonElement element, string propertyName)
    {
        var json = GetString(element, propertyName);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return GetNestedString(document.RootElement, ["command"]);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
