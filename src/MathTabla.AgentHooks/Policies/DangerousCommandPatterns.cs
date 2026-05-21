using System.Text.RegularExpressions;

namespace MathTabla.AgentHooks.Policies;

internal static partial class DangerousCommandPatterns
{
    private static readonly string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static bool IsRmRf(string command) => RmRfRegex().IsMatch(command);

    public static bool IsPowerShellForcedRecursiveDelete(string command) =>
        PowerShellForcedRecursiveDeleteRegex().IsMatch(command);

    public static bool IsDropTable(string command) => DropTableRegex().IsMatch(command);

    public static bool IsDestructiveFileCommand(string command) =>
        DestructiveFileCommandRegex().IsMatch(command);

    public static bool TargetsGitInternals(string command) =>
        command.Contains(".git", StringComparison.OrdinalIgnoreCase);

    public static bool TargetsProtectedRoot(string command)
    {
        var protectedTargets = new[]
        {
            UserProfile,
            "%USERPROFILE%",
            "$env:USERPROFILE",
            "~",
            @"C:\",
            @"C:\Windows",
            @"C:\Windows\System32",
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        return protectedTargets
            .Where(target => !string.IsNullOrWhiteSpace(target))
            .Any(target => ContainsPathTarget(command, target));
    }

    private static bool ContainsPathTarget(string command, string target)
    {
        var escaped = Regex.Escape(target.TrimEnd('\\', '/'));
        var pattern = $@"(^|[\s'""]){escaped}([\\/]?)([\s'""]|$)";
        return Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    [GeneratedRegex(@"\brm\s+.*-[A-Za-z]*r[A-Za-z]*f[A-Za-z]*\b|\brm\s+.*-[A-Za-z]*f[A-Za-z]*r[A-Za-z]*\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RmRfRegex();

    [GeneratedRegex(@"\b(remove-item|rm|del|erase)\b(?=.*\s-(recurse|r)\b)(?=.*\s-(force|f)\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PowerShellForcedRecursiveDeleteRegex();

    [GeneratedRegex(@"\bdrop\s+table\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DropTableRegex();

    [GeneratedRegex(@"\b(rm|remove-item|del|erase|rmdir|rd)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DestructiveFileCommandRegex();
}
