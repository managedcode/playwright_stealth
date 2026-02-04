using ManagedCode.Playwright.Stealth;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace ManagedCode.Playwright.Stealth.Tests;

public sealed class StealthConfigTests
{
    [Test]
    public async Task Defaults_Should_Enable_Core_Stealth_Scripts()
    {
        var config = new StealthConfig();

        await Assert.That(config.WebDriver).IsTrue();
        await Assert.That(config.WebglVendor).IsTrue();
        await Assert.That(config.NavigatorLanguages).IsTrue();
        await Assert.That(config.NavigatorHardwareConcurrency).IsGreaterThan(0);
    }

    [Test]
    public async Task CustomConfig_Should_Preserve_Provided_Values()
    {
        var config = new StealthConfig
        {
            NavigatorHardwareConcurrency = 12,
            NavigatorUserAgentValue = "CustomAgent",
            NavigatorPlatformValue = "Win32",
            NavigatorLanguages = false,
            WebglVendor = false
        };

        await Assert.That(config.NavigatorHardwareConcurrency).IsEqualTo(12);
        await Assert.That(config.NavigatorUserAgentValue).IsEqualTo("CustomAgent");
        await Assert.That(config.NavigatorPlatformValue).IsEqualTo("Win32");
        await Assert.That(config.NavigatorLanguages).IsFalse();
        await Assert.That(config.WebglVendor).IsFalse();
    }
}
