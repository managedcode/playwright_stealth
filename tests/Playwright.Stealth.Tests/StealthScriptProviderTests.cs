using System.Collections.Generic;
using System.Linq;
using ManagedCode.Playwright.Stealth;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace ManagedCode.Playwright.Stealth.Tests;

public sealed class StealthScriptProviderTests
{
    [Test]
    public async Task BuildScripts_Should_Include_Default_Scripts()
    {
        var scripts = StealthScriptProvider.BuildScripts(new StealthConfig()).ToList();

        await Assert.That(ContainsScript(scripts, "const utils")).IsTrue();
        await Assert.That(ContainsScript(scripts, "generateMagicArray")).IsTrue();
        await Assert.That(ContainsScript(scripts, "Object.defineProperty(Object.getPrototypeOf(navigator), 'webdriver'")).IsTrue();
        await Assert.That(ContainsScript(scripts, "defineProperty(proto, 'languages'")).IsTrue();
        await Assert.That(ContainsScript(scripts, "HeadlessChrome")).IsFalse();
        await Assert.That(ContainsScript(scripts, "ua_patch_prefix")).IsTrue();
        await Assert.That(ContainsScript(scripts, "patchNavigator('hardwareConcurrency'")).IsTrue();
        await Assert.That(ContainsScript(scripts, "defineProperty(proto, 'deviceMemory'")).IsTrue();
        await Assert.That(ContainsScript(scripts, "effectiveType")).IsTrue();
        await Assert.That(ContainsScript(scripts, "HTMLImageElement.prototype")).IsTrue();
        await Assert.That(ContainsScript(scripts, "speechSynthesis")).IsTrue();
        await Assert.That(ContainsScript(scripts, "screen_width")).IsTrue();
        await Assert.That(ContainsScript(scripts, "__cdp_binding__")).IsTrue();
        await Assert.That(ContainsScript(scripts, "automationPatterns")).IsTrue();
        await Assert.That(ContainsScript(scripts, "maxTouchPoints")).IsTrue();
        await Assert.That(ContainsScript(scripts, "noiseSeed")).IsTrue();
        await Assert.That(ContainsScript(scripts, "requestAnimationFrame")).IsTrue();
        await Assert.That(ContainsScript(scripts, "pdfViewerEnabled")).IsTrue();
        await Assert.That(ContainsScript(scripts, "AudioContext")).IsTrue();
    }

    [Test]
    public async Task BuildScripts_Should_Respect_Disabled_Flags()
    {
        var config = new StealthConfig
        {
            WebDriver = false,
            NavigatorLanguages = false,
            NavigatorUserAgent = false,
            NavigatorHardwareConcurrency = 0,
            WebglVendor = false,
            BrokenImage = false,
            NavigatorConnection = false,
            NavigatorDeviceMemory = 0,
            SpeechSynthesis = false,
            ScreenDimensions = false,
            CdpDetection = false,
            AutomationProperties = false,
            NavigatorMaxTouchPoints = -1,
            CanvasFingerprint = false,
            PerformanceJitter = false,
            NavigatorPdfViewer = false,
            AudioContext = false
        };

        var scripts = StealthScriptProvider.BuildScripts(config).ToList();

        await Assert.That(ContainsScript(scripts, "Object.defineProperty(Object.getPrototypeOf(navigator), 'webdriver'")).IsFalse();
        await Assert.That(ContainsScript(scripts, "defineProperty(proto, 'languages'")).IsFalse();
        await Assert.That(ContainsScript(scripts, "HeadlessChrome")).IsFalse();
        await Assert.That(ContainsScript(scripts, "patchNavigator('hardwareConcurrency'")).IsFalse();
        await Assert.That(ContainsScript(scripts, "UNMASKED_VENDOR_WEBGL")).IsFalse();
        await Assert.That(ContainsScript(scripts, "defineProperty(proto, 'deviceMemory'")).IsFalse();
        await Assert.That(ContainsScript(scripts, "effectiveType")).IsFalse();
        await Assert.That(ContainsScript(scripts, "HTMLImageElement.prototype")).IsFalse();
        await Assert.That(ContainsScript(scripts, "speechSynthesis")).IsFalse();
        await Assert.That(ContainsScript(scripts, "screen_width")).IsFalse();
        await Assert.That(ContainsScript(scripts, "__cdp_binding__")).IsFalse();
        await Assert.That(ContainsScript(scripts, "automationPatterns")).IsFalse();
        await Assert.That(ContainsScript(scripts, "noiseSeed")).IsFalse();
        await Assert.That(ContainsScript(scripts, "pdfViewerEnabled")).IsFalse();
        await Assert.That(ContainsScript(scripts, "AudioContext")).IsFalse();
    }

    private static bool ContainsScript(IEnumerable<string> scripts, string marker)
    {
        return scripts.Any(script => script.Contains(marker, System.StringComparison.Ordinal));
    }
}
