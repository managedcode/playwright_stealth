using System.Linq;
using ManagedCode.Playwright.Stealth;
using Microsoft.Playwright;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace ManagedCode.Playwright.Stealth.Tests;

public sealed class StealthRuntimeTests
{
    private static readonly BrowserTypeLaunchOptions HeadlessLaunchOptions = new()
    {
        Headless = true,
        Args = PlaywrightStealthExtensions.StealthArgs
    };

    [Test]
    public async Task BuildScripts_ChromeLoadTimesAlreadyExists_ContinuesLaterEvasions()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);

        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig
        {
            NavigatorHardwareConcurrency = 18
        }).Single();

        var result = await page.EvaluateAsync<ChromeLoadTimesRegressionResult>("""
            script => {
                Object.defineProperty(window, 'chrome', {
                    writable: true,
                    enumerable: true,
                    configurable: true,
                    value: {
                        loadTimes: function loadTimes() {
                            return { requestTime: 123 };
                        }
                    }
                });

                try {
                    Function(script)();
                } catch (error) {
                    return {
                        error: error && error.message ? error.message : String(error),
                        hasLoadTimes: typeof window.chrome.loadTimes === 'function',
                        loadTimesRequestTime: window.chrome.loadTimes().requestTime,
                        hardwareConcurrency: navigator.hardwareConcurrency
                    };
                }

                return {
                    error: '',
                    hasLoadTimes: typeof window.chrome.loadTimes === 'function',
                    loadTimesRequestTime: window.chrome.loadTimes().requestTime,
                    hardwareConcurrency: navigator.hardwareConcurrency
                };
            }
            """, script);

        await Assert.That(result.Error).IsEmpty();
        await Assert.That(result.HasLoadTimes).IsTrue();
        await Assert.That(result.LoadTimesRequestTime).IsEqualTo(123);
        await Assert.That(result.HardwareConcurrency).IsEqualTo(18);
    }

    [Test]
    public async Task BuildScripts_CdpConsoleStackProbe_DoesNotExposeProbeObject()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<CdpConsoleStackProbeResult>("""
            script => {
                console.log = function(...args) {
                    for (const arg of args) {
                        if (arg && typeof arg === 'object') {
                            void arg.stack;
                        }
                    }
                };

                const runProbe = () => {
                    let detected = false;
                    const error = new Error('cdp-probe');
                    Object.defineProperty(error, 'stack', {
                        configurable: true,
                        get() {
                            detected = true;
                            return 'cdp-probe-stack';
                        }
                    });
                    Object.defineProperty(error, 'name', {
                        configurable: true,
                        get() {
                            detected = true;
                            return 'ProbeError';
                        }
                    });

                    console.log(error);
                    return detected;
                };

                const beforeStealth = runProbe();
                Function(script)();
                const afterStealth = runProbe();

                return { beforeStealth, afterStealth };
            }
            """, script);

        await Assert.That(result.BeforeStealth).IsTrue();
        await Assert.That(result.AfterStealth).IsFalse();
    }

    [Test]
    public async Task BuildScripts_CdpConsolePerformanceProbe_DoesNotExposeLargeTabularPayload()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<CdpConsolePerformanceProbeResult>("""
            script => {
                const makePayload = () => {
                    const item = {};
                    for (let index = 0; index < 500; index++) {
                        item[String(index)] = String(index);
                    }

                    const payload = [];
                    for (let index = 0; index < 50; index++) {
                        payload.push(item);
                    }

                    return payload;
                };

                const isLargePayload = value =>
                    Array.isArray(value) &&
                    value.length === 50 &&
                    value[0] &&
                    typeof value[0] === 'object' &&
                    Object.getOwnPropertyNames(value[0]).length === 500;

                let tableReceivedLargePayload = false;
                let logReceivedLargePayload = false;
                console.table = function(value) {
                    tableReceivedLargePayload = isLargePayload(value);
                };
                console.log = function(value) {
                    logReceivedLargePayload = isLargePayload(value);
                };

                const runProbe = () => {
                    tableReceivedLargePayload = false;
                    logReceivedLargePayload = false;
                    console.table(makePayload());
                    console.log(makePayload());

                    return {
                        tableReceivedLargePayload,
                        logReceivedLargePayload
                    };
                };

                const beforeStealth = runProbe();
                Function(script)();
                const afterStealth = runProbe();

                return {
                    beforeTableReceivedLargePayload: beforeStealth.tableReceivedLargePayload,
                    beforeLogReceivedLargePayload: beforeStealth.logReceivedLargePayload,
                    afterTableReceivedLargePayload: afterStealth.tableReceivedLargePayload,
                    afterLogReceivedLargePayload: afterStealth.logReceivedLargePayload
                };
            }
            """, script);

        await Assert.That(result.BeforeTableReceivedLargePayload).IsTrue();
        await Assert.That(result.BeforeLogReceivedLargePayload).IsTrue();
        await Assert.That(result.AfterTableReceivedLargePayload).IsFalse();
        await Assert.That(result.AfterLogReceivedLargePayload).IsFalse();
    }

    [Test]
    public async Task BuildScripts_CdpConsoleToStringProbe_DoesNotInvokeDetectorToString()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<CdpConsoleToStringProbeResult>("""
            script => {
                console.log = function(...args) {
                    for (const arg of args) {
                        if (!arg || typeof arg !== 'object') {
                            continue;
                        }

                        if (typeof arg.toString === 'function') {
                            arg.toString();
                        }

                        for (const propertyName of Object.getOwnPropertyNames(arg)) {
                            const descriptor = Object.getOwnPropertyDescriptor(arg, propertyName);
                            if (!descriptor || !Object.prototype.hasOwnProperty.call(descriptor, 'value')) {
                                continue;
                            }

                            const value = descriptor.value;
                            if (value && typeof value === 'object' && typeof value.toString === 'function') {
                                value.toString();
                            }
                        }
                    }
                };

                const runProbe = () => {
                    let directDetected = false;
                    let nestedDetected = false;
                    const direct = / /;
                    const nested = / /;

                    direct.toString = function() {
                        directDetected = true;
                        return 'direct-probe';
                    };
                    nested.toString = function() {
                        nestedDetected = true;
                        return 'nested-probe';
                    };

                    console.log(direct);
                    console.log({ dep: nested });

                    return {
                        directDetected,
                        nestedDetected
                    };
                };

                const beforeStealth = runProbe();
                Function(script)();
                const afterStealth = runProbe();

                return {
                    beforeDirectDetected: beforeStealth.directDetected,
                    beforeNestedDetected: beforeStealth.nestedDetected,
                    afterDirectDetected: afterStealth.directDetected,
                    afterNestedDetected: afterStealth.nestedDetected
                };
            }
            """, script);

        await Assert.That(result.BeforeDirectDetected).IsTrue();
        await Assert.That(result.BeforeNestedDetected).IsTrue();
        await Assert.That(result.AfterDirectDetected).IsFalse();
        await Assert.That(result.AfterNestedDetected).IsFalse();
    }

    [Test]
    public async Task BuildScripts_CdpConsoleBenignObjects_PreservesConsoleArguments()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<CdpConsoleBenignObjectResult>("""
            script => {
                let received = [];
                console.log = function(...args) {
                    received = args;
                };

                const accessorObject = {};
                let getterCalls = 0;
                Object.defineProperty(accessorObject, 'value', {
                    configurable: true,
                    get() {
                        getterCalls++;
                        return 42;
                    }
                });

                let toStringCalls = 0;
                const toStringObject = {
                    toString() {
                        toStringCalls++;
                        return 'benign-object';
                    }
                };

                Function(script)();
                console.log(accessorObject, toStringObject);

                return {
                    accessorObjectPreserved: received[0] === accessorObject,
                    toStringObjectPreserved: received[1] === toStringObject,
                    getterCalls,
                    toStringCalls
                };
            }
            """, script);

        await Assert.That(result.AccessorObjectPreserved).IsTrue();
        await Assert.That(result.ToStringObjectPreserved).IsTrue();
        await Assert.That(result.GetterCalls).IsEqualTo(0);
        await Assert.That(result.ToStringCalls).IsEqualTo(0);
    }

    [Test]
    public async Task BuildScripts_MissingNavigatorConnection_AddsEnumerableGetter()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<NavigatorConnectionDescriptorResult>("""
            script => {
                const proto = Object.getPrototypeOf(navigator);
                const original = Object.getOwnPropertyDescriptor(proto, 'connection');
                if (original && !original.configurable) {
                    return {
                        removedNativeGetter: false,
                        hasConnection: 'connection' in navigator,
                        hasDescriptor: true,
                        enumerable: original.enumerable
                    };
                }

                delete proto.connection;
                Function(script)();

                const descriptor = Object.getOwnPropertyDescriptor(proto, 'connection');
                return {
                    removedNativeGetter: true,
                    hasConnection: !!navigator.connection,
                    hasDescriptor: !!descriptor,
                    enumerable: !!descriptor && descriptor.enumerable
                };
            }
            """, script);

        await Assert.That(result.RemovedNativeGetter).IsTrue();
        await Assert.That(result.HasConnection).IsTrue();
        await Assert.That(result.HasDescriptor).IsTrue();
        await Assert.That(result.Enumerable).IsTrue();
    }

    [Test]
    public async Task BuildScripts_DefaultConfig_PreservesNativeNavigatorValues()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<NativeNavigatorPreservationResult>("""
            script => {
                const before = {
                    hardwareConcurrency: navigator.hardwareConcurrency,
                    languages: Array.from(navigator.languages || []),
                    connection: navigator.connection ? navigator.connection.effectiveType : '',
                    deviceMemory: navigator.deviceMemory || 0,
                    maxTouchPoints: navigator.maxTouchPoints,
                    pdfViewerEnabled: navigator.pdfViewerEnabled === true,
                    vendor: navigator.vendor
                };

                Function(script)();

                return {
                    beforeHardwareConcurrency: before.hardwareConcurrency,
                    afterHardwareConcurrency: navigator.hardwareConcurrency,
                    beforeLanguages: before.languages.join(','),
                    afterLanguages: Array.from(navigator.languages || []).join(','),
                    beforeConnection: before.connection,
                    afterConnection: navigator.connection ? navigator.connection.effectiveType : '',
                    beforeDeviceMemory: before.deviceMemory,
                    afterDeviceMemory: navigator.deviceMemory || 0,
                    beforeMaxTouchPoints: before.maxTouchPoints,
                    afterMaxTouchPoints: navigator.maxTouchPoints,
                    beforePdfViewerEnabled: before.pdfViewerEnabled,
                    afterPdfViewerEnabled: navigator.pdfViewerEnabled === true,
                    beforeVendor: before.vendor,
                    afterVendor: navigator.vendor
                };
            }
            """, script);

        await Assert.That(result.AfterHardwareConcurrency).IsEqualTo(result.BeforeHardwareConcurrency);
        await Assert.That(result.AfterLanguages).IsEqualTo(result.BeforeLanguages);
        await Assert.That(result.AfterConnection).IsEqualTo(result.BeforeConnection);
        await Assert.That(result.AfterDeviceMemory).IsEqualTo(result.BeforeDeviceMemory);
        await Assert.That(result.AfterMaxTouchPoints).IsEqualTo(result.BeforeMaxTouchPoints);
        await Assert.That(result.AfterPdfViewerEnabled).IsEqualTo(result.BeforePdfViewerEnabled);
        await Assert.That(result.AfterVendor).IsEqualTo(result.BeforeVendor);
    }

    [Test]
    public async Task BuildScripts_ExplicitNavigatorConfig_OverridesConfiguredNavigatorValues()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig
        {
            NavigatorHardwareConcurrency = 12,
            NavigatorDeviceMemory = 16,
            NavigatorMaxTouchPoints = 3,
            Languages = ["uk-UA", "uk"],
            NavigatorPlatformValue = "Linux x86_64",
            NavigatorUserAgentValue = "Mozilla/5.0 ManagedCodeTest/1.0",
            NavigatorVendorValue = "ManagedCode Inc."
        }).Single();

        var result = await page.EvaluateAsync<ExplicitNavigatorConfigResult>("""
            script => {
                Function(script)();

                return {
                    hardwareConcurrency: navigator.hardwareConcurrency,
                    deviceMemory: navigator.deviceMemory,
                    maxTouchPoints: navigator.maxTouchPoints,
                    languages: Array.from(navigator.languages || []).join(','),
                    platform: navigator.platform,
                    userAgent: navigator.userAgent,
                    vendor: navigator.vendor
                };
            }
            """, script);

        await Assert.That(result.HardwareConcurrency).IsEqualTo(12);
        await Assert.That(result.DeviceMemory).IsEqualTo(16);
        await Assert.That(result.MaxTouchPoints).IsEqualTo(3);
        await Assert.That(result.Languages).IsEqualTo("uk-UA,uk");
        await Assert.That(result.Platform).IsEqualTo("Linux x86_64");
        await Assert.That(result.UserAgent).IsEqualTo("Mozilla/5.0 ManagedCodeTest/1.0");
        await Assert.That(result.Vendor).IsEqualTo("ManagedCode Inc.");
    }

    [Test]
    public async Task BuildScripts_NativePluginsPresent_PreservesNativePluginsAndMimeTypes()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<NativePluginMimeTypePreservationResult>("""
            script => {
                const proto = Object.getPrototypeOf(navigator);
                const pluginsDescriptor = Object.getOwnPropertyDescriptor(proto, 'plugins');
                const mimeTypesDescriptor = Object.getOwnPropertyDescriptor(proto, 'mimeTypes');
                if (
                    (pluginsDescriptor && !pluginsDescriptor.configurable) ||
                    (mimeTypesDescriptor && !mimeTypesDescriptor.configurable)
                ) {
                    return { canOverrideNative: false };
                }

                const nativePlugin = {
                    name: 'Native Plugin',
                    filename: 'native-plugin',
                    description: 'Native plugin fixture',
                    length: 1
                };
                const nativeMimeType = {
                    type: 'application/native-fixture',
                    suffixes: 'native',
                    description: 'Native mime fixture',
                    enabledPlugin: nativePlugin
                };
                nativePlugin[0] = nativeMimeType;

                const nativePlugins = [nativePlugin];
                nativePlugins['Native Plugin'] = nativePlugin;
                const nativeMimeTypes = [nativeMimeType];
                nativeMimeTypes['application/native-fixture'] = nativeMimeType;

                Object.defineProperty(proto, 'plugins', {
                    configurable: true,
                    enumerable: true,
                    get() {
                        return nativePlugins;
                    }
                });
                Object.defineProperty(proto, 'mimeTypes', {
                    configurable: true,
                    enumerable: true,
                    get() {
                        return nativeMimeTypes;
                    }
                });

                Function(script)();

                return {
                    canOverrideNative: true,
                    samePluginsObject: navigator.plugins === nativePlugins,
                    sameMimeTypesObject: navigator.mimeTypes === nativeMimeTypes,
                    afterPluginsLength: navigator.plugins ? navigator.plugins.length : 0,
                    afterMimeTypesLength: navigator.mimeTypes ? navigator.mimeTypes.length : 0,
                    afterPluginNames: Array.from(navigator.plugins || []).map(plugin => plugin.name).join('|'),
                    afterMimeTypeNames: Array.from(navigator.mimeTypes || []).map(mimeType => mimeType.type).join('|')
                };
            }
            """, script);

        await Assert.That(result.CanOverrideNative).IsTrue();
        await Assert.That(result.SamePluginsObject).IsTrue();
        await Assert.That(result.SameMimeTypesObject).IsTrue();
        await Assert.That(result.AfterPluginsLength).IsEqualTo(1);
        await Assert.That(result.AfterMimeTypesLength).IsEqualTo(1);
        await Assert.That(result.AfterPluginNames).IsEqualTo("Native Plugin");
        await Assert.That(result.AfterMimeTypeNames).IsEqualTo("application/native-fixture");
    }

    [Test]
    public async Task BuildScripts_EmptyNativePlugins_AddsConsistentPluginAndMimeTypeMocks()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<PluginMimeTypeMockResult>("""
            script => {
                const proto = Object.getPrototypeOf(navigator);
                const pluginsDescriptor = Object.getOwnPropertyDescriptor(proto, 'plugins');
                const mimeTypesDescriptor = Object.getOwnPropertyDescriptor(proto, 'mimeTypes');
                if (
                    (pluginsDescriptor && !pluginsDescriptor.configurable) ||
                    (mimeTypesDescriptor && !mimeTypesDescriptor.configurable)
                ) {
                    return { canOverrideNative: false };
                }

                Object.defineProperty(proto, 'plugins', {
                    configurable: true,
                    enumerable: true,
                    get() {
                        return [];
                    }
                });
                Object.defineProperty(proto, 'mimeTypes', {
                    configurable: true,
                    enumerable: true,
                    get() {
                        return [];
                    }
                });

                Function(script)();

                const patchedPluginsDescriptor = Object.getOwnPropertyDescriptor(proto, 'plugins');
                const patchedMimeTypesDescriptor = Object.getOwnPropertyDescriptor(proto, 'mimeTypes');
                const pdfMimeType = navigator.mimeTypes['application/pdf'];
                const pdfViewerPlugin = navigator.plugins['Chrome PDF Viewer'];

                return {
                    canOverrideNative: true,
                    pluginsLength: navigator.plugins.length,
                    mimeTypesLength: navigator.mimeTypes.length,
                    hasPdfViewerPlugin: !!pdfViewerPlugin,
                    hasPdfMimeType: !!pdfMimeType,
                    pdfMimeTypeHasEnabledPlugin: !!(pdfMimeType && pdfMimeType.enabledPlugin),
                    pdfViewerPluginMimeType: pdfViewerPlugin && pdfViewerPlugin[0]
                        ? pdfViewerPlugin[0].type
                        : '',
                    pluginsDescriptorEnumerable: !!patchedPluginsDescriptor && patchedPluginsDescriptor.enumerable,
                    mimeTypesDescriptorEnumerable: !!patchedMimeTypesDescriptor && patchedMimeTypesDescriptor.enumerable
                };
            }
            """, script);

        await Assert.That(result.CanOverrideNative).IsTrue();
        await Assert.That(result.PluginsLength).IsEqualTo(3);
        await Assert.That(result.MimeTypesLength).IsEqualTo(4);
        await Assert.That(result.HasPdfViewerPlugin).IsTrue();
        await Assert.That(result.HasPdfMimeType).IsTrue();
        await Assert.That(result.PdfMimeTypeHasEnabledPlugin).IsTrue();
        await Assert.That(result.PdfViewerPluginMimeType).IsEqualTo("application/pdf");
        await Assert.That(result.PluginsDescriptorEnumerable).IsTrue();
        await Assert.That(result.MimeTypesDescriptorEnumerable).IsTrue();
    }

    [Test]
    public async Task BuildScripts_NativePdfViewerFalse_PreservesNativeValue()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<NativePdfViewerPreservationResult>("""
            script => {
                const proto = Object.getPrototypeOf(navigator);
                const descriptor = Object.getOwnPropertyDescriptor(proto, 'pdfViewerEnabled');
                if (descriptor && !descriptor.configurable) {
                    return { canOverrideNative: false };
                }

                Object.defineProperty(proto, 'pdfViewerEnabled', {
                    configurable: true,
                    enumerable: true,
                    get() {
                        return false;
                    }
                });

                Function(script)();

                return {
                    canOverrideNative: true,
                    pdfViewerEnabled: navigator.pdfViewerEnabled
                };
            }
            """, script);

        await Assert.That(result.CanOverrideNative).IsTrue();
        await Assert.That(result.PdfViewerEnabled).IsFalse();
    }

    [Test]
    public async Task BuildScripts_MissingPdfViewer_AddsEnumerableGetter()
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(HeadlessLaunchOptions);
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("about:blank");

        var script = StealthScriptProvider.BuildScripts(new StealthConfig()).Single();
        var result = await page.EvaluateAsync<NavigatorPdfViewerDescriptorResult>("""
            script => {
                const proto = Object.getPrototypeOf(navigator);
                const descriptor = Object.getOwnPropertyDescriptor(proto, 'pdfViewerEnabled');
                if (descriptor && !descriptor.configurable) {
                    return {
                        removedNativeGetter: false,
                        hasPdfViewerEnabled: 'pdfViewerEnabled' in navigator,
                        hasDescriptor: true,
                        enumerable: descriptor.enumerable
                    };
                }

                delete proto.pdfViewerEnabled;
                Function(script)();

                const patchedDescriptor = Object.getOwnPropertyDescriptor(proto, 'pdfViewerEnabled');
                return {
                    removedNativeGetter: true,
                    hasPdfViewerEnabled: navigator.pdfViewerEnabled === true,
                    hasDescriptor: !!patchedDescriptor,
                    enumerable: !!patchedDescriptor && patchedDescriptor.enumerable
                };
            }
            """, script);

        await Assert.That(result.RemovedNativeGetter).IsTrue();
        await Assert.That(result.HasPdfViewerEnabled).IsTrue();
        await Assert.That(result.HasDescriptor).IsTrue();
        await Assert.That(result.Enumerable).IsTrue();
    }

    private sealed class CdpConsoleStackProbeResult
    {
        public bool BeforeStealth { get; set; }
        public bool AfterStealth { get; set; }
    }

    private sealed class CdpConsolePerformanceProbeResult
    {
        public bool BeforeTableReceivedLargePayload { get; set; }
        public bool BeforeLogReceivedLargePayload { get; set; }
        public bool AfterTableReceivedLargePayload { get; set; }
        public bool AfterLogReceivedLargePayload { get; set; }
    }

    private sealed class CdpConsoleToStringProbeResult
    {
        public bool BeforeDirectDetected { get; set; }
        public bool BeforeNestedDetected { get; set; }
        public bool AfterDirectDetected { get; set; }
        public bool AfterNestedDetected { get; set; }
    }

    private sealed class CdpConsoleBenignObjectResult
    {
        public bool AccessorObjectPreserved { get; set; }
        public bool ToStringObjectPreserved { get; set; }
        public int GetterCalls { get; set; }
        public int ToStringCalls { get; set; }
    }

    private sealed class NavigatorConnectionDescriptorResult
    {
        public bool RemovedNativeGetter { get; set; }
        public bool HasConnection { get; set; }
        public bool HasDescriptor { get; set; }
        public bool Enumerable { get; set; }
    }

    private sealed class NativeNavigatorPreservationResult
    {
        public int BeforeHardwareConcurrency { get; set; }
        public int AfterHardwareConcurrency { get; set; }
        public string BeforeLanguages { get; set; } = string.Empty;
        public string AfterLanguages { get; set; } = string.Empty;
        public string BeforeConnection { get; set; } = string.Empty;
        public string AfterConnection { get; set; } = string.Empty;
        public int BeforeDeviceMemory { get; set; }
        public int AfterDeviceMemory { get; set; }
        public int BeforeMaxTouchPoints { get; set; }
        public int AfterMaxTouchPoints { get; set; }
        public bool BeforePdfViewerEnabled { get; set; }
        public bool AfterPdfViewerEnabled { get; set; }
        public string BeforeVendor { get; set; } = string.Empty;
        public string AfterVendor { get; set; } = string.Empty;
    }

    private sealed class ExplicitNavigatorConfigResult
    {
        public int HardwareConcurrency { get; set; }
        public int DeviceMemory { get; set; }
        public int MaxTouchPoints { get; set; }
        public string Languages { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
    }

    private sealed class NativePluginMimeTypePreservationResult
    {
        public bool CanOverrideNative { get; set; }
        public bool SamePluginsObject { get; set; }
        public bool SameMimeTypesObject { get; set; }
        public int AfterPluginsLength { get; set; }
        public int AfterMimeTypesLength { get; set; }
        public string AfterPluginNames { get; set; } = string.Empty;
        public string AfterMimeTypeNames { get; set; } = string.Empty;
    }

    private sealed class PluginMimeTypeMockResult
    {
        public bool CanOverrideNative { get; set; }
        public int PluginsLength { get; set; }
        public int MimeTypesLength { get; set; }
        public bool HasPdfViewerPlugin { get; set; }
        public bool HasPdfMimeType { get; set; }
        public bool PdfMimeTypeHasEnabledPlugin { get; set; }
        public string PdfViewerPluginMimeType { get; set; } = string.Empty;
        public bool PluginsDescriptorEnumerable { get; set; }
        public bool MimeTypesDescriptorEnumerable { get; set; }
    }

    private sealed class NativePdfViewerPreservationResult
    {
        public bool CanOverrideNative { get; set; }
        public bool PdfViewerEnabled { get; set; }
    }

    private sealed class NavigatorPdfViewerDescriptorResult
    {
        public bool RemovedNativeGetter { get; set; }
        public bool HasPdfViewerEnabled { get; set; }
        public bool HasDescriptor { get; set; }
        public bool Enumerable { get; set; }
    }

    private sealed class ChromeLoadTimesRegressionResult
    {
        public string Error { get; set; } = string.Empty;
        public bool HasLoadTimes { get; set; }
        public int LoadTimesRequestTime { get; set; }
        public int HardwareConcurrency { get; set; }
    }
}
