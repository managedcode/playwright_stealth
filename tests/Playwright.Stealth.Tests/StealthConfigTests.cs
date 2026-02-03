using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Playwright.Stealth.Tests;

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
}
