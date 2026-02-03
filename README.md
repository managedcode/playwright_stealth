# ManagedCode.Playwright.Stealth (.NET)

A .NET port of the Playwright stealth scripts adapted for ManagedCode usage.
This package adds a single call to apply a collection of stealth init scripts to a Playwright page or browser context.

## Install

```bash
 dotnet add package ManagedCode.Playwright.Stealth
```

## Usage

```csharp
using Microsoft.Playwright;
using ManagedCode.Playwright.Stealth;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true
});

var context = await browser.NewContextAsync();
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
    NavigatorUserAgentValue = "Mozilla/5.0 ...",
    WebglVendor = true
};

await page.ApplyStealthAsync(config);
```

## Development

1. Install .NET SDK 10.0.102 or later.
2. Restore dependencies:

```bash
 dotnet restore ManagedCode.Playwright.Stealth.sln
```

3. Run tests (uses Microsoft.Testing.Platform via `global.json`):

```bash
 dotnet test --solution ManagedCode.Playwright.Stealth.sln --configuration Release
```

## Test Sites

The integration tests exercise stealth checks on these sites:

- https://www.browserscan.net/bot-detection
- https://bot.sannysoft.com/
- https://www.intoli.com/blog/not-possible-to-block-chrome-headless/chrome-headless-test.html
- https://fingerprint.com/demo
- https://nowsecure.nl

> These sites can change at any time; adjust assertions as needed.
