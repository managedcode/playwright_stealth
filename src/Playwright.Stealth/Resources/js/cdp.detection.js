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

    // Prevent detection via console._commandLineAPI or console.__proto__
    // CDP exposes extra console methods
    try {
        if (console._commandLineAPI) {
            delete console._commandLineAPI
        }
    } catch (e) {}

    // Mask globalThis.Playwright detection
    try {
        if (typeof globalThis.__playwright !== 'undefined') {
            delete globalThis.__playwright
        }
        if (typeof globalThis.__pw_manual !== 'undefined') {
            delete globalThis.__pw_manual
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

    // CDP/devtools detectors time or inspect serialization of console arguments.
    // Keep normal console behavior, but avoid handing known probe shapes to the protocol.
    try {
        const consoleMethods = [
            'assert',
            'debug',
            'dir',
            'dirxml',
            'error',
            'info',
            'log',
            'table',
            'trace',
            'warn'
        ]
        const getObjectTag = value => {
            try {
                return Object.prototype.toString.call(value)
            } catch (e) {
                return ''
            }
        }

        const isObjectLike = value =>
            !!value && (typeof value === 'object' || typeof value === 'function')

        const isErrorLike = value => {
            if (!isObjectLike(value)) {
                return false
            }

            try {
                const tag = getObjectTag(value)
                return value instanceof Error ||
                    tag === '[object Error]' ||
                    tag === '[object DOMException]'
            } catch (e) {
                return false
            }
        }

        const hasOwnAccessorNamed = (value, propertyNames) => {
            if (!isObjectLike(value)) {
                return false
            }

            try {
                return propertyNames.some(propertyName => {
                    const descriptor = Object.getOwnPropertyDescriptor(value, propertyName)
                    return !!descriptor &&
                        (typeof descriptor.get === 'function' || typeof descriptor.set === 'function')
                })
            } catch (e) {
                return false
            }
        }

        const hasConsoleAccessorProbe = value =>
            isErrorLike(value) && hasOwnAccessorNamed(value, ['stack', 'name', 'message'])

        const hasOwnToStringOverride = value => {
            if (!isObjectLike(value)) {
                return false
            }

            try {
                const descriptor = Object.getOwnPropertyDescriptor(value, 'toString')
                return !!descriptor && typeof descriptor.value === 'function'
            } catch (e) {
                return false
            }
        }

        const hasKnownToStringProbe = value => {
            if (!hasOwnToStringOverride(value)) {
                return false
            }

            const tag = getObjectTag(value)
            return typeof value === 'function' ||
                tag === '[object RegExp]' ||
                tag === '[object Date]' ||
                tag === '[object Error]'
        }

        const hasNestedKnownToStringProbe = value => {
            if (!isObjectLike(value)) {
                return false
            }

            try {
                return Object.getOwnPropertyNames(value).some(propertyName => {
                    const descriptor = Object.getOwnPropertyDescriptor(value, propertyName)
                    return !!descriptor &&
                        Object.prototype.hasOwnProperty.call(descriptor, 'value') &&
                        hasKnownToStringProbe(descriptor.value)
                })
            } catch (e) {
                return false
            }
        }

        const isLargeTabularPayload = value => {
            if (!value || typeof value !== 'object' || !Array.isArray(value) || value.length < 25) {
                return false
            }

            try {
                const first = value[0]
                return !!first &&
                    typeof first === 'object' &&
                    Object.getOwnPropertyNames(first).length >= 100
            } catch (e) {
                return false
            }
        }

        const readStringDataProperty = (value, propertyName) => {
            try {
                const descriptor = Object.getOwnPropertyDescriptor(value, propertyName)
                return !!descriptor &&
                    Object.prototype.hasOwnProperty.call(descriptor, 'value') &&
                    typeof descriptor.value === 'string'
                    ? descriptor.value
                    : ''
            } catch (e) {
                return ''
            }
        }

        const sanitizeConsoleArg = value => {
            if (isLargeTabularPayload(value)) {
                return '[object Array]'
            }

            if (hasKnownToStringProbe(value) || hasNestedKnownToStringProbe(value)) {
                return Object.prototype.toString.call(value)
            }

            if (!hasConsoleAccessorProbe(value)) {
                return value
            }

            const name = readStringDataProperty(value, 'name') || 'Error'
            const messageValue = readStringDataProperty(value, 'message')
            const message = messageValue ? `: ${messageValue}` : ''
            return `${name}${message}`
        }

        for (const method of consoleMethods) {
            if (typeof console[method] === 'function') {
                utils.replaceWithProxy(console, method, {
                    apply(target, ctx, args) {
                        return utils.cache.Reflect.apply(target, ctx, args.map(sanitizeConsoleArg))
                    }
                })
            }
        }
    } catch (e) {}
} catch (err) {
}
