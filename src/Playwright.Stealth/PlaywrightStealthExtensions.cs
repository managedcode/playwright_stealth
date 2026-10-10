using Microsoft.Playwright;

namespace ManagedCode.Playwright.Stealth;

public static class PlaywrightStealthExtensions
{
    /// <summary>
    /// Recommended Chrome arguments to reduce bot-detection surface.
    /// Pass these to <see cref="BrowserTypeLaunchOptions.Args"/>.
    /// </summary>
    public static readonly string[] StealthArgs =
    [
        "--disable-blink-features=AutomationControlled",
        "--disable-default-apps",
        "--no-first-run",
        "--no-default-browser-check",
        "--disable-component-update",
        "--disable-client-side-phishing-detection",
        "--disable-hang-monitor",
        "--disable-breakpad",
        "--metrics-recording-only",
        // Use the native OpenGL driver on Linux, including Mesa on CI runners.
        // Chromium otherwise falls back to the detectable SwiftShader backend.
        .. OperatingSystem.IsLinux() ? new[] { "--use-angle=gl-egl", "--ignore-gpu-blocklist" } : []
    ];

    /// <summary>
    /// Apply stealth evasion scripts to an existing page.
    /// Call <b>before</b> navigating to the target URL.
    /// </summary>
    public static async Task ApplyStealthAsync(this IPage page, StealthConfig? config = null)
    {
        var stealthConfig = config ?? new StealthConfig();
        foreach (var script in StealthScriptProvider.BuildScripts(stealthConfig))
        {
            await page.AddInitScriptAsync(script).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Apply stealth evasion scripts to a browser context.
    /// Call <b>before</b> creating pages. All pages created from this context will inherit the stealth scripts.
    /// </summary>
    public static async Task ApplyStealthAsync(this IBrowserContext context, StealthConfig? config = null)
    {
        var stealthConfig = config ?? new StealthConfig();
        foreach (var script in StealthScriptProvider.BuildScripts(stealthConfig))
        {
            await context.AddInitScriptAsync(script).ConfigureAwait(false);
        }

        await StealthContextController.ApplyAsync(context, stealthConfig).ConfigureAwait(false);
    }

    /// <summary>
    /// Launch Chromium with stealth arguments and create a context with stealth scripts applied.
    /// This is the recommended one-call setup for most use cases.
    /// </summary>
    public static async Task<(IBrowser Browser, IBrowserContext Context)> LaunchStealthAsync(
        this IBrowserType browserType,
        StealthConfig? config = null,
        BrowserTypeLaunchOptions? launchOptions = null,
        BrowserNewContextOptions? contextOptions = null)
    {
        var opts = launchOptions ?? new BrowserTypeLaunchOptions();
        if (browserType.Name == "chromium" && opts.Channel is null && opts.ExecutablePath is null)
        {
            opts.Channel = "chromium";
        }
        string[]? requestedArgs = opts.Args?.ToArray();
        bool hasRequestedGraphicsBackend = requestedArgs?.Any(argument =>
            argument.StartsWith("--use-angle=", StringComparison.Ordinal)
            || argument.StartsWith("--use-gl=", StringComparison.Ordinal)) ?? false;
        opts.Args = requestedArgs is null
            ? StealthArgs
            : [.. requestedArgs, .. StealthArgs.Where(argument =>
                !hasRequestedGraphicsBackend || !argument.StartsWith("--use-angle=", StringComparison.Ordinal))];

        var browser = await browserType.LaunchAsync(opts).ConfigureAwait(false);

        var ctxOpts = contextOptions ?? new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            DeviceScaleFactor = 2
        };

        var context = await browser.NewContextAsync(ctxOpts).ConfigureAwait(false);
        await context.ApplyStealthAsync(config).ConfigureAwait(false);

        return (browser, context);
    }
}
