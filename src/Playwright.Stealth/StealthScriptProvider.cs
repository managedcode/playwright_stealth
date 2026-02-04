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
        var scripts = new List<string>
        {
            "(function(){",
            config.BuildOptionsScript(),
            Scripts["utils"],
            Scripts["generate_magic_arrays"]
        };

        void AddWrapped(string scriptKey)
        {
            scripts.Add($"(function(){{\n{Scripts[scriptKey]}\n}})();");
        }

        if (config.ChromeApp)
        {
            AddWrapped("chrome_app");
        }

        if (config.ChromeCsi)
        {
            AddWrapped("chrome_csi");
        }

        if (config.Hairline)
        {
            AddWrapped("chrome_hairline");
        }

        if (config.ChromeLoadTimes)
        {
            AddWrapped("chrome_load_times");
        }

        if (config.ChromeRuntime)
        {
            AddWrapped("chrome_runtime");
        }

        if (config.IframeContentWindow)
        {
            AddWrapped("iframe_content_window");
        }

        if (config.MediaCodecs)
        {
            AddWrapped("media_codecs");
        }

        if (config.NavigatorLanguages)
        {
            AddWrapped("navigator_languages");
        }

        if (config.NavigatorPermissions)
        {
            AddWrapped("navigator_permissions");
        }

        if (config.NavigatorPlatform)
        {
            AddWrapped("navigator_platform");
        }

        if (config.NavigatorPlugins)
        {
            AddWrapped("navigator_plugins");
        }

        if (config.NavigatorUserAgent)
        {
            AddWrapped("navigator_user_agent");
        }

        if (config.NavigatorVendor)
        {
            AddWrapped("navigator_vendor");
        }

        if (config.WebDriver)
        {
            AddWrapped("webdriver");
        }

        if (config.OuterDimensions)
        {
            AddWrapped("outerdimensions");
        }

        if (config.WebglVendor)
        {
            AddWrapped("webgl_vendor");
        }

        if (config.NavigatorHardwareConcurrency > 0)
        {
            AddWrapped("navigator_hardware_concurrency");
        }

        scripts.Add("})();");

        yield return string.Join("\n;\n", scripts);
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
