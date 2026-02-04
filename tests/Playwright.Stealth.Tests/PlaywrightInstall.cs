using System;
using System.Threading.Tasks;

namespace ManagedCode.Playwright.Stealth.Tests;

internal static class PlaywrightInstall
{
    private static readonly object InstallLock = new();
    private static Task? installTask;

    public static Task EnsureInstalledAsync()
    {
        lock (InstallLock)
        {
            installTask ??= Task.Run(() =>
            {
                var args = GetInstallArgs();
                var exitCode = Microsoft.Playwright.Program.Main(args);
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Playwright install failed with code {exitCode}.");
                }
            });

            return installTask;
        }
    }

    private static string[] GetInstallArgs()
    {
        var installDeps = ShouldInstallDeps();
        return installDeps
            ? ["install", "--with-deps", "chromium"]
            : ["install", "chromium"];
    }

    private static bool ShouldInstallDeps()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        var installDeps = Environment.GetEnvironmentVariable("PLAYWRIGHT_INSTALL_DEPS");
        if (string.Equals(installDeps, "1", StringComparison.Ordinal))
        {
            return true;
        }

        var ci = Environment.GetEnvironmentVariable("CI");
        return string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ci, "1", StringComparison.Ordinal);
    }
}
