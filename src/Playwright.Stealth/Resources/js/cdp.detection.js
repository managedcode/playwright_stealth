// Mask Chrome DevTools Protocol (CDP) traces
// Bot detectors check for CDP Runtime.enable, Debugger.enable and other fingerprints
try {
    // Remove the Runtime.enable signature that CDP leaves behind
    // CDP injects a binding that can be detected
    const cdpBindings = [
        '__cdp_binding__',
        '__playwright_binding__',
        '__puppeteer_evaluation_script__'
    ]
    for (const binding of cdpBindings) {
        try {
            if (typeof window[binding] !== 'undefined') {
                delete window[binding]
            }
        } catch (e) {}
    }

    // Mask the Debugger detection via Error stack traces
    // CDP leaves traces in error stack that mention DevTools protocol
    const originalError = Error
    const originalCaptureStackTrace = Error.captureStackTrace
    if (originalCaptureStackTrace) {
        Error.captureStackTrace = function(targetObject, constructorOpt) {
            originalCaptureStackTrace.call(Error, targetObject, constructorOpt)
            if (targetObject && targetObject.stack) {
                // Remove any CDP/DevTools references from stack traces
                targetObject.stack = targetObject.stack
                    .split('\n')
                    .filter(line => {
                        const lower = line.toLowerCase()
                        return !lower.includes('devtools') &&
                               !lower.includes('__puppeteer') &&
                               !lower.includes('__playwright') &&
                               !lower.includes('__cdp')
                    })
                    .join('\n')
            }
        }
        try {
            utils.patchToString(Error.captureStackTrace, 'function captureStackTrace() { [native code] }')
        } catch (e) {}
    }

    // Prevent detection via console._commandLineAPI or console.__proto__
    // CDP exposes extra console methods
    try {
        if (console._commandLineAPI) {
            delete console._commandLineAPI
        }
    } catch (e) {}

    // Mask window.Playwright detection
    try {
        if (typeof window.__playwright !== 'undefined') {
            delete window.__playwright
        }
        if (typeof window.__pw_manual !== 'undefined') {
            delete window.__pw_manual
        }
    } catch (e) {}

    // Override the prototype chain for Runtime.enable detection
    // Some detectors check if certain native functions have been modified by CDP
    try {
        const nativeFunctionStr = 'function () { [native code] }'
        const functionsToCheck = [
            'console.log',
            'console.warn',
            'console.error',
            'console.info',
            'console.debug'
        ]
        // Ensure console functions look native
        for (const funcPath of functionsToCheck) {
            const parts = funcPath.split('.')
            const obj = parts[0] === 'console' ? console : window
            const method = parts[1]
            if (obj[method] && typeof obj[method] === 'function') {
                try {
                    utils.patchToString(obj[method])
                } catch (e) {}
            }
        }
    } catch (e) {}
} catch (err) {
}
