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

The configuration covers **31 detection vectors** across these categories. Defaults preserve native navigator, GPU, screen, permission, media and speech APIs where current Chromium already exposes normal values. Canvas/audio noise is opt-in because it can introduce detectable inconsistencies. `PerformanceJitter` coarsens `performance.now()` to 10ms by default while keeping it monotonic and preserving native requestAnimationFrame timestamps. Set it to `false` when native clock precision is needed.

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
| Performance jitter | Monotonic 10ms clock precision to normalize CDP error-stack timing; enabled by default | `PerformanceJitter` |

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

For Ubuntu runners, install the native graphics libraries alongside Playwright's browser dependencies:

```bash
sudo apt-get install -y --no-install-recommends libegl1 libegl-mesa0 libgles2 libgl1-mesa-dri
```

## Configuration Reference

### Toggle Options (bool)

`WebDriver`, `WebglVendor`, `ChromeApp`, `ChromeCsi`, `ChromeLoadTimes`, `ChromeRuntime`,
`IframeContentWindow`, `MediaCodecs`, `Hairline`, `OuterDimensions`,
`NavigatorLanguages`, `NavigatorPermissions`, `NavigatorPlatform`, `NavigatorPlugins`,
`NavigatorUserAgent`, `NavigatorVendor`, `NavigatorConnection`, `NavigatorPdfViewer`,
`BrokenImage`, `SpeechSynthesis`, `ScreenDimensions`,
`CdpDetection`, `AutomationProperties`, `CanvasFingerprint`, `PerformanceJitter`, `AudioContext`

`PerformanceJitter` keeps `performance.now()` monotonic and preserves native receiver errors and animation-frame timestamps. Set it to `false` to preserve native clock precision; Chromium's CDP error-stack collection may then remain detectable by timing probes.

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

The 1.1.1 suite contains **64 test cases**: 5 configuration/script-provider tests, 12 runtime tests, 16 compatibility tests and 31 live-site integration tests. Detector coverage spans **12 pages across 10 sites**, with Google search checked separately. CI, Nightly and Release run the full suite.

### Issue #46 detector regressions

