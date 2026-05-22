using System.Diagnostics;
using MathTabla.AgentHooks.Domain;

namespace MathTabla.AgentHooks.Maintenance.Commands;

internal static class PlaywrightCleanupCommand
{
    public static async Task<int> RunAsync(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (!OperatingSystem.IsWindows())
        {
            stderr.WriteLine("playwright-cleanup currently supports Windows only.");
            return HookExitCodes.InvalidInvocation;
        }

        if (!PlaywrightCleanupOptions.TryParse(args, out var options, out var error))
        {
            stderr.WriteLine(error);
            return HookExitCodes.InvalidInvocation;
        }

        var scriptPath = ResolveScriptPath(options);
        if (!File.Exists(scriptPath))
        {
            stderr.WriteLine($"Cleanup script was not found: {scriptPath}");
            return HookExitCodes.InvalidInvocation;
        }

        var result = await PowerShellRunner.RunAsync(
            scriptPath,
            options.DryRun ? ["-DryRun"] : [],
            stdout,
            stderr);

        return result == 0 ? HookExitCodes.Allow : HookExitCodes.InvalidInvocation;
    }

    internal static string ResolveScriptPath(PlaywrightCleanupOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ScriptPath))
        {
            return Path.GetFullPath(options.ScriptPath);
        }

        return Path.Combine(
            Path.GetFullPath(options.RepositoryRoot),
            "scripts",
            "Kill-OrphanMcpProcesses.ps1");
    }
}

internal static class PowerShellRunner
{
    public static async Task<int> RunAsync(
        string scriptPath,
        IReadOnlyList<string> scriptArguments,
        TextWriter stdout,
        TextWriter stderr)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);

        foreach (var argument in scriptArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            stderr.WriteLine("Failed to start PowerShell cleanup process.");
            return 1;
        }

        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            await stdout.WriteAsync(standardOutput);
        }

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            await stderr.WriteAsync(standardError);
        }

        return process.ExitCode;
    }
}
