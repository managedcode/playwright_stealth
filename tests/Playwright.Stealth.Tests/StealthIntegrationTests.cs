using System.Globalization;
using System.IO;
using System.Text.Json;
using ManagedCode.Playwright.Stealth;
using Microsoft.Playwright;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace ManagedCode.Playwright.Stealth.Tests;

public sealed class StealthIntegrationTests
{
    private const int NavigationTimeoutMs = 120_000;
    private const int RetryCount = 2;
    private const string SignalAttributeName = "data-stealth-signals";
    private const int NetworkIdleTimeoutMs = 10_000;
    private const string GoogleSearchUrl = "https://www.google.com/search?q=managed+code+software+company+managed-code.com&hl=en&gl=us&num=10&safe=off";
    private const string GoogleChallengeSkipReason = "Google returned an unusual-traffic challenge instead of search results.";
    private static readonly string ScreenshotDirectory = Path.Combine(AppContext.BaseDirectory, "artifacts", "screenshots");
    private static readonly string SignalDirectory = Path.Combine(AppContext.BaseDirectory, "artifacts", "signals");

    [Test]
    public Task BrowserScan_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.BrowserScan, applyStealth: true);

    [Test]
    public Task SannySoft_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.SannySoft, applyStealth: true);

    [Test]
    public Task Intoli_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.Intoli, applyStealth: true);

    [Test]
    public Task Fingerprint_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.Fingerprint, applyStealth: true);

    [Test]
    public Task AreYouHeadless_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.AreYouHeadless, applyStealth: true);

    [Test]
    public Task PixelScan_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.PixelScan, applyStealth: true);

    [Test]
    public Task BrowserScan_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.BrowserScan, applyStealth: false);

    [Test]
    public Task SannySoft_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.SannySoft, applyStealth: false);

    [Test]
    public Task Intoli_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.Intoli, applyStealth: false);

    [Test]
    public Task Fingerprint_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.Fingerprint, applyStealth: false);

    [Test]
    public Task AreYouHeadless_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.AreYouHeadless, applyStealth: false);

    [Test]
    public Task Incolumitas_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.Incolumitas, applyStealth: true);

    [Test]
    public Task CreepJs_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.CreepJs, applyStealth: true);

    [Test]
    public Task DeviceAndBrowserInfo_Should_Not_Surface_BotSignals() =>
        RunSiteCheckAsync(StealthTestSites.DeviceAndBrowserInfo, applyStealth: true);

    [Test]
    public Task PixelScan_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.PixelScan, applyStealth: false);

    [Test]
    public Task Incolumitas_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.Incolumitas, applyStealth: false);

    [Test]
    public Task CreepJs_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.CreepJs, applyStealth: false);

    [Test]
    public Task DeviceAndBrowserInfo_Baseline_Should_Capture_Signals() =>
        RunSiteCheckAsync(StealthTestSites.DeviceAndBrowserInfo, applyStealth: false);

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public Task Iphey_BrowserAssessment_DoesNotReportErrors(bool headless) =>
        RunFeedbackCheckAsync(StealthTestSites.Iphey, headless);

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public Task DeviceAndBrowserInfo_BotAssessment_ReportsHuman(bool headless) =>
        RunFeedbackCheckAsync(StealthTestSites.DeviceAndBrowserBotCheck, headless);

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public Task DeviceAndBrowserInfo_BotAssessment_ReportsNoAutomationSignals(bool headless) =>
        RunFeedbackCheckAsync(StealthTestSites.DeviceAndBrowserBotCheck, headless, requireHumanVerdict: false);

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public Task CreepJs_PrototypeAssessment_ReportsNoLies(bool headless) =>
        RunFeedbackCheckAsync(StealthTestSites.CreepJsPrototype, headless);

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public Task CreepJs_HeadlessAssessment_ReportsNoHeadlessOrStealthSignals(bool headless) =>
        RunFeedbackCheckAsync(StealthTestSites.CreepJs, headless);

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public Task Incolumitas_WorkerAssessment_MatchesPageNavigator(bool headless) =>
        RunFeedbackCheckAsync(StealthTestSites.Incolumitas, headless);

    private static async Task RunFeedbackCheckAsync(string url, bool headless, bool requireHumanVerdict = true)
    {
        if (url == StealthTestSites.Iphey && OperatingSystem.IsMacOS()
            && Environment.GetEnvironmentVariable("RUN_MACOS_IPHEY_TESTS") != "1")
            throw new TUnit.Core.Exceptions.SkipTestException("Iphey probes optional macOS fonts. Run this regression on Linux; RUN_MACOS_IPHEY_TESTS=1 opts in on a Mac with the necessary fonts already available.");

        await WithPageAsync(true, async page =>
        {
            string label = $"feedback_{(headless ? "headless" : "headful")}_{GetScreenshotLabelFromUrl(url)}{(requireHumanVerdict ? string.Empty : "_automation")}";
            try
            {
                await NavigateWithRetriesAsync(page, url);
                string expression = url switch
                {
                    StealthTestSites.DeviceAndBrowserBotCheck => "document.querySelector('#jsonResult')?.textContent.trim()",
                    StealthTestSites.CreepJsPrototype => "document.querySelector('#fingerprint-data .visitor-info')?.textContent.includes('lies detected')",
                    StealthTestSites.CreepJs => "document.querySelector('.headless-rating') && document.querySelector('.stealth-rating')",
                    StealthTestSites.Incolumitas => "document.querySelector('#webWorkerRes')?.textContent.trim().startsWith('{') && document.querySelector('#serviceWorkerRes')?.textContent.trim().startsWith('{')",
                    StealthTestSites.Iphey => "document.querySelector('#hero-status')?.textContent.trim()",
                    _ => throw new InvalidOperationException($"No detector assertion for {url}.")
                };
                await page.WaitForFunctionAsync($"() => !!({expression})", options: new() { Timeout = NavigationTimeoutMs });
                string[] failures = await page.EvaluateAsync<string[]>("""
                    mode => {
                        const failures = [];
                        const json = document.querySelector('#jsonResult')?.textContent;
                        if (json) {
                            const result = JSON.parse(json);
                            if (mode === 'automation') {
                                for (const [signal, detected] of Object.entries(result.details)) {
                                    if (detected && signal !== 'hasSuspiciousWeakSignals') failures.push(`${signal}: ${detected}`);
                                }
                            } else if (result.isBot !== false) failures.push(json);
                        }
                        const prototype = document.querySelector('#fingerprint-data .visitor-info')?.textContent;
                        if (mode === 'prototype' && prototype?.includes('lies detected') && !/\b0 lies detected\b/.test(prototype)) failures.push(prototype.trim());
                        for (const selector of ['.headless-rating', '.stealth-rating']) {
                            const text = document.querySelector(selector)?.textContent.trim();
                            if (text && !text.startsWith('0% ')) failures.push(text);
                        }
                        for (const selector of ['#webWorkerRes', '#serviceWorkerRes', '#fp']) {
                            const text = document.querySelector(selector)?.textContent;
                            if (!text) continue;
                            const values = JSON.parse(text);
                            for (const key of ['userAgent', 'appVersion', 'platform', 'hardwareConcurrency', 'language', 'languages', 'deviceMemory']) {
                                if (key in values && key in navigator && JSON.stringify(values[key]) !== JSON.stringify(navigator[key])) {
                                    failures.push(`${selector}: ${key}: ${JSON.stringify(values[key])} != ${JSON.stringify(navigator[key])}`);
                                }
                            }
                        }
                        const browser = document.querySelector('.browser-tile');
                        if (mode === 'iphey' && (!browser || browser.classList.contains('code-block--error'))) failures.push('Iphey: browser assessment failed');
                        return failures;
                    }
                    """, requireHumanVerdict ? url switch
                {
                    StealthTestSites.CreepJsPrototype => "prototype",
                    StealthTestSites.Iphey => "iphey",
                    _ => "verdict"
                } : "automation");
                Directory.CreateDirectory(SignalDirectory);
                await File.WriteAllTextAsync(Path.Combine(SignalDirectory, label + ".json"), JsonSerializer.Serialize(new
                {
                    Url = url,
                    Headless = headless,
                    Failures = failures,
                    DetectorText = await page.Locator("body").InnerTextAsync()
                }, new JsonSerializerOptions { WriteIndented = true }));
                if (url == StealthTestSites.DeviceAndBrowserBotCheck)
                {
                    Console.WriteLine(await page.EvaluateAsync<string>("""
                        () => JSON.stringify({ renderer: window.fingerprint?.webGLRenderer,
                            vendor: window.fingerprint?.webGLVendor,
                            shader: window.fingerprint?.webGLTranslatedLang,
                            gpu: window.fingerprint?.webGPUAdapterInfo,
                            voices: window.fingerprint?.speechSynthesisVoices,
                            language: window.fingerprint?.language,
                            locale: window.fingerprint?.localeLanguage })
                        """));
                }
                await Assert.That(failures).IsEmpty();
            }
            finally
            {
                await CaptureScreenshotAsync(page, label);
            }
        }, headless, new() { NavigatorHardwareConcurrency = 18 });
    }

    [Test]
    public async Task GoogleSearch_Should_Find_ManagedCode()
    {
        await WithPageAsync(applyStealth: true, async page =>
        {
            var label = "google_search";
            try
            {
                await NavigateWithRetriesAsync(page, GoogleSearchUrl);
                await TryAcceptGoogleConsentAsync(page);
                await SkipIfGoogleChallengeAsync(page);
                try
                {
                    await page.WaitForSelectorAsync("#search", new PageWaitForSelectorOptions
                    {
                        Timeout = NavigationTimeoutMs
                    });
                }
                catch (TimeoutException)
                {
                    await SkipIfGoogleChallengeAsync(page);
                    throw;
                }
                await PrimePageAsync(page);

                var links = await page.EvaluateAsync<string[]>("""
                    () => Array.from(document.querySelectorAll('#search a'))
                        .map(link => link.href)
                        .filter(href => href)
                """);

                var hasManagedCode = links.Any(link => link.Contains("managed-code.com", StringComparison.OrdinalIgnoreCase));
                await Assert.That(hasManagedCode).IsTrue();
            }
            finally
            {
                await CaptureScreenshotAsync(page, label);
            }
        });
    }

    private static async Task SkipIfGoogleChallengeAsync(IPage page)
    {
        if (await IsGoogleChallengeAsync(page))
        {
            throw new TUnit.Core.Exceptions.SkipTestException(GoogleChallengeSkipReason);
        }
    }

    private static async Task<bool> IsGoogleChallengeAsync(IPage page)
    {
        if (page.Url.Contains("/sorry/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            var bodyText = await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions
            {
                Timeout = 2_000
            });

            return bodyText.Contains("unusual traffic", StringComparison.OrdinalIgnoreCase) ||
                   bodyText.Contains("Our systems have detected", StringComparison.OrdinalIgnoreCase) ||
                   bodyText.Contains("I'm not a robot", StringComparison.OrdinalIgnoreCase);
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (PlaywrightException)
        {
            return false;
        }
    }

    private static async Task RunSiteCheckAsync(string url, bool applyStealth)
    {
        await WithPageAsync(applyStealth, async page =>
        {
            var label = GetScreenshotLabelFromUrl(url);
            var modeLabel = applyStealth ? "stealth" : "baseline";
            try
            {
                await NavigateWithRetriesAsync(page, url);
                await PrimePageAsync(page);
                var signals = await GetSignalsAsync(page);
                await PersistSignalsAsync($"{modeLabel}_{label}", url, modeLabel, signals);
                if (applyStealth)
                {
                    await AssertStealthSignalsAsync(signals);
                }
            }
            finally
            {
                await CaptureScreenshotAsync(page, $"{modeLabel}_{label}");
            }
        });
    }

    private static async Task WithPageAsync(bool applyStealth, Func<IPage, Task> action, bool? headless = null, StealthConfig? config = null)
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless ?? MaxParallelTestsForPipeline.Headless,
            Channel = "chromium",
            Args = PlaywrightStealthExtensions.StealthArgs
        });

        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = CultureInfo.CurrentCulture.Name,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            DeviceScaleFactor = 2
        });

        if (applyStealth)
        {
            await context.ApplyStealthAsync(config);
        }
        var page = await context.NewPageAsync();
        await InstallSignalProbeAsync(page);

        await action(page);
    }

    private static async Task NavigateWithRetriesAsync(IPage page, string url)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= RetryCount; attempt++)
        {
            try
            {
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = NavigationTimeoutMs
                });
                await page.WaitForLoadStateAsync(LoadState.Load, new PageWaitForLoadStateOptions
                {
                    Timeout = NavigationTimeoutMs
                });
                return;
            }
            catch (Exception ex) when (attempt < RetryCount)
            {
                lastError = ex;
                await page.WaitForTimeoutAsync(1_000);
            }
        }

        throw new InvalidOperationException($"Navigation failed for {url}.", lastError);
    }

    private static async Task<StealthSignals> GetSignalsAsync(IPage page)
    {
        await page.WaitForFunctionAsync($"() => document.documentElement.hasAttribute('{SignalAttributeName}')", new PageWaitForFunctionOptions
        {
            Timeout = NavigationTimeoutMs
        });

        var json = await page.GetAttributeAsync("html", SignalAttributeName);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Stealth signal payload was not generated.");
        }

        var signals = JsonSerializer.Deserialize<StealthSignals>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (signals is null)
        {
            throw new InvalidOperationException("Stealth signal payload could not be parsed.");
        }

        return signals;
    }

    private static async Task AssertStealthSignalsAsync(StealthSignals signals)
    {
        await Assert.That(signals.WebDriver).IsFalse();
        if (signals.PluginCount <= 0)
        {
            throw new InvalidOperationException("PluginCount=0.");
        }
        await Assert.That(signals.LanguageCount).IsGreaterThan(0);
        await Assert.That(signals.UserAgent.Contains("Headless", StringComparison.OrdinalIgnoreCase)).IsFalse();
        await Assert.That(signals.HardwareConcurrency).IsGreaterThan(0);
        await Assert.That(signals.Vendor.Length).IsGreaterThan(0);
        await Assert.That(signals.Platform.Length).IsGreaterThan(0);
        await Assert.That(signals.HasChrome).IsTrue();
        await Assert.That(signals.WebglVendor.Length).IsGreaterThan(0);
        await Assert.That(signals.WebglRenderer.Length).IsGreaterThan(0);

        // New assertions: WebGL RENDERER param should not contain SwiftShader
        await Assert.That(signals.WebglRendererParam.Contains("SwiftShader", StringComparison.OrdinalIgnoreCase)).IsFalse();

        // navigator.connection should exist
        await Assert.That(signals.HasConnection).IsTrue();
        await Assert.That(signals.ConnectionType.Length).IsGreaterThan(0);

        // navigator.deviceMemory should be present
        await Assert.That(signals.DeviceMemory).IsGreaterThan(0);

        // Screen dimensions should be realistic
        await Assert.That(signals.ScreenWidth).IsGreaterThan(0);
        await Assert.That(signals.ScreenHeight).IsGreaterThan(0);
        await Assert.That(signals.ColorDepth).IsGreaterThan(0);

        // Broken image should not show 16x16 (headless artifact)
        await Assert.That(signals.BrokenImageWidth).IsNotEqualTo(16);
        await Assert.That(signals.BrokenImageHeight).IsNotEqualTo(16);

        // Touch points should be realistic
        await Assert.That(signals.MaxTouchPoints).IsGreaterThanOrEqualTo(0);

        // PDF viewer should be exposed without forcing a non-native value.
        await Assert.That(signals.HasPdfViewerEnabled).IsTrue();

        // No automation properties should be present
        await Assert.That(signals.HasAutomationProps).IsFalse();
    }

    private static async Task PrimePageAsync(IPage page)
    {
        try
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
            {
                Timeout = NetworkIdleTimeoutMs
            });
        }
        catch (TimeoutException)
        {
            // Some sites keep background requests alive; continue with best-effort readiness.
        }

        await page.WaitForTimeoutAsync(500);

        try
        {
            await page.Locator("body").ClickAsync(new LocatorClickOptions { Timeout = 2_000 });
        }
        catch (TimeoutException)
        {
            // Ignore when the body is not clickable.
        }
        catch (PlaywrightException)
        {
            // Ignore transient input errors.
        }

        try
        {
            await page.EvaluateAsync("() => window.scrollBy(0, 800)");
        }
        catch (PlaywrightException)
        {
            // Ignore if the execution context is unavailable.
        }

        await page.WaitForTimeoutAsync(500);
    }

    private static async Task TryAcceptGoogleConsentAsync(IPage page)
    {
        var candidates = new[]
        {
            "#L2AGLb",
            "button:has-text(\"I agree\")",
            "button:has-text(\"Accept all\")",
            "button:has-text(\"Accept all cookies\")",
            "button:has-text(\"Accept everything\")"
        };

        foreach (var selector in candidates)
        {
            try
            {
                await page.Locator(selector).ClickAsync(new LocatorClickOptions { Timeout = 2_000 });
                return;
            }
            catch (TimeoutException)
            {
                // Ignore timeouts for missing selectors.
            }
            catch (PlaywrightException)
            {
                // Ignore transient Playwright selector errors.
            }
        }
    }

    private static async Task InstallSignalProbeAsync(IPage page)
    {
        await page.AddInitScriptAsync($$"""
            const writeSignals = () => {
                const canvas = document.createElement('canvas');
                const gl = canvas.getContext('webgl');
                const debugInfo = gl ? gl.getExtension('WEBGL_debug_renderer_info') : null;
                const webglVendor = debugInfo ? gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL) : '';
                const webglRenderer = debugInfo ? gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL) : '';

                // Check WebGL RENDERER param (7937) for ANGLE/SwiftShader leaks
                const webglRendererParam = gl ? gl.getParameter(7937) : '';

                // Check broken image dimensions
                let brokenImageWidth = -1;
                let brokenImageHeight = -1;
                try {
                    const img = new Image();
                    img.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==';
                    // Use a non-existent URL to test broken image
                    const brokenImg = document.createElement('img');
                    brokenImg.src = 'https://non-existent-domain-stealth-test.invalid/broken.png';
                    brokenImageWidth = brokenImg.width;
                    brokenImageHeight = brokenImg.height;
                } catch (e) {}

                const signals = {
                    webDriver: navigator.webdriver === true,
                    pluginCount: navigator.plugins ? navigator.plugins.length : 0,
                    languageCount: navigator.languages ? navigator.languages.length : 0,
                    userAgent: navigator.userAgent || '',
                    hardwareConcurrency: navigator.hardwareConcurrency || 0,
                    vendor: navigator.vendor || '',
                    platform: navigator.platform || '',
                    hasChrome: typeof window.chrome !== 'undefined',
                    webglVendor: webglVendor || '',
                    webglRenderer: webglRenderer || '',
                    webglRendererParam: webglRendererParam || '',
                    hasConnection: typeof navigator.connection !== 'undefined',
                    connectionType: (navigator.connection && navigator.connection.effectiveType) || '',
                    deviceMemory: navigator.deviceMemory || 0,
                    screenWidth: screen.width || 0,
                    screenHeight: screen.height || 0,
                    colorDepth: screen.colorDepth || 0,
                    brokenImageWidth: brokenImageWidth,
                    brokenImageHeight: brokenImageHeight,
                    maxTouchPoints: navigator.maxTouchPoints || 0,
                    hasPdfViewerEnabled: 'pdfViewerEnabled' in navigator,
                    pdfViewerEnabled: navigator.pdfViewerEnabled === true,
                    hasAutomationProps: (typeof window.cdc_adoQpoasnfa76pfcZLmcfl_Array !== 'undefined') ||
                                       (typeof window.domAutomationController !== 'undefined') ||
                                       (typeof window.__webdriver_script_fn !== 'undefined')
                };

                document.documentElement.setAttribute('{{SignalAttributeName}}', JSON.stringify(signals));
            };

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', writeSignals, { once: true });
            } else {
                writeSignals();
            }
            """);
    }

    private static async Task CaptureScreenshotAsync(IPage page, string label)
    {
        try
        {
            Directory.CreateDirectory(ScreenshotDirectory);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            var safeLabel = SanitizeFileName(label);
            var baseName = $"{timestamp}_{safeLabel}";
            var path = Path.Combine(ScreenshotDirectory, $"{baseName}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = path,
                FullPage = true,
                Timeout = 15_000
            });
            Console.WriteLine($"Saved screenshot: {path}");

            await CaptureViewportSlicesAsync(page, baseName);
        }
        catch (Exception ex) when (ex is IOException or PlaywrightException or TimeoutException)
        {
            Console.WriteLine($"Failed to capture screenshot: {ex.Message}");
        }
    }

    private static async Task PersistSignalsAsync(string label, string url, string mode, StealthSignals signals)
    {
        try
        {
            Directory.CreateDirectory(SignalDirectory);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            var safeLabel = SanitizeFileName(label);
            var path = Path.Combine(SignalDirectory, $"{timestamp}_{safeLabel}.json");
            var payload = new SignalSnapshot
            {
                Url = url,
                Mode = mode,
                TimestampUtc = DateTimeOffset.UtcNow,
                Signals = signals
            };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(path, json);
            Console.WriteLine($"Saved signals: {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Failed to persist signals: {ex.Message}");
        }
    }

    private static async Task CaptureViewportSlicesAsync(IPage page, string baseName)
    {
        try
        {
            var metrics = await page.EvaluateAsync<ViewportMetrics>("""
                () => ({
                    scrollHeight: document.documentElement.scrollHeight,
                    viewportHeight: window.innerHeight
                })
                """);

            if (metrics.ScrollHeight <= 0 || metrics.ViewportHeight <= 0)
            {
                return;
            }

            var step = Math.Max(1, metrics.ViewportHeight - 120);
            var index = 1;
            for (var offset = 0; offset < metrics.ScrollHeight; offset += step)
            {
                await page.EvaluateAsync("y => window.scrollTo(0, y)", offset);
                await page.WaitForTimeoutAsync(250);

                var path = Path.Combine(ScreenshotDirectory, $"{baseName}_slice_{index:D2}.png");
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = path,
                    FullPage = false
                });
                Console.WriteLine($"Saved screenshot: {path}");

                index++;
                if (offset + metrics.ViewportHeight >= metrics.ScrollHeight)
                {
                    break;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or PlaywrightException)
        {
            Console.WriteLine($"Failed to capture viewport slices: {ex.Message}");
        }
    }

    private static string GetScreenshotLabelFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        var path = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrWhiteSpace(path))
        {
            return uri.Host;
        }

        return $"{uri.Host}_{path.Replace('/', '_')}";
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var buffer = value.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            if (Array.IndexOf(invalidChars, buffer[i]) >= 0)
            {
                buffer[i] = '_';
            }
        }

        return new string(buffer);
    }

    private sealed class StealthSignals
    {
        public bool WebDriver { get; set; }
        public int PluginCount { get; set; }
        public int LanguageCount { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public int HardwareConcurrency { get; set; }
        public string Vendor { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public bool HasChrome { get; set; }
        public string WebglVendor { get; set; } = string.Empty;
        public string WebglRenderer { get; set; } = string.Empty;
        public string WebglRendererParam { get; set; } = string.Empty;
        public bool HasConnection { get; set; }
        public string ConnectionType { get; set; } = string.Empty;
        public int DeviceMemory { get; set; }
        public int ScreenWidth { get; set; }
        public int ScreenHeight { get; set; }
        public int ColorDepth { get; set; }
        public int BrokenImageWidth { get; set; }
        public int BrokenImageHeight { get; set; }
        public int MaxTouchPoints { get; set; }
        public bool HasPdfViewerEnabled { get; set; }
        public bool PdfViewerEnabled { get; set; }
        public bool HasAutomationProps { get; set; }
    }

    private sealed class SignalSnapshot
    {
        public string Url { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public DateTimeOffset TimestampUtc { get; set; }
        public StealthSignals Signals { get; set; } = new();
    }

    private sealed class ViewportMetrics
    {
        public int ScrollHeight { get; set; }
        public int ViewportHeight { get; set; }
    }

}
