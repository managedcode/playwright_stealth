using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright;

namespace ManagedCode.Playwright.Stealth;

/// <summary>Initializes Chromium pages and workers before their first script runs.</summary>
internal sealed class StealthContextController
{
    private const string BootstrapUrl = "https://stealth-bootstrap.invalid/";
    private static readonly ConditionalWeakTable<IBrowser, Lazy<Task<StealthContextController>>> Controllers = new();
    private readonly IPage transport;
    private readonly ICDPSession requests;
    private readonly ConcurrentDictionary<string, ContextSettings> contexts = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> commands = new();
    private int nextCommandId;

    private StealthContextController(IPage transport, ICDPSession requests)
    {
        this.transport = transport;
        this.requests = requests;
        transport.Close += (_, _) =>
        {
            foreach (TaskCompletionSource<JsonElement> command in commands.Values)
            {
                command.TrySetException(new PlaywrightException("Browser closed during stealth initialization."));
            }
        };
    }

    public static async Task ApplyAsync(IBrowserContext context, StealthConfig config)
    {
        IBrowser? browser = context.Browser;
        if (browser is null || browser.BrowserType.Name != "chromium") return;

        // A locally fulfilled HTTPS page exposes client hints without contacting
        // a server. Close it before returning the application context to its owner.
        IPage bootstrap = await context.NewPageAsync().ConfigureAwait(false);
        NativeIdentity identity;
        string contextId;
        try
        {
            await bootstrap.RouteAsync(BootstrapUrl, route => route.FulfillAsync(new()
            {
                ContentType = "text/html",
                Body = "<!doctype html><title>Stealth initialization</title>"
            })).ConfigureAwait(false);
            await bootstrap.GotoAsync(BootstrapUrl).ConfigureAwait(false);
            string identityJson = await bootstrap.EvaluateAsync<string>("""
                async () => {
                    const metadata = navigator.userAgentData ? {
                        ...navigator.userAgentData.toJSON(),
                        ...await navigator.userAgentData.getHighEntropyValues([
                            'architecture', 'bitness', 'model', 'platformVersion',
                            'uaFullVersion', 'fullVersionList', 'wow64'
                        ])
                    } : null;
                    if (metadata && !navigator.userAgent.includes('HeadlessChrome')) {
                        for (const key of ['brands', 'fullVersionList']) {
                            metadata[key] = metadata[key]?.map(value => ({ ...value,
                                brand: value.brand.replace('HeadlessChrome', 'Google Chrome') }));
                        }
                    }
                    return JSON.stringify({ userAgent: navigator.userAgent, platform: navigator.platform,
                        languages: [...navigator.languages], metadata });
                }
                """).ConfigureAwait(false);
            identity = JsonSerializer.Deserialize<NativeIdentity>(identityJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            ICDPSession pageSession = await context.NewCDPSessionAsync(bootstrap).ConfigureAwait(false);
            try
            {
                JsonElement? target = await pageSession.SendAsync("Target.getTargetInfo").ConfigureAwait(false);
                contextId = target!.Value.GetProperty("targetInfo").GetProperty("browserContextId").GetString()!;
            }
            finally
            {
                await pageSession.DetachAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await bootstrap.CloseAsync().ConfigureAwait(false);
        }

        StealthContextController controller = await Controllers.GetValue(browser, key => new(() => CreateAsync(key)))
            .Value.ConfigureAwait(false);
        ContextSettings settings = new(context, config, identity);
        controller.contexts[contextId] = settings;
        await controller.transport.EvaluateAsync("json => { const settings = JSON.parse(json); window.__stealthWorkerSettings[settings.id] = settings; }",
            JsonSerializer.Serialize(new
            {
                id = contextId,
                script = settings.WorkerScript,
                userAgent = config.NavigatorUserAgent ? BuildUserAgentOverride(settings) : null
            })).ConfigureAwait(false);
        context.Close += (_, _) =>
        {
            controller.contexts.TryRemove(contextId, out _);
            _ = controller.RemoveWorkerSettingsAsync(contextId);
        };
    }

    private static async Task<StealthContextController> CreateAsync(IBrowser browser)
    {
        // Playwright's public CDPSession API cannot address flattened child
        // sessions. Chromium's protocol binding provides that public transport
        // in an isolated, blank context that lives until the browser closes.
        IBrowserContext transportContext = await browser.NewContextAsync(new()
        {
            ServiceWorkers = ServiceWorkerPolicy.Block
        }).ConfigureAwait(false);
        await transportContext.RouteAsync("**/*", route => route.AbortAsync()).ConfigureAwait(false);
        IPage transport = await transportContext.NewPageAsync().ConfigureAwait(false);
        ICDPSession session = await browser.NewBrowserCDPSessionAsync().ConfigureAwait(false);
        StealthContextController controller = new(transport, session);
        try
        {
            await transport.ExposeBindingAsync("__stealthProtocolMessage", (Action<BindingSource, string>)((_, payload) =>
                controller.Receive(payload))).ConfigureAwait(false);
            ICDPSession pageSession = await transportContext.NewCDPSessionAsync(transport).ConfigureAwait(false);
            try
            {
                JsonElement? target = await pageSession.SendAsync("Target.getTargetInfo").ConfigureAwait(false);
                await session.SendAsync("Target.exposeDevToolsProtocol", new()
                {
                    ["targetId"] = target!.Value.GetProperty("targetInfo").GetProperty("targetId").GetString()!,
                    ["bindingName"] = "__stealthProtocol"
                }).ConfigureAwait(false);
                await transport.EvaluateAsync("""
                    () => {
                        const settings = window.__stealthWorkerSettings = Object.create(null);
                        const commands = new Map();
                        let nextId = 0;
                        const send = (sessionId, method, params = {}) => new Promise((resolve, reject) => {
                            const id = --nextId;
                            commands.set(id, { resolve, reject });
                            window.__stealthProtocol.send(JSON.stringify({ id, sessionId, method, params }));
                        });
                        const initialize = async ({ sessionId, targetInfo }) => {
                            try {
                                const workerSettings = settings[targetInfo.browserContextId];
                                const evaluation = send(sessionId, 'Runtime.evaluate', { expression: workerSettings.script });
                                const identity = workerSettings.userAgent ? send(sessionId, 'Network.setUserAgentOverride', workerSettings.userAgent) : Promise.resolve();
                                const [result] = await Promise.all([evaluation, identity]);
                                if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails));
                                await send(sessionId, 'Target.setAutoAttach', {
                                    autoAttach: true, waitForDebuggerOnStart: true, flatten: true,
                                    filter: [{ type: 'worker' }, { exclude: true }]
                                });
                            } catch (error) {
                                window.__stealthProtocolMessage(JSON.stringify({ method: 'Stealth.workerInitializationFailed',
                                    params: { browserContextId: targetInfo.browserContextId, error: error.message } }));
                            } finally {
                                try { await send(sessionId, 'Runtime.runIfWaitingForDebugger'); } catch {}
                            }
                        };
                        window.__stealthProtocol.onmessage = payload => {
                            const message = JSON.parse(payload);
                            const command = commands.get(message.id);
                            if (command) {
                                commands.delete(message.id);
                                if (message.error) command.reject(new Error(message.error.message));
                                else command.resolve(message.result);
                                return;
                            }
                            if (message.method === 'Target.attachedToTarget') {
                                const info = message.params.targetInfo;
                                if (['worker', 'shared_worker', 'service_worker'].includes(info.type) && settings[info.browserContextId]) {
                                    // Send directly from the protocol binding: an extra trip through
                                    // Playwright can let its service-worker session resume first.
                                    initialize(message.params);
                                    return;
                                }
                            }
                            window.__stealthProtocolMessage(payload);
                        };
                    }
                    """).ConfigureAwait(false);
            }
            finally
            {
                await pageSession.DetachAsync().ConfigureAwait(false);
            }

            // Playwright can resume a shared/service worker from its own CDP
            // session. Prepend the same initialization to its original HTTP
            // response so its first script is independent of that scheduling.
            session.Event("Fetch.requestPaused").OnEvent += (_, request) =>
                _ = controller.PrepareWorkerResponseAsync(request!.Value.Clone());
            await session.SendAsync("Fetch.enable", new()
            {
                ["patterns"] = new[] { new { resourceType = "Other", requestStage = "Response" } }
            }).ConfigureAwait(false);

            await controller.SendAsync(null, "Target.setAutoAttach", new()
            {
                ["autoAttach"] = true,
                ["waitForDebuggerOnStart"] = true,
                ["flatten"] = true,
                ["filter"] = new object[]
                {
                    new { type = "page" }, new { type = "shared_worker" },
                    new { type = "service_worker" }, new { exclude = true }
                }
            }).ConfigureAwait(false);
            return controller;
        }
        catch
        {
            await session.DetachAsync().ConfigureAwait(false);
            await transportContext.CloseAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task PrepareWorkerResponseAsync(JsonElement data)
    {
        string requestId = data.GetProperty("requestId").GetString()!;
        bool fulfilled = false;
        ContextSettings? settings = null;
        try
        {
            if (!data.TryGetProperty("responseStatusCode", out JsonElement status) || status.GetInt32() != 200) return;
            JsonElement[] headers = data.GetProperty("responseHeaders").EnumerateArray().Select(header => header.Clone()).ToArray();
            if (!headers.Any(header => header.GetProperty("name").GetString()!.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)
                && (header.GetProperty("value").GetString()!.Contains("javascript", StringComparison.OrdinalIgnoreCase)
                    || header.GetProperty("value").GetString()!.Contains("ecmascript", StringComparison.OrdinalIgnoreCase)))) return;

            string? targetId = data.TryGetProperty("networkId", out JsonElement network) && Guid.TryParseExact(network.GetString(), "N", out _)
                ? network.GetString() : data.GetProperty("frameId").GetString();
            if (string.IsNullOrEmpty(targetId)) return;
            JsonElement? target = await requests.SendAsync("Target.getTargetInfo", new() { ["targetId"] = targetId }).ConfigureAwait(false);
            JsonElement info = target!.Value.GetProperty("targetInfo");
            if (info.GetProperty("type").GetString() is not ("shared_worker" or "service_worker")
                || !info.TryGetProperty("browserContextId", out JsonElement contextId)
                || !contexts.TryGetValue(contextId.GetString()!, out settings)) return;

            JsonElement? response = await requests.SendAsync("Fetch.getResponseBody", new() { ["requestId"] = requestId }).ConfigureAwait(false);
            string body = response!.Value.GetProperty("body").GetString()!;
            if (response.Value.GetProperty("base64Encoded").GetBoolean()) body = Encoding.UTF8.GetString(Convert.FromBase64String(body));
            string initialized = InsertWorkerInitialization(body, settings.WorkerScript);
            await requests.SendAsync("Fetch.fulfillRequest", new()
            {
                ["requestId"] = requestId,
                ["responseCode"] = status.GetInt32(),
                ["responseHeaders"] = headers.Where(header => !header.GetProperty("name").GetString()!.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
                    && !header.GetProperty("name").GetString()!.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase)).ToArray(),
                ["body"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(initialized))
            }).ConfigureAwait(false);
            fulfilled = true;
        }
        catch (PlaywrightException error) when (IsClosedTarget(error)) { }
        catch (Exception error)
        {
            if (settings is not null)
                await settings.Context.CloseAsync(new() { Reason = $"Stealth worker response initialization failed: {error.Message}" }).ConfigureAwait(false);
        }
        finally
        {
            if (!fulfilled)
            {
                try { await requests.SendAsync("Fetch.continueRequest", new() { ["requestId"] = requestId }).ConfigureAwait(false); }
                catch (PlaywrightException error) when (IsClosedTarget(error)) { }
            }
        }
    }

    // Preserve JavaScript's directive prologue, including automatic semicolon
    // insertion and comments. Initialization must not turn a string literal
    // into a function call or disable a classic worker's strict mode.
    private static string InsertWorkerInitialization(string body, string script)
    {
        int position = 0;
        if (body.StartsWith("#!", StringComparison.Ordinal))
        {
            while (position < body.Length && !IsLineBreak(body[position])) position++;
        }
        int prologueEnd = position;
        while (position < body.Length)
        {
            SkipTrivia(body, ref position);
            if (position >= body.Length || body[position] is not ('\'' or '"')) break;
            char quote = body[position++];
            bool closed = false;
            while (position < body.Length)
            {
                char value = body[position++];
                if (value == quote) { closed = true; break; }
                if (value == '\\' && position < body.Length)
                {
                    if (body[position++] == '\r' && position < body.Length && body[position] == '\n') position++;
                    continue;
                }
                if (IsLineBreak(value)) break;
            }
            if (!closed) break;
            bool lineBreak = SkipTrivia(body, ref position);
            if (position < body.Length && body[position] == ';') position++;
            else if (position < body.Length && (!lineBreak || ContinuesStringExpression(body, position))) break;
            prologueEnd = position;
        }
        return body[..prologueEnd] + "\n;" + script + "\n" + body[prologueEnd..];
    }

    private static bool SkipTrivia(string body, ref int position)
    {
        bool lineBreak = false;
        while (position < body.Length)
        {
            char value = body[position];
            if (char.IsWhiteSpace(value) || value == '\uFEFF')
            {
                lineBreak |= IsLineBreak(value);
                position++;
            }
            else if (body.AsSpan(position).StartsWith("//", StringComparison.Ordinal))
            {
                while (position < body.Length && !IsLineBreak(body[position])) position++;
            }
            else if (body.AsSpan(position).StartsWith("/*", StringComparison.Ordinal))
            {
                position += 2;
                while (position < body.Length && !body.AsSpan(position).StartsWith("*/", StringComparison.Ordinal))
                    lineBreak |= IsLineBreak(body[position++]);
                position = Math.Min(position + 2, body.Length);
            }
            else break;
        }
        return lineBreak;
    }

    private static bool ContinuesStringExpression(string body, int position)
    {
        char value = body[position];
        if (value is '+' or '-') return position + 1 >= body.Length || body[position + 1] != value;
        if (value == '!') return position + 1 < body.Length && body[position + 1] == '=';
        if ("([.,?/*%<>=&|^`".Contains(value)) return true;
        foreach (string keyword in new[] { "in", "instanceof" })
        {
            if (body.AsSpan(position).StartsWith(keyword, StringComparison.Ordinal)
                && (position + keyword.Length == body.Length
                    || !(char.IsLetterOrDigit(body[position + keyword.Length]) || body[position + keyword.Length] is '_' or '$')))
                return true;
        }
        return false;
    }

    private static bool IsLineBreak(char value) => value is '\r' or '\n' or '\u2028' or '\u2029';

    private void Receive(string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement message = document.RootElement;
        if (message.TryGetProperty("id", out JsonElement id))
        {
            if (commands.TryRemove(id.GetInt32(), out TaskCompletionSource<JsonElement>? completion))
            {
                if (message.TryGetProperty("error", out JsonElement error))
                    completion.TrySetException(new PlaywrightException(error.GetProperty("message").GetString()!));
                else
                    completion.TrySetResult(message.GetProperty("result").Clone());
            }
        }
        else if (message.TryGetProperty("method", out JsonElement method))
        {
            if (method.GetString() == "Target.attachedToTarget")
                _ = InitializeAsync(message.GetProperty("params").Clone());
            else if (method.GetString() == "Stealth.workerInitializationFailed")
            {
                JsonElement error = message.GetProperty("params");
                if (contexts.TryGetValue(error.GetProperty("browserContextId").GetString()!, out ContextSettings? settings)
                    && !IsClosedTarget(new PlaywrightException(error.GetProperty("error").GetString()!)))
                    _ = settings.Context.CloseAsync(new() { Reason = $"Stealth worker initialization failed: {error.GetProperty("error").GetString()}" });
            }
        }
    }

    private async Task InitializeAsync(JsonElement data)
    {
        string sessionId = data.GetProperty("sessionId").GetString()!;
        JsonElement info = data.GetProperty("targetInfo");
        string type = info.GetProperty("type").GetString()!;
        ContextSettings? settings = info.TryGetProperty("browserContextId", out JsonElement id)
            && contexts.TryGetValue(id.GetString()!, out ContextSettings? value) ? value : null;
        try
        {
            if (settings is not null)
            {
                if (type == "page")
                {
                    if (settings.Config.NavigatorUserAgent)
                    {
                        await SendAsync(sessionId, "Emulation.setUserAgentOverride", BuildUserAgentOverride(settings)).ConfigureAwait(false);
                    }
                    if (settings.Config.NavigatorHardwareConcurrency > 0)
                    {
                        await SendAsync(sessionId, "Emulation.setHardwareConcurrencyOverride", new()
                        {
                            ["hardwareConcurrency"] = settings.Config.NavigatorHardwareConcurrency
                        }).ConfigureAwait(false);
                    }
                }

                await SendAsync(sessionId, "Target.setAutoAttach", new()
                {
                    ["autoAttach"] = true,
                    ["waitForDebuggerOnStart"] = true,
                    ["flatten"] = true,
                    ["filter"] = new object[]
                    {
                        new { type = "worker" }, new { exclude = true }
                    }
                }).ConfigureAwait(false);
            }
        }
        catch (PlaywrightException error) when (IsClosedTarget(error)) { }
        catch (Exception error)
        {
            if (settings is not null)
            {
                await settings.Context.CloseAsync(new()
                {
                    Reason = $"Chromium stealth initialization failed: {error.Message}"
                }).ConfigureAwait(false);
            }
        }
        finally
        {
            try
            {
                await SendAsync(sessionId, "Runtime.runIfWaitingForDebugger", []).ConfigureAwait(false);
            }
            catch (PlaywrightException error) when (IsClosedTarget(error)) { }
        }
    }

    private static Dictionary<string, object> BuildUserAgentOverride(ContextSettings settings)
    {
        Dictionary<string, object> identity = new()
        {
            ["userAgent"] = settings.Identity.UserAgent,
            ["platform"] = settings.Identity.Platform,
            ["acceptLanguage"] = string.Join(",", settings.Identity.Languages)
        };
        if (settings.Identity.Metadata.ValueKind == JsonValueKind.Object)
            identity["userAgentMetadata"] = settings.Identity.Metadata;
        return identity;
    }

    private async Task RemoveWorkerSettingsAsync(string contextId)
    {
        try
        {
            await transport.EvaluateAsync("id => { delete window.__stealthWorkerSettings[id]; }", contextId).ConfigureAwait(false);
        }
        catch (PlaywrightException error) when (IsClosedTarget(error)) { }
    }

    private static bool IsClosedTarget(PlaywrightException error) =>
        error.Message.Contains("closed", StringComparison.OrdinalIgnoreCase)
        || error.Message.Contains("No session", StringComparison.OrdinalIgnoreCase)
        || error.Message.Contains("No target", StringComparison.OrdinalIgnoreCase)
        || error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase);

    private async Task<JsonElement> SendAsync(string? sessionId, string method, Dictionary<string, object> parameters)
    {
        int id = Interlocked.Increment(ref nextCommandId);
        TaskCompletionSource<JsonElement> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        commands[id] = completion;
        Dictionary<string, object> message = new() { ["id"] = id, ["method"] = method, ["params"] = parameters };
        if (sessionId is not null) message["sessionId"] = sessionId;
        try
        {
            await transport.EvaluateAsync("message => window.__stealthProtocol.send(message)",
                JsonSerializer.Serialize(message)).ConfigureAwait(false);
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        finally
        {
            commands.TryRemove(id, out _);
        }
    }

    private sealed record ContextSettings(IBrowserContext Context, StealthConfig Config, NativeIdentity Identity)
    {
        public string WorkerScript { get; } = StealthScriptProvider.BuildWorkerScript(Config, Identity.Languages);
    }

    private sealed class NativeIdentity
    {
        public string UserAgent { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string[] Languages { get; set; } = [];
        public JsonElement Metadata { get; set; }
    }
}
