# ManagedCode.Playwright.Stealth

[![NuGet](https://img.shields.io/nuget/v/ManagedCode.Playwright.Stealth.svg)](https://www.nuget.org/packages/ManagedCode.Playwright.Stealth)
[![Downloads](https://img.shields.io/nuget/dt/ManagedCode.Playwright.Stealth.svg)](https://www.nuget.org/packages/ManagedCode.Playwright.Stealth)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Stealth for Playwright in .NET.** Reduce common browser automation signals with a single call, using the Microsoft.Playwright APIs you already know.

## Highlights

- **One-call setup** — launch Chromium with stealth already applied.
- **Headless and headful** — use the same API for background automation and visible browsers.
- **Consistent browser identity** — keep pages and Chromium workers aligned.
- **Configurable behavior** — customize navigator values and enable or disable individual patches.
- **Native defaults** — preserve native GPU, media and browser values where available; canvas and audio noise are opt-in.

Built for **.NET 10** and **Microsoft.Playwright**.

## Install

```bash
dotnet add package ManagedCode.Playwright.Stealth
```

Install Playwright's Chromium browser for your application before the first launch.

## Quick Start

```csharp
using Microsoft.Playwright;
using ManagedCode.Playwright.Stealth;

using var playwright = await Playwright.CreateAsync();
var (browser, context) = await playwright.Chromium.LaunchStealthAsync();

var page = await context.NewPageAsync();
await page.GotoAsync("https://www.browserscan.net/bot-detection");

await context.CloseAsync();
await browser.CloseAsync();
```

Stealth is applied to the context before pages are created, so every page inherits the configuration.

## Use Your Own Browser Context

Keep your existing launch options and add stealth to your context:

```csharp
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new()
{
    Headless = true,
    Channel = "chromium",
    Args = PlaywrightStealthExtensions.StealthArgs
});

await using var context = await browser.NewContextAsync();
await context.ApplyStealthAsync();

var page = await context.NewPageAsync();
await page.GotoAsync("https://www.browserscan.net/bot-detection");
```

Apply stealth before creating pages. Set `Headless = false` to use a visible browser.

## Customize

Pass a `StealthConfig` to choose the browser profile and patches your application needs:

```csharp
var config = new StealthConfig
{
    NavigatorHardwareConcurrency = 8,
    NavigatorDeviceMemory = 16,
    Languages = ["en-US", "en"],
    CanvasFingerprint = false,
    AudioContext = false
};

await context.ApplyStealthAsync(config);
```

You can also pass the configuration directly to `LaunchStealthAsync`. See [StealthConfig](src/Playwright.Stealth/StealthConfig.cs) for all available options.

## Bot Detection Coverage

Browser checks cover [Iphey](https://iphey.com/), [DeviceAndBrowserInfo](https://deviceandbrowserinfo.com/are_you_a_bot), [CreepJS](https://abrahamjuliot.github.io/creepjs/) and its [prototype checks](https://abrahamjuliot.github.io/creepjs/tests/prototype.html), and [Incolumitas](https://bot.incolumitas.com/) in both headless and headful Chromium.

Coverage also includes [BrowserScan](https://www.browserscan.net/bot-detection), [SannySoft](https://bot.sannysoft.com/), [Intoli](https://www.intoli.com/blog/not-possible-to-block-chrome-headless/chrome-headless-test.html), [Fingerprint](https://fingerprint.com/demo), [Are You Headless](https://arh.antoinevastel.com/bots/areyouheadless/) and [PixelScan](https://pixelscan.net/bot-check).

## Attribution

Adapted for .NET from the original [playwright_stealth](https://github.com/AtuboDad/playwright_stealth) project.

## License

[MIT](LICENSE).
