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

// One-call launch with stealth args + context pre-configured:
var (browser, context) = await playwright.Chromium.LaunchStealthAsync();

var page = await context.NewPageAsync();
await page.GotoAsync("https://www.browserscan.net/bot-detection");
```

## Manual Setup

If you need more control over launch options:

```csharp
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true,
    Args = PlaywrightStealthExtensions.StealthArgs // recommended Chrome flags
});

var context = await browser.NewContextAsync();

// Apply stealth before creating pages.
await context.ApplyStealthAsync();

var page = await context.NewPageAsync();
await page.GotoAsync("https://www.browserscan.net/bot-detection");
```

## Configuration

```csharp
var config = new StealthConfig
{
    NavigatorHardwareConcurrency = 8,
    NavigatorDeviceMemory = 16,
    NavigatorMaxTouchPoints = 1,
    NavigatorUserAgentValue = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    Vendor = "Intel Inc.",
    Renderer = "Intel Iris OpenGL Engine"
};

await context.ApplyStealthAsync(config);
```

Apply stealth on a page (if you already have a page instance):

```csharp
var page = await context.NewPageAsync();
await page.ApplyStealthAsync();
await page.GotoAsync("https://www.browserscan.net/bot-detection");
```

Disable individual patches:

```csharp
var config = new StealthConfig
{
    WebDriver = false,
    WebglVendor = false,
    CanvasFingerprint = false,
    AudioContext = false,
    PerformanceJitter = false
};

await context.ApplyStealthAsync(config);
```

## Patched Signals

The default configuration patches **31 detection vectors** across these categories:

### Navigator Properties
| Patch | Description | Config |
|-------|-------------|--------|
| `navigator.webdriver` | Returns `false` instead of `true` | `WebDriver` |
| `navigator.plugins` / `mimeTypes` | Fake plugin array (Chrome PDF Plugin, etc.) | `NavigatorPlugins` |
| `navigator.languages` | Configurable language array | `NavigatorLanguages` |
| `navigator.userAgent` | Strips headless markers from UA string | `NavigatorUserAgent` |
| `navigator.vendor` | Returns "Google Inc." | `NavigatorVendor` |
| `navigator.platform` | Configurable platform string | `NavigatorPlatform` |
| `navigator.hardwareConcurrency` | Configurable CPU core count (default: 4) | `NavigatorHardwareConcurrency` |
| `navigator.deviceMemory` | Configurable device memory in GB (default: 8) | `NavigatorDeviceMemory` |
| `navigator.connection` | Full NetworkInformation API mock (4g, 10 Mbps) | `NavigatorConnection` |
| `navigator.permissions` | Normalizes Notification permission state | `NavigatorPermissions` |
| `navigator.maxTouchPoints` | Configurable touch point count (default: 1) | `NavigatorMaxTouchPoints` |
| `navigator.pdfViewerEnabled` | Returns `true` | `NavigatorPdfViewer` |

### Chrome APIs
| Patch | Description | Config |
|-------|-------------|--------|
| `window.chrome` / `chrome.runtime` | Full Chrome extension API mock | `ChromeRuntime` |
| `chrome.app` | Chrome App API mock | `ChromeApp` |
| `chrome.csi` | Chrome CSI timing mock | `ChromeCsi` |
| `chrome.loadTimes` | Chrome load times mock | `ChromeLoadTimes` |

### Graphics & Rendering
| Patch | Description | Config |
|-------|-------------|--------|
| WebGL vendor/renderer | Spoofs UNMASKED and standard WebGL params; hides ANGLE/SwiftShader | `WebglVendor` |
| Canvas fingerprint | Adds session-stable noise to canvas rendering | `CanvasFingerprint` |
| Broken image dimensions | Fixes 16x16 headless artifact to 0x0 | `BrokenImage` |

### Audio & Media
| Patch | Description | Config |
|-------|-------------|--------|
| AudioContext fingerprint | Adds noise to audio frequency/channel data | `AudioContext` |
| Media codecs | Correct codec support responses | `MediaCodecs` |
| Speech synthesis | Mock voice list for `getVoices()` | `SpeechSynthesis` |

### Window & Screen
| Patch | Description | Config |
|-------|-------------|--------|
| `window.outerWidth/Height` | Realistic outer dimensions | `OuterDimensions` |
| `screen.*` dimensions | Consistent screen width/height/colorDepth | `ScreenDimensions` |

### Anti-Detection & Timing
| Patch | Description | Config |
|-------|-------------|--------|
| CDP detection | Masks Chrome DevTools Protocol traces | `CdpDetection` |
| Automation properties | Removes `cdc_*`, `$cdc_*`, `domAutomationController` | `AutomationProperties` |
| Performance jitter | Adds realistic timing noise to `performance.now()` and `requestAnimationFrame` | `PerformanceJitter` |

### DOM & Internals
| Patch | Description | Config |
|-------|-------------|--------|
| iframe `contentWindow` | Fixes iframe proxy behavior | `IframeContentWindow` |
| Hairline detection | Fixes Modernizr `offsetHeight` check | `Hairline` |

## Public API

### Extension Methods

```csharp
// Apply to browser context (recommended)
await context.ApplyStealthAsync();
await context.ApplyStealthAsync(customConfig);

