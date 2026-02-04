# ManagedCode.Playwright.Stealth (.NET)

ManagedCode.Playwright.Stealth applies a curated set of init scripts to Microsoft.Playwright contexts to reduce common bot-detection signals. It adapts the original `playwright_stealth` scripts for .NET with ManagedCode conventions.

## Install

```bash
dotnet add package ManagedCode.Playwright.Stealth
```

## Quick Start

```csharp
using Microsoft.Playwright;
using ManagedCode.Playwright.Stealth;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true
});

var context = await browser.NewContextAsync();

// Apply ManagedCode.Playwright.Stealth before creating pages.
await context.ApplyStealthAsync();

var page = await context.NewPageAsync();
await page.GotoAsync("https://www.browserscan.net/bot-detection");
```

## Configuration

```csharp
var config = new StealthConfig
{
    NavigatorHardwareConcurrency = 8,
    NavigatorLanguages = true,
    NavigatorUserAgentValue = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    WebglVendor = true
};

// Apply ManagedCode.Playwright.Stealth with a custom config.
await context.ApplyStealthAsync(config);
```

## Usage Variants (C#/.NET)

Apply stealth on a page (if you already have a page instance):

```csharp
var page = await context.NewPageAsync();

// Apply ManagedCode.Playwright.Stealth to an existing page.
await page.ApplyStealthAsync();

await page.GotoAsync("https://www.browserscan.net/bot-detection");
```

Disable individual patches:

```csharp
var config = new StealthConfig
{
    WebDriver = false,
    WebglVendor = false,
    NavigatorLanguages = false,
    NavigatorPlugins = false,
    ChromeRuntime = false
};

// Apply ManagedCode.Playwright.Stealth with selective patches disabled.
await context.ApplyStealthAsync(config);
```

Customize platform, vendor, and WebGL identity:

```csharp
var config = new StealthConfig
{
    NavigatorPlatformValue = "Win32",
    NavigatorVendorValue = "Google Inc.",
    NavigatorUserAgentValue = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    Vendor = "Intel Inc.",
    Renderer = "Intel Iris OpenGL Engine",
    Languages = new[] { "en-US", "en" },
    NavigatorHardwareConcurrency = 8
};

// Apply ManagedCode.Playwright.Stealth with explicit platform and WebGL identity.
await context.ApplyStealthAsync(config);
```

Run on insecure origins (affects `chrome.runtime` behavior):

```csharp
var config = new StealthConfig
{
    RunOnInsecureOrigins = true
};

// Apply ManagedCode.Playwright.Stealth on insecure origins.
await context.ApplyStealthAsync(config);
```

## Patched Signals

The default configuration applies patches for:

- `navigator.webdriver`
- `navigator.plugins` and `navigator.mimeTypes`
- `navigator.languages`
- `navigator.userAgent`
- `navigator.vendor`
- `navigator.platform`
- `navigator.hardwareConcurrency`
- `window.chrome` / `chrome.runtime` / related Chrome APIs
- WebGL vendor/renderer
- `window.outerWidth` / `window.outerHeight`
- media codecs and hairline fixes
- iframe `contentWindow` quirks

## Configuration Reference

Common options in `StealthConfig`:

- `WebDriver`, `WebglVendor`, `ChromeApp`, `ChromeCsi`, `ChromeLoadTimes`, `ChromeRuntime`
- `IframeContentWindow`, `MediaCodecs`, `Hairline`, `OuterDimensions`
- `NavigatorLanguages`, `NavigatorPermissions`, `NavigatorPlatform`, `NavigatorPlugins`, `NavigatorUserAgent`, `NavigatorVendor`
- `NavigatorHardwareConcurrency`, `NavigatorUserAgentValue`, `NavigatorPlatformValue`, `NavigatorVendorValue`
- `Vendor`, `Renderer`, `Languages`, `RunOnInsecureOrigins`

## Testing

Integration tests target these bot-detection sites:

- https://www.browserscan.net/bot-detection
- https://bot.sannysoft.com/
- https://www.intoli.com/blog/not-possible-to-block-chrome-headless/chrome-headless-test.html
- https://fingerprint.com/demo
- https://arh.antoinevastel.com/bots/areyouheadless/
- https://pixelscan.net/bot-check

These sites can change at any time. If a site changes, update the corresponding test assertions.

Run tests (Playwright browsers install automatically on first run):

```bash
dotnet test --solution ManagedCode.Playwright.Stealth.slnx -c Release
```

On Linux CI runners, set `PLAYWRIGHT_INSTALL_DEPS=1` to install system dependencies
(`playwright install --with-deps`) when tests start.

Google search verification is optional (Google may block automated traffic). Enable it with:

```bash
RUN_GOOGLE_SEARCH_TESTS=1 dotnet test --solution ManagedCode.Playwright.Stealth.slnx -c Release
```

## Attribution

This project uses code from the original `playwright_stealth` repository and adapts it for .NET:
https://github.com/AtuboDad/playwright_stealth

## License

MIT. See `LICENSE`.
