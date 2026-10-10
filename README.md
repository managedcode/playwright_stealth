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
    Channel = "chromium",
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

The configuration covers **31 detection vectors** across these categories. Defaults preserve native navigator, GPU, screen, permission, media and speech APIs where current Chromium already exposes normal values. Canvas/audio noise is opt-in because it can introduce detectable inconsistencies. `PerformanceJitter` coarsens `performance.now()` to milliseconds by default while keeping it monotonic and preserving native requestAnimationFrame timestamps. Set it to `false` when sub-millisecond measurements are needed.

On Chromium, `context.ApplyStealthAsync` also initializes dedicated, module, shared and service workers. HTTP shared/service-worker scripts receive initialization in their original response to prevent a first-script race with Playwright; their URLs and strict-mode directives are preserved. Blob/data workers use Chromium's paused-target initialization, whose timing can also depend on other CDP clients. User-Agent headers and client hints use the same identity as the page; configured CPU counts and navigator values are applied to workers too. Setup briefly opens a locally fulfilled initialization page and closes it before returning. One separate blank context per browser carries Chromium's CDP protocol messages until the browser closes; its network requests are blocked. Apply stealth before creating application pages. The page-only overload installs document scripts; use the context overload for worker and request identity consistency.

### Navigator Properties
| Patch | Description | Config |
|-------|-------------|--------|
| `navigator.webdriver` | Returns `false` instead of `true` | `WebDriver` |
| `navigator.plugins` / `mimeTypes` | Fake plugin array (Chrome PDF Plugin, etc.) | `NavigatorPlugins` |
| `navigator.languages` | Preserves native values by default; configurable language array when supplied | `NavigatorLanguages` |
| `navigator.userAgent` | Strips headless markers from UA string | `NavigatorUserAgent` |
| `navigator.vendor` | Preserves native value when already "Google Inc."; configurable otherwise | `NavigatorVendor` |
| `navigator.platform` | Configurable platform string | `NavigatorPlatform` |
| `navigator.hardwareConcurrency` | Preserves native value by default; configurable CPU core count when supplied | `NavigatorHardwareConcurrency` |
| `navigator.deviceMemory` | Preserves native value by default; configurable device memory in GB when supplied | `NavigatorDeviceMemory` |
| `navigator.connection` | Preserves native value when present; mocks NetworkInformation when missing | `NavigatorConnection` |
| `navigator.permissions` | Preserves native permission states and PermissionStatus objects | `NavigatorPermissions` |
| `navigator.maxTouchPoints` | Preserves native value by default; configurable touch point count when supplied | `NavigatorMaxTouchPoints` |
| `navigator.pdfViewerEnabled` | Preserves native value when present; mocks it when missing | `NavigatorPdfViewer` |

### Chrome APIs
| Patch | Description | Config |
|-------|-------------|--------|
| `window.chrome` / `chrome.runtime` | Full Chrome extension API mock | `ChromeRuntime` |
| `chrome.app` | Chrome App API mock | `ChromeApp` |
| `chrome.csi` | Chrome CSI timing mock | `ChromeCsi` |
| `chrome.loadTimes` | Preserves the native headful API when present; mocks it when missing | `ChromeLoadTimes` |

### Graphics & Rendering
| Patch | Description | Config |
|-------|-------------|--------|
| WebGL vendor/renderer | Preserves native GPU values by default; explicit vendor/renderer overrides | `WebglVendor` |
| Canvas fingerprint | Optional noise; disabled by default | `CanvasFingerprint` |
| Broken image dimensions | Fixes 16x16 headless artifact to 0x0 | `BrokenImage` |

### Audio & Media
| Patch | Description | Config |
|-------|-------------|--------|
| AudioContext fingerprint | Optional noise; disabled by default | `AudioContext` |
| Media codecs | Correct codec support responses | `MediaCodecs` |
| Speech synthesis | Preserves native voices and asynchronous voice loading | `SpeechSynthesis` |

### Window & Screen
| Patch | Description | Config |
|-------|-------------|--------|
| `window.outerWidth/Height` | Realistic outer dimensions | `OuterDimensions` |
| `screen.*` dimensions | Preserves native metrics; supplies fallbacks for missing dimensions | `ScreenDimensions` |

### Anti-Detection & Timing
| Patch | Description | Config |
|-------|-------------|--------|
| CDP detection | Masks Chrome DevTools Protocol traces | `CdpDetection` |
| Automation properties | Removes `cdc_*`, `$cdc_*`, `domAutomationController` | `AutomationProperties` |
| Performance jitter | Monotonic millisecond clock precision; enabled by default | `PerformanceJitter` |

### DOM & Internals
| Patch | Description | Config |
|-------|-------------|--------|
| iframe `contentWindow` | Preserves the native getter and srcdoc behavior when available | `IframeContentWindow` |
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

