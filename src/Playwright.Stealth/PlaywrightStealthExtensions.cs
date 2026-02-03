using Microsoft.Playwright;

namespace ManagedCode.Playwright.Stealth;

public static class PlaywrightStealthExtensions
{
    public static async Task ApplyStealthAsync(this IPage page, StealthConfig? config = null)
    {
        var stealthConfig = config ?? new StealthConfig();
        foreach (var script in StealthScriptProvider.BuildScripts(stealthConfig))
        {
            await page.AddInitScriptAsync(script).ConfigureAwait(false);
        }
    }

    public static async Task ApplyStealthAsync(this IBrowserContext context, StealthConfig? config = null)
    {
        var stealthConfig = config ?? new StealthConfig();
        foreach (var script in StealthScriptProvider.BuildScripts(stealthConfig))
        {
            await context.AddInitScriptAsync(script).ConfigureAwait(false);
        }
    }
}
