using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ManagedCode.Playwright.Stealth;
using Microsoft.Playwright;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace ManagedCode.Playwright.Stealth.Tests;

public sealed class StealthCompatibilityTests
{
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ApplyStealth_DefaultConfig_PreservesNativeRenderingApis(bool headless)
    {
        await WithBrowserAsync(headless, async browser =>
        {
            await using IBrowserContext context = await browser.NewContextAsync(new() { Locale = "uk-UA" });
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("about:blank");
            string script = StealthScriptProvider.BuildScripts(new()).Single();
            string[] changed = await page.EvaluateAsync<string[]>("""
                script => {
                    const descriptors = [
                        [CanvasRenderingContext2D.prototype, 'getImageData'],
                        [HTMLCanvasElement.prototype, 'toDataURL'],
                        [HTMLCanvasElement.prototype, 'toBlob'],
                        [AudioBuffer.prototype, 'getChannelData'],
                        [OfflineAudioContext.prototype, 'startRendering'],
                        [Error, 'captureStackTrace'],
                        [Screen.prototype, 'width'], [Screen.prototype, 'height'],
                        [Screen.prototype, 'availWidth'], [Screen.prototype, 'availHeight'],
                        [Screen.prototype, 'colorDepth'], [Screen.prototype, 'pixelDepth'],
                        [HTMLImageElement.prototype, 'naturalWidth'],
                        [HTMLImageElement.prototype, 'naturalHeight'],
                        [HTMLIFrameElement.prototype, 'contentWindow'],
                        [SpeechSynthesis.prototype, 'getVoices'],
                        [Permissions.prototype, 'query'],
                        [Navigator.prototype, 'webdriver'],
                        [WebGLRenderingContext.prototype, 'getParameter'],
                        [WebGL2RenderingContext.prototype, 'getParameter']
                    ];
                    const before = descriptors.map(([object, key]) => Object.getOwnPropertyDescriptor(object, key));
                    const screenBefore = [screen.width, screen.height, screen.availWidth, screen.availHeight];
                    Function(script)();
                    const changed = descriptors.flatMap(([object, key], index) => {
                        const after = Object.getOwnPropertyDescriptor(object, key);
                        return ['value', 'get', 'set', 'enumerable', 'writable', 'configurable']
                            .some(field => before[index]?.[field] !== after?.[field]) ? [key] : [];
                    });
                    if (String(screenBefore) !== String([screen.width, screen.height, screen.availWidth, screen.availHeight])) {
                        changed.push('screen values');
                    }
                    return changed;
                }
                """, script);
            await Assert.That(changed).IsEmpty();
        });
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task LaunchStealth_DefaultOptions_RendersWebGlInPageAndWorker(bool headless)
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using IPlaywright playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        (IBrowser Browser, IBrowserContext Context) launched = await playwright.Chromium.LaunchStealthAsync(
            launchOptions: new() { Headless = headless });
        await using IBrowser browser = launched.Browser;
        await using IBrowserContext context = launched.Context;
        IPage page = await context.NewPageAsync();
        string[] failures = await page.EvaluateAsync<string[]>("""
            requireNativeOpenGl => (async () => {
                const inspect = () => {
                    const results = [];
                    for (const type of ['webgl', 'webgl2']) {
                        const gl = new OffscreenCanvas(1, 1).getContext(type);
                        if (!gl) throw new Error(`${type} unavailable`);
                        const extension = gl.getExtension('WEBGL_debug_renderer_info');
                        if (!extension) throw new Error(`${type} renderer unavailable`);
                        gl.clearColor(1, 0, 0, 1);
                        gl.clear(gl.COLOR_BUFFER_BIT);
                        const pixel = new Uint8Array(4);
                        gl.readPixels(0, 0, 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, pixel);
                        if (String(pixel) !== '255,0,0,255') throw new Error(`${type} pixel: ${pixel}`);
                        results.push({ type, vendor: gl.getParameter(extension.UNMASKED_VENDOR_WEBGL),
                            renderer: gl.getParameter(extension.UNMASKED_RENDERER_WEBGL) });
                    }
                    return results;
                };
                const main = inspect();
                if (requireNativeOpenGl && main.some(value => /SwiftShader/i.test(value.renderer))) {
                    return [`Native EGL driver unavailable: ${JSON.stringify(main)}`];
                }
                const url = URL.createObjectURL(new Blob([
                    `const inspect = ${inspect}; try { postMessage({ results: inspect() }); }
                     catch (error) { postMessage({ error: error.message }); }`
                ], { type: 'application/javascript' }));
                const worker = new Worker(url);
                try {
                    const result = await new Promise((resolve, reject) => {
                        const timer = setTimeout(() => reject(new Error('WebGL worker timed out')), 20_000);
                        worker.onmessage = event => { clearTimeout(timer); resolve(event.data); };
                        worker.onerror = event => { clearTimeout(timer); reject(new Error(event.message)); };
                    });
                    if (result.error) return [result.error];
                    return JSON.stringify(main) === JSON.stringify(result.results)
                        ? [] : [`WebGL page/worker mismatch: ${JSON.stringify({ main, worker: result.results })}`];
                } finally {
                    worker.terminate();
                    URL.revokeObjectURL(url);
                }
            })()
            """, OperatingSystem.IsLinux());
        await Assert.That(failures).IsEmpty();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ApplyStealth_PerformanceJitterDisabled_PreservesNativeClock(bool headless)
    {
        await WithBrowserAsync(headless, async browser =>
        {
            await using IBrowserContext context = await browser.NewContextAsync();
            IPage page = await context.NewPageAsync();
            string script = StealthScriptProvider.BuildScripts(new() { PerformanceJitter = false }).Single();
            bool preserved = await page.EvaluateAsync<bool>("""
                script => {
                    const before = Object.getOwnPropertyDescriptor(Performance.prototype, 'now');
                    Function(script)();
                    const after = Object.getOwnPropertyDescriptor(Performance.prototype, 'now');
                    return ['value', 'get', 'set', 'enumerable', 'writable', 'configurable']
                        .every(field => before[field] === after[field]);
                }
                """, script);
            await Assert.That(preserved).IsTrue();
        });
    }

    [Test]
    [Arguments(true, false)]
    [Arguments(false, false)]
    [Arguments(true, true)]
    [Arguments(false, true)]
    public async Task ApplyStealth_PerformanceClock_RemainsMonotonicAndPreservesNativeErrors(bool headless, bool reducePrecision)
    {
        await WithBrowserAsync(headless, async browser =>
        {
            await using IBrowserContext context = await browser.NewContextAsync();
            await context.ApplyStealthAsync(new() { PerformanceJitter = reducePrecision });
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("about:blank");
            string[] failures = await page.EvaluateAsync<string[]>("""
                reducePrecision => {
                    const failures = [];
                    const now = Performance.prototype.now;
                    try { now.call({}); failures.push('accepted invalid receiver'); }
                    catch (error) { if (!(error instanceof TypeError)) failures.push(error.name); }
                    try { new now(); failures.push('constructible clock'); }
                    catch (error) { if (!(error instanceof TypeError)) failures.push(error.name); }
                    let previous = performance.now();
                    const deadline = Date.now() + 30;
                    while (Date.now() < deadline) {
                        const current = performance.now();
                        if (reducePrecision && current % 10 !== 0) { failures.push('10ms clock precision option ignored'); break; }
                        if (current < previous) { failures.push('clock moved backwards'); break; }
                        previous = current;
                    }
                    if (now.name !== 'now' || now.length !== 0) failures.push('native clock signature');
                    if (!now.toString().includes('[native code]')) failures.push('native clock source');
                    if (Math.abs(performance.timeOrigin + performance.now() - Date.now()) > 1000) failures.push('clock time origin');
                    return failures;
                }
                """, reducePrecision);
            await Assert.That(failures).IsEmpty();
        });
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ApplyStealth_ToStringReceivers_PreservesNativeErrorsWithoutRecursion(bool headless)
    {
        await WithBrowserAsync(headless, async browser =>
        {
            await using IBrowserContext context = await browser.NewContextAsync();
            await context.ApplyStealthAsync(new() { NavigatorDeviceMemory = 16 });
            await context.ApplyStealthAsync(new() { NavigatorDeviceMemory = 16 });
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("about:blank");
            string[] failures = await page.EvaluateAsync<string[]>("""
                () => {
                    const failures = [];
                    const stringify = Function.prototype.toString;
                    let getterCalls = 0;
                    const receiver = { get toString() { getterCalls++; throw new Error('accessed'); } };
                    for (const value of [null, undefined, receiver, Object.create(stringify)]) {
                        try { stringify.call(value); failures.push('accepted invalid receiver'); }
                        catch (error) { if (!(error instanceof TypeError)) failures.push(error.name); }
                    }
                    if (getterCalls) failures.push('read toString accessor');
                    const custom = function receiver() {};
                    const originalSource = stringify.call(custom);
                    Object.setPrototypeOf(custom, { toString() { getterCalls++; throw new Error('custom prototype'); } });
                    if (stringify.call(custom) !== originalSource || getterCalls) failures.push('called custom prototype toString');
                    const proto = Object.getPrototypeOf(stringify);
                    try {
                        if (Reflect.setPrototypeOf(stringify, Object.create(stringify))) failures.push('accepted cycle');
                    } finally { Object.setPrototypeOf(stringify, proto); }
                    if ('prototype' in stringify) failures.push('constructible toString');
                    if (!stringify.call(stringify).includes('[native code]')) failures.push('toString source');
                    const iframe = document.createElement('iframe');
                    document.body.appendChild(iframe);
                    const foreignStringify = iframe.contentWindow.Function.prototype.toString;
                    for (const fn of [stringify, Function.prototype.call.toString, Performance.prototype.now]) {
                        const source = foreignStringify.call(fn);
                        if (!/^function\b[^()]*\(\)\s*\{\s*\[native code\]\s*\}$/.test(source)) failures.push('cross-frame native source');
                    }
                    iframe.remove();
                    for (const key of ['deviceMemory', 'hardwareConcurrency', 'webdriver']) {
                        const getter = Object.getOwnPropertyDescriptor(Navigator.prototype, key)?.get;
                        if (!getter) continue;
                        try { getter.call({}); failures.push(`${key} accepted invalid receiver`); }
                        catch (error) { if (!(error instanceof TypeError)) failures.push(error.name); }
                    }
                    return failures;
                }
                """);
            await Assert.That(failures).IsEmpty();
        });
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ApplyStealth_SrcdocFrame_PreservesNativeFrameBehavior(bool headless)
    {
        await WithBrowserAsync(headless, async browser =>
        {
            await using IBrowserContext context = await browser.NewContextAsync();
            await context.ApplyStealthAsync();
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("about:blank");
            bool correct = await page.EvaluateAsync<bool>("""
                async () => {
                    const iframe = document.createElement('iframe');
                    iframe.srcdoc = '<p id="loaded">frame content</p>';
                    if (iframe.contentWindow !== null || iframe.hasOwnProperty('srcdoc') || iframe.hasOwnProperty('contentWindow')) return false;
                    const loaded = new Promise(resolve => iframe.onload = resolve);
                    document.body.appendChild(iframe);
                    await loaded;
                    return iframe.contentWindow.self === iframe.contentWindow &&
                        iframe.contentWindow.frameElement === iframe &&
                        iframe.contentDocument.querySelector('#loaded').textContent === 'frame content';
                }
                """);
            await Assert.That(correct).IsTrue();
        });
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task ApplyStealth_FirstWorkerScript_MatchesPageAndRequestIdentity(bool headless)
    {
        await WithBrowserAsync(headless, async browser =>
        {
            await using WorkerTestServer server = new();
            await using IBrowserContext context = await browser.NewContextAsync(new() { Locale = "uk-UA" });
            await context.ApplyStealthAsync(new() { NavigatorHardwareConcurrency = 18 });
            IPage page = await context.NewPageAsync();
            page.PageError += (_, error) => Console.WriteLine($"Worker page error: {error}");
            page.RequestFailed += (_, request) => Console.WriteLine($"Worker request failed: {request.Url}: {request.Failure}");
            await page.GotoAsync(server.Url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForFunctionAsync("() => window.workerSnapshots.then(() => true)", options: new() { Timeout = 30_000 });
            string[] failures = await page.EvaluateAsync<string[]>("""
                async () => {
                    const values = await window.workerSnapshots;
                    const page = window.navigatorSnapshot();
                    const failures = [];
                    for (let i = 0; i < values.length; i++) {
                        for (const key of ['userAgent', 'platform', 'hardwareConcurrency', 'language', 'languages']) {
                            if (JSON.stringify(values[i][key]) !== JSON.stringify(page[key])) {
                                failures.push(`worker ${i}: ${key}: ${JSON.stringify(values[i][key])} != ${JSON.stringify(page[key])}`);
                            }
                        }
                        if (values[i].cdp) failures.push(`worker ${i}: CDP console getter`);
                        if (!values[i].strict) failures.push(`worker ${i}: lost strict mode`);
                        if (!values[i].url.startsWith(location.origin + '/') || values[i].url.startsWith('blob:')) failures.push(`worker ${i}: changed script URL`);
                        if (values[i].headers.userAgent !== page.userAgent) failures.push(`worker ${i}: HTTP user agent ${values[i].headers.userAgent}`);
                    }
                    if (page.hardwareConcurrency !== 18) failures.push('page hardware concurrency');
                    if (page.userAgent.includes('Headless')) failures.push('headless user agent');
                    const response = await fetch('/headers');
                    const headers = await response.json();
                    if (headers.userAgent !== navigator.userAgent) failures.push('HTTP user agent');
                    const hints = navigator.userAgentData;
                    if (hints && (!hints.brands.length || !hints.platform)) failures.push('missing client hints');
                    if (hints?.brands.some(value => value.brand.includes('Headless'))) failures.push('headless client hints');
                    return failures;
                }
                """);
            await Assert.That(failures).IsEmpty();

            await using IBrowserContext plain = await browser.NewContextAsync();
            IPage plainPage = await plain.NewPageAsync();
            await plainPage.GotoAsync(server.Url + "plain");
            int concurrency = await plainPage.EvaluateAsync<int>("navigator.hardwareConcurrency");
            await Assert.That(concurrency).IsNotEqualTo(18);
            bool workerIsolated = await plainPage.EvaluateAsync<bool>("""
                () => new Promise(resolve => {
                    const worker = new SharedWorker('/shared.js');
                    worker.port.onmessage = event => {
                        const value = event.data;
                        resolve(value.hardwareConcurrency === navigator.hardwareConcurrency &&
                            value.userAgent === navigator.userAgent && value.headers.userAgent === navigator.userAgent &&
                            JSON.stringify(value.languages) === JSON.stringify(navigator.languages));
                        worker.port.close();
                    };
                    worker.port.start();
                })
                """);
            await Assert.That(workerIsolated).IsTrue();
        });
    }

    private static async Task WithBrowserAsync(bool headless, Func<IBrowser, Task> action)
    {
        await PlaywrightInstall.EnsureInstalledAsync();
        using IPlaywright playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using IBrowser browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = headless,
            Channel = "chromium",
            Args = PlaywrightStealthExtensions.StealthArgs
        });
        await action(browser);
    }

    // Real localhost responses also exercise service-worker registration, which
    // cannot be replaced with a page route or an after-start worker evaluation.
    private sealed class WorkerTestServer : IAsyncDisposable
    {
        private const string SnapshotScript = """
            // Keep directives and comments intact when initializing the worker.
            'worker compatibility';
            'use strict' // Exercise automatic semicolon insertion.
            /* A comment between the directive and the first statement. */
            const snapshot = () => ({ userAgent: navigator.userAgent, platform: navigator.platform,
                hardwareConcurrency: navigator.hardwareConcurrency, language: navigator.language,
                languages: [...navigator.languages] });
            const probe = new Error('worker CDP probe');
            let cdp = false;
            Object.defineProperty(probe, 'stack', { get() { cdp = true; return ''; } });
            console.log(probe);
            const result = { ...snapshot(), cdp, strict: (function() { return this === undefined; })(), url: self.location.href };
            const resultWithHeaders = fetch('/headers').then(response => response.json()).then(headers => ({ ...result, headers }));
            """;
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stopping = new();
        private readonly Task serving;
        public string Url { get; }

        public WorkerTestServer()
        {
            listener.Start();
            Url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
            serving = ServeAsync();
        }

        private readonly List<Task> requests = [];

        private async Task ServeAsync()
        {
            try
            {
                while (!stopping.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(stopping.Token);
                    requests.Add(HandleAsync(client));
                }
            }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
        }

        private async Task HandleAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    await using NetworkStream stream = client.GetStream();
                    using StreamReader reader = new(stream, Encoding.ASCII, leaveOpen: true);
                    string? request = await reader.ReadLineAsync(stopping.Token);
                    string path = request?.Split(' ')[1] ?? "/";
                    string userAgent = string.Empty;
                    while (await reader.ReadLineAsync(stopping.Token) is { Length: > 0 } header)
                    {
                        if (header.StartsWith("User-Agent: ", StringComparison.OrdinalIgnoreCase)) userAgent = header[12..];
                    }
                    string body = path switch
                    {
                        "/headers" => JsonSerializer.Serialize(new { userAgent }),
                        "/classic.js" => SnapshotScript + "resultWithHeaders.then(value => postMessage(value));",
                        "/module.js" => "import './classic.js';",
                        "/shared.js" => SnapshotScript + "onconnect = event => resultWithHeaders.then(value => event.ports[0].postMessage(value));",
                        "/service-worker.js" => SnapshotScript + """
                            self.addEventListener('install', () => self.skipWaiting());
                            self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));
                            self.addEventListener('message', event => event.waitUntil(resultWithHeaders.then(value => event.source.postMessage(value))));
                            """,
                        "/plain" => "<!doctype html><title>Unmodified context</title>",
                        _ => """
                            <!doctype html><title>Worker compatibility</title><script>
                            window.navigatorSnapshot = () => ({ userAgent: navigator.userAgent, platform: navigator.platform,
                                hardwareConcurrency: navigator.hardwareConcurrency, language: navigator.language,
                                languages: [...navigator.languages] });
                            const worker = (url, options) => new Promise((resolve, reject) => {
                                const instance = new Worker(url, options);
                                instance.onmessage = event => { resolve(event.data); instance.terminate(); };
                                instance.onerror = event => reject(new Error(`${url}: ${event.message}`));
                            });
                            const shared = new Promise((resolve, reject) => {
                                const instance = new SharedWorker('/shared.js');
                                instance.port.onmessage = event => { resolve(event.data); instance.port.close(); };
                                instance.onerror = event => reject(new Error(`shared worker: ${event.message}`));
                                instance.port.start();
                            });
                            const service = new Promise(async (resolve, reject) => {
                                navigator.serviceWorker.onmessage = event => resolve(event.data);
                                try {
                                    await navigator.serviceWorker.register('/service-worker.js');
                                    const registration = await navigator.serviceWorker.ready;
                                    registration.active.postMessage('snapshot');
                                } catch (error) { reject(error); }
                            });
                            window.workerSnapshots = Promise.all([worker('/classic.js'), worker('/module.js', { type: 'module' }), shared, service]);
                            </script>
                            """
                    };
                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    string contentType = path.EndsWith(".js", StringComparison.Ordinal) ? "application/javascript" : "text/html";
                    byte[] headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(headers, stopping.Token);
                    await stream.WriteAsync(bytes, stopping.Token);
                }
                catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await stopping.CancelAsync();
            listener.Stop();
            await serving;
            await Task.WhenAll(requests);
            stopping.Dispose();
        }
    }
}