On Linux these arguments select ANGLE's native OpenGL backend through EGL and allow the installed driver, including Mesa on CI runners, instead of falling back to SwiftShader. This also supports headless rendering without a display. WebGL rendering and shader reporting remain native in pages and workers. `LaunchStealthAsync` preserves an explicitly supplied `--use-angle` or `--use-gl` backend. Linux hosts need a working EGL/OpenGL driver; headful runs also need a display.

## Configuration Reference

### Toggle Options (bool)

`WebDriver`, `WebglVendor`, `ChromeApp`, `ChromeCsi`, `ChromeLoadTimes`, `ChromeRuntime`,
`IframeContentWindow`, `MediaCodecs`, `Hairline`, `OuterDimensions`,
`NavigatorLanguages`, `NavigatorPermissions`, `NavigatorPlatform`, `NavigatorPlugins`,
`NavigatorUserAgent`, `NavigatorVendor`, `NavigatorConnection`, `NavigatorPdfViewer`,
`BrokenImage`, `SpeechSynthesis`, `ScreenDimensions`,
`CdpDetection`, `AutomationProperties`, `CanvasFingerprint`, `PerformanceJitter`, `AudioContext`

### Numeric Options (int)

- `NavigatorHardwareConcurrency` (default: 0 to preserve native value, set >0 to spoof)
- `NavigatorDeviceMemory` (default: 0 to preserve native value, set >0 to spoof)
- `NavigatorMaxTouchPoints` (default: -1 to preserve native value, set >=0 to spoof)

### String Options

- `NavigatorUserAgentValue` - Custom user agent string
- `NavigatorPlatformValue` - Custom platform (e.g., "Win32")
- `NavigatorVendorValue` - Vendor name (default: "Google Inc.")
- `Vendor` - WebGL vendor (default: empty to preserve the actual GPU)
- `Renderer` - WebGL renderer (default: empty to preserve the actual GPU)
- `Languages` - Language array (default: empty to preserve native values)
- `RunOnInsecureOrigins` - Allow stealth on http:// origins

## Testing

Integration tests target **10 bot-detection sites**, including both CreepJS pages and both DeviceAndBrowserInfo pages:

- https://www.browserscan.net/bot-detection
- https://bot.sannysoft.com/
- https://www.intoli.com/blog/not-possible-to-block-chrome-headless/chrome-headless-test.html
- https://fingerprint.com/demo
- https://arh.antoinevastel.com/bots/areyouheadless/
- https://pixelscan.net/bot-check
- https://bot.incolumitas.com/
- https://abrahamjuliot.github.io/creepjs/
- https://abrahamjuliot.github.io/creepjs/tests/prototype.html
- https://deviceandbrowserinfo.com/info_device
- https://deviceandbrowserinfo.com/are_you_a_bot
- https://iphey.com/

`LaunchStealthAsync` selects Playwright's full Chromium channel by default, including its new headless mode. When launching the browser yourself, set `Channel = "chromium"`. The separate `chrome-headless-shell` binary lacks native Chrome APIs and cannot provide the same fingerprint. Tests use full Chromium in both headless and headful modes; the installer uses `--no-shell`. Local tests do not install system dependencies or fonts. Iphey can prompt for optional fonts on macOS, so its regressions run on Linux by default; `RUN_MACOS_IPHEY_TESTS=1` opts in on a Mac where those fonts are already available.

Issue #46 regressions run in both headless and headful Chromium. They assert the detector's own rendered verdict, prototype lies, headless/stealth ratings and worker values, and save screenshots and detector output. Local worker tests also cover the first script in classic, module, shared and service workers, HTTP headers and isolation from unconfigured contexts. Graphics regressions require working WebGL and WebGL2, verify actual rendered pixels and compare the native renderer in pages and workers.

DeviceAndBrowserInfo automation checks and the full human verdict must pass on every platform, in both headless and headful modes. Any positive bot verdict, including `hasSuspiciousWeakSignals`, fails the full-verdict test. Screenshots and actual detector output are saved.

These sites can change at any time. If a site changes, update the corresponding test assertions.

Run tests (Playwright browsers install automatically on first run):

```bash
dotnet test --solution ManagedCode.Playwright.Stealth.slnx -c Release
```

On Linux CI runners, set `PLAYWRIGHT_INSTALL_DEPS=1` to install system dependencies
(`playwright install --with-deps`) when tests start. Set `PLAYWRIGHT_INSTALL_DEPS=0` to forbid system dependency installation, including in CI, when the browser libraries are already available.

Headful tests on Linux need a display. CI runs the suite with `xvfb-run --auto-servernum`; use the same wrapper locally when no display is available.

Google search verification runs with the full suite, including CI and nightly, without an opt-in flag. Google may block a runner's IP; an actual unusual-traffic challenge is recorded as a skip with its reason.

## Attribution

This project uses code from the original `playwright_stealth` repository and adapts it for .NET:
https://github.com/AtuboDad/playwright_stealth

## License

MIT. See `LICENSE`.