// Apply to individual page
await page.ApplyStealthAsync();
await page.ApplyStealthAsync(customConfig);

// One-call launch with stealth pre-configured
var (browser, context) = await playwright.Chromium.LaunchStealthAsync();
var (browser, context) = await playwright.Chromium.LaunchStealthAsync(config, launchOptions, contextOptions);
```

### Stealth Chrome Arguments

```csharp
// Access recommended Chrome args for manual launch setup
string[] args = PlaywrightStealthExtensions.StealthArgs;
```

## Configuration Reference

### Toggle Options (bool)

`WebDriver`, `WebglVendor`, `ChromeApp`, `ChromeCsi`, `ChromeLoadTimes`, `ChromeRuntime`,
`IframeContentWindow`, `MediaCodecs`, `Hairline`, `OuterDimensions`,
`NavigatorLanguages`, `NavigatorPermissions`, `NavigatorPlatform`, `NavigatorPlugins`,
`NavigatorUserAgent`, `NavigatorVendor`, `NavigatorConnection`, `NavigatorPdfViewer`,
`BrokenImage`, `SpeechSynthesis`, `ScreenDimensions`,
`CdpDetection`, `AutomationProperties`, `CanvasFingerprint`, `PerformanceJitter`, `AudioContext`

### Numeric Options (int)

- `NavigatorHardwareConcurrency` (default: 4, set 0 to disable)
- `NavigatorDeviceMemory` (default: 8, set 0 to disable)
- `NavigatorMaxTouchPoints` (default: 1, set -1 to disable)

### String Options

- `NavigatorUserAgentValue` - Custom user agent string
- `NavigatorPlatformValue` - Custom platform (e.g., "Win32")
- `NavigatorVendorValue` - Vendor name (default: "Google Inc.")
- `Vendor` - WebGL vendor (default: "Intel Inc.")
- `Renderer` - WebGL renderer (default: "Intel Iris OpenGL Engine")
- `Languages` - Language array (default: `["en-US", "en"]`)
- `RunOnInsecureOrigins` - Allow stealth on http:// origins

## Testing

Integration tests target **9 bot-detection sites**:

- https://www.browserscan.net/bot-detection
- https://bot.sannysoft.com/
- https://www.intoli.com/blog/not-possible-to-block-chrome-headless/chrome-headless-test.html
- https://fingerprint.com/demo
- https://arh.antoinevastel.com/bots/areyouheadless/
- https://pixelscan.net/bot-check
- https://bot.incolumitas.com/
- https://abrahamjuliot.github.io/creepjs/
- https://deviceandbrowserinfo.com/info_device

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