All five pages from [issue #46](https://github.com/managedcode/playwright_stealth/issues/46#issuecomment-6064954615) have explicit **headless and headful** cases in [StealthIntegrationTests](https://github.com/managedcode/playwright_stealth/blob/v1.1.1/tests/Playwright.Stealth.Tests/StealthIntegrationTests.cs):

| Detector page | Required assertion |
|---------------|--------------------|
| [Iphey](https://iphey.com/) | Browser assessment is present and does not report an error. |
| [DeviceAndBrowserInfo bot assessment](https://deviceandbrowserinfo.com/are_you_a_bot) | Full verdict reports `isBot: false`; separate tests check automation signals. |
| [CreepJS prototype tests](https://abrahamjuliot.github.io/creepjs/tests/prototype.html) | Reports `0 lies detected`. |
| [CreepJS](https://abrahamjuliot.github.io/creepjs/) | Headless and stealth ratings both report `0%`. |
| [Incolumitas](https://bot.incolumitas.com/) | Page, dedicated-worker and service-worker navigator values match, including user agent, platform, CPU count and languages. |

DeviceAndBrowserInfo's full human verdict is mandatory on every platform in both modes. Any positive bot verdict, including `hasSuspiciousWeakSignals`, fails the test. Detector failures are never converted to skips. Tests save actual detector output and attempt screenshots for diagnosis.

Additional detector pages are [BrowserScan](https://www.browserscan.net/bot-detection), [SannySoft](https://bot.sannysoft.com/), [Intoli](https://www.intoli.com/blog/not-possible-to-block-chrome-headless/chrome-headless-test.html), [Fingerprint](https://fingerprint.com/demo), [Are You Headless](https://arh.antoinevastel.com/bots/areyouheadless/), [PixelScan](https://pixelscan.net/bot-check) and [DeviceAndBrowserInfo device info](https://deviceandbrowserinfo.com/info_device). The suite also captures baseline signals without stealth. Live sites can change; update the corresponding assertions when their detector output changes.

### Browser runtime regressions

[StealthCompatibilityTests](https://github.com/managedcode/playwright_stealth/blob/v1.1.1/tests/Playwright.Stealth.Tests/StealthCompatibilityTests.cs) run in both browser modes and check:

| Coverage | Required behavior |
|----------|-------------------|
| Native rendering APIs | Default settings preserve canvas, audio, screen, image, iframe, speech, permission and WebGL descriptors. |
| WebGL / WebGL2 | Real rendered pixels are correct and native vendor/renderer values match between page and worker. Linux defaults must use a native EGL driver rather than SwiftShader. |
| Performance clock | Enabled and disabled `PerformanceJitter` settings keep the clock monotonic and preserve native receiver errors, signature and time origin. Enabled precision is 10ms; disabling it preserves the native descriptor. |
| Function prototypes and frames | Native receiver errors, cross-frame function source and srcdoc iframe behavior remain consistent. |
| Worker initialization | First scripts in classic, module, shared and service workers match page navigator values and request identity. HTTP headers, strict-mode scripts and isolation from unconfigured contexts are covered. |

[StealthRuntimeTests](https://github.com/managedcode/playwright_stealth/blob/v1.1.1/tests/Playwright.Stealth.Tests/StealthRuntimeTests.cs) also cover CDP console probes, native navigator/plugins/PDF preservation, explicit navigator overrides and the regression where an existing `chrome.loadTimes` must not abort later scripts.

### Running locally and in CI

Build and run analyzers before testing. Playwright browsers install automatically on the first test run:

```bash
dotnet build -c Release ManagedCode.Playwright.Stealth.slnx
dotnet build -c Release /p:RunAnalyzers=true ManagedCode.Playwright.Stealth.slnx
dotnet test --solution ManagedCode.Playwright.Stealth.slnx -c Release
```

Tests use full Chromium in both modes; the installer uses `--no-shell`. `LaunchStealthAsync` selects `Channel = "chromium"` by default. Set that channel explicitly when launching manually. The separate `chrome-headless-shell` binary lacks native Chrome APIs and cannot provide the same fingerprint.

Headful tests on Linux need a display. CI installs the [native EGL/OpenGL libraries](#stealth-chrome-arguments) and runs:

```bash
xvfb-run --auto-servernum dotnet test --solution ManagedCode.Playwright.Stealth.slnx -c Release
```

On Linux CI runners, `PLAYWRIGHT_INSTALL_DEPS=1` installs Playwright's system dependencies when tests start. Set `PLAYWRIGHT_INSTALL_DEPS=0` to forbid system dependency installation, including in CI, when the libraries are already available. Local tests do not install system dependencies or fonts.

Two environment-dependent exceptions are recorded explicitly:

- Google search runs with the full suite without an opt-in flag. An actual unusual-traffic challenge caused by a runner's IP is recorded as a skip with its reason.
- Iphey runs on Linux by default because it can request optional fonts on macOS. Mac cases skip unless `RUN_MACOS_IPHEY_TESTS=1` is set on a machine where the necessary fonts are already available.

### Verified release results

[Release 1.1.1](https://github.com/managedcode/playwright_stealth/releases/tag/v1.1.1) was verified on **2026-10-10**, at commit [`6538025`](https://github.com/managedcode/playwright_stealth/commit/65380250876127c8e0e3763a6a70cb2169b690f5):

| Workflow | Verified result |
|----------|-----------------|
| [CI](https://github.com/managedcode/playwright_stealth/actions/runs/38044243462) | 64 passed, 0 failed, 0 skipped |
| [Nightly](https://github.com/managedcode/playwright_stealth/actions/runs/38044276667) | 64 passed, 0 failed, 0 skipped |
| [Release](https://github.com/managedcode/playwright_stealth/actions/runs/38044243493) | 64 passed, 0 failed, 0 skipped; NuGet publication succeeded |
| [CodeQL](https://github.com/managedcode/playwright_stealth/actions/runs/38044243466) | Passed |

The published [NuGet 1.1.1 package](https://www.nuget.org/packages/ManagedCode.Playwright.Stealth/1.1.1) was downloaded and checked against the release commit and its embedded 10ms clock script. These results describe that release; current checks appear in [GitHub Actions](https://github.com/managedcode/playwright_stealth/actions).

## Attribution

This project uses code from the original `playwright_stealth` repository and adapts it for .NET:
https://github.com/AtuboDad/playwright_stealth

## License

MIT. See `LICENSE`.
