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
        await Assert.That(ContainsScript(scripts, "HeadlessChrome")).IsTrue();
        await Assert.That(ContainsScript(scripts, "patchNavigator('hardwareConcurrency'")).IsTrue();
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
            WebglVendor = false
        };

        var scripts = StealthScriptProvider.BuildScripts(config).ToList();

        await Assert.That(ContainsScript(scripts, "Object.defineProperty(Object.getPrototypeOf(navigator), 'webdriver'")).IsFalse();
        await Assert.That(ContainsScript(scripts, "defineProperty(proto, 'languages'")).IsFalse();
        await Assert.That(ContainsScript(scripts, "HeadlessChrome")).IsFalse();
        await Assert.That(ContainsScript(scripts, "patchNavigator('hardwareConcurrency'")).IsFalse();
        await Assert.That(ContainsScript(scripts, "UNMASKED_VENDOR_WEBGL")).IsFalse();
    }

    private static bool ContainsScript(IEnumerable<string> scripts, string marker)
    {
        return scripts.Any(script => script.Contains(marker, System.StringComparison.Ordinal));
    }
}
