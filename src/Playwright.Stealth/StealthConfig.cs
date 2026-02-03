using System.Collections.Generic;
using System.Text.Json;

namespace ManagedCode.Playwright.Stealth;

public sealed class StealthConfig
{
    public bool WebDriver { get; init; } = true;
    public bool WebglVendor { get; init; } = true;
    public bool ChromeApp { get; init; } = true;
    public bool ChromeCsi { get; init; } = true;
    public bool ChromeLoadTimes { get; init; } = true;
    public bool ChromeRuntime { get; init; } = true;
    public bool IframeContentWindow { get; init; } = true;
    public bool MediaCodecs { get; init; } = true;
    public int NavigatorHardwareConcurrency { get; init; } = 4;
    public bool NavigatorLanguages { get; init; } = true;
    public bool NavigatorPermissions { get; init; } = true;
    public bool NavigatorPlatform { get; init; } = true;
    public bool NavigatorPlugins { get; init; } = true;
    public bool NavigatorUserAgent { get; init; } = true;
    public bool NavigatorVendor { get; init; } = true;
    public bool OuterDimensions { get; init; } = true;
    public bool Hairline { get; init; } = true;

    public string Vendor { get; init; } = "Intel Inc.";
    public string Renderer { get; init; } = "Intel Iris OpenGL Engine";
    public string NavigatorVendorValue { get; init; } = "Google Inc.";
    public string? NavigatorUserAgentValue { get; init; }
    public string? NavigatorPlatformValue { get; init; }
    public IReadOnlyList<string> Languages { get; init; } = new[] { "en-US", "en" };
    public bool? RunOnInsecureOrigins { get; init; }

    internal string BuildOptionsScript()
    {
        var payload = new Dictionary<string, object?>
        {
            ["webgl_vendor"] = Vendor,
            ["webgl_renderer"] = Renderer,
            ["navigator_vendor"] = NavigatorVendorValue,
            ["navigator_platform"] = NavigatorPlatformValue,
            ["navigator_user_agent"] = NavigatorUserAgentValue,
            ["languages"] = Languages,
            ["runOnInsecureOrigins"] = RunOnInsecureOrigins,
            ["navigator_hardware_concurrency"] = NavigatorHardwareConcurrency
        };

        return $"const opts = {JsonSerializer.Serialize(payload)}";
    }
}
