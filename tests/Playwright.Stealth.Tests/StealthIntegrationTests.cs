using System.Globalization;
using Microsoft.Playwright;
using ManagedCode.Playwright.Stealth;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace ManagedCode.Playwright.Stealth.Tests;

public sealed class StealthIntegrationTests
{
    private static readonly IReadOnlyList<StealthSite> Sites =
    [
        new("https://www.browserscan.net/bot-detection", "BrowserScan"),
        new("https://bot.sannysoft.com/", "SannySoft"),
        new("https://www.intoli.com/blog/not-possible-to-block-chrome-headless/chrome-headless-test.html", "Intoli"),
        new("https://fingerprint.com/demo", "Fingerprint"),
        new("https://nowsecure.nl", "NowSecure")
    ];

    [Test]
    [Explicit("Integration test hitting external bot-detection sites. Set RUN_STEALTH_INTEGRATION_TESTS=1 to run intentionally.")]
    public async Task Pages_Should_Not_Report_As_Bot()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_STEALTH_INTEGRATION_TESTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = CultureInfo.CurrentCulture.Name
        });

        await context.ApplyStealthAsync();
        var page = await context.NewPageAsync();

        foreach (var site in Sites)
        {
            await page.GotoAsync(site.Url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 120_000
            });

            await EnsureNoBotSignalsAsync(page, site);
        }
    }

    private static async Task EnsureNoBotSignalsAsync(IPage page, StealthSite site)
    {
        var hasWebDriver = await page.EvaluateAsync<bool>("() => navigator.webdriver === true");
        var pluginCount = await page.EvaluateAsync<int>("() => navigator.plugins ? navigator.plugins.length : 0");
        var languageCount = await page.EvaluateAsync<int>("() => navigator.languages ? navigator.languages.length : 0");
        var hasChrome = await page.EvaluateAsync<bool>("() => typeof window.chrome !== 'undefined'");
        var bodyText = await page.InnerTextAsync("body");

        await Assert.That(hasWebDriver).IsFalse();
        await Assert.That(pluginCount).IsGreaterThan(0);
        await Assert.That(languageCount).IsGreaterThan(0);
        await Assert.That(hasChrome).IsTrue();
        await Assert.That(bodyText.Contains("bot", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await Assert.That(bodyText.Contains("headless", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await Assert.That(bodyText.Contains("automation", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    private sealed record StealthSite(string Url, string Name);
}
