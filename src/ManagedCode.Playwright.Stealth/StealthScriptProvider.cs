using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ManagedCode.Playwright.Stealth;

internal static class StealthScriptProvider
{
    private static readonly IReadOnlyDictionary<string, string> Scripts = new Dictionary<string, string>
    {
        ["chrome_app"] = LoadScript("chrome.app.js"),
        ["chrome_csi"] = LoadScript("chrome.csi.js"),
        ["chrome_hairline"] = LoadScript("chrome.hairline.js"),
        ["chrome_load_times"] = LoadScript("chrome.load.times.js"),
        ["chrome_runtime"] = LoadScript("chrome.runtime.js"),
        ["generate_magic_arrays"] = LoadScript("generate.magic.arrays.js"),
        ["iframe_content_window"] = LoadScript("iframe.contentWindow.js"),
        ["media_codecs"] = LoadScript("media.codecs.js"),
        ["navigator_hardware_concurrency"] = LoadScript("navigator.hardwareConcurrency.js"),
        ["navigator_languages"] = LoadScript("navigator.languages.js"),
        ["navigator_permissions"] = LoadScript("navigator.permissions.js"),
        ["navigator_platform"] = LoadScript("navigator.platform.js"),
        ["navigator_plugins"] = LoadScript("navigator.plugins.js"),
        ["navigator_user_agent"] = LoadScript("navigator.userAgent.js"),
        ["navigator_vendor"] = LoadScript("navigator.vendor.js"),
        ["outerdimensions"] = LoadScript("window.outerdimensions.js"),
        ["utils"] = LoadScript("utils.js"),
        ["webdriver"] = LoadScript("navigator.webdriver.js"),
        ["webgl_vendor"] = LoadScript("webgl.vendor.js")
    };

    public static IEnumerable<string> BuildScripts(StealthConfig config)
    {
        yield return config.BuildOptionsScript();
        yield return Scripts["utils"];
        yield return Scripts["generate_magic_arrays"];

        if (config.ChromeApp)
        {
            yield return Scripts["chrome_app"];
        }

        if (config.ChromeCsi)
        {
            yield return Scripts["chrome_csi"];
        }

        if (config.Hairline)
        {
            yield return Scripts["chrome_hairline"];
        }

        if (config.ChromeLoadTimes)
        {
            yield return Scripts["chrome_load_times"];
        }

        if (config.ChromeRuntime)
        {
            yield return Scripts["chrome_runtime"];
        }

        if (config.IframeContentWindow)
        {
            yield return Scripts["iframe_content_window"];
        }

        if (config.MediaCodecs)
        {
            yield return Scripts["media_codecs"];
        }

        if (config.NavigatorLanguages)
        {
            yield return Scripts["navigator_languages"];
        }

        if (config.NavigatorPermissions)
        {
            yield return Scripts["navigator_permissions"];
        }

        if (config.NavigatorPlatform)
        {
            yield return Scripts["navigator_platform"];
        }

        if (config.NavigatorPlugins)
        {
            yield return Scripts["navigator_plugins"];
        }

        if (config.NavigatorUserAgent)
        {
            yield return Scripts["navigator_user_agent"];
        }

        if (config.NavigatorVendor)
        {
            yield return Scripts["navigator_vendor"];
        }

        if (config.WebDriver)
        {
            yield return Scripts["webdriver"];
        }

        if (config.OuterDimensions)
        {
            yield return Scripts["outerdimensions"];
        }

        if (config.WebglVendor)
        {
            yield return Scripts["webgl_vendor"];
        }

        if (config.NavigatorHardwareConcurrency > 0)
        {
            yield return Scripts["navigator_hardware_concurrency"];
        }
    }

    private static string LoadScript(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.Resources.js.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Missing embedded script resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
