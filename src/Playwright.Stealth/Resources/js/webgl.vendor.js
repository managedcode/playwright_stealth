const desiredVendor = opts.webgl_vendor
const desiredRenderer = opts.webgl_renderer

// Keep the actual GPU consistent with WebGPU, shaders and OffscreenCanvas workers.
if (!desiredVendor && !desiredRenderer) {
    return
}

const getParameterProxyHandler = {
    apply: function (target, ctx, args) {
        const param = (args || [])[0]
        const nativeValue = utils.cache.Reflect.apply(target, ctx, args)
        // UNMASKED_VENDOR_WEBGL
        if (param === 37445 && desiredVendor) {
            return desiredVendor
        }
        // UNMASKED_RENDERER_WEBGL
        if (param === 37446 && desiredRenderer) {
            return desiredRenderer
        }
        // VENDOR (7936) - may also leak GPU info
        if (param === 7936) {
            const val = nativeValue
            if (val && typeof val === 'string' && /SwiftShader/i.test(val)) {
                return desiredVendor || val
            }
            return val
        }
        // RENDERER (7937) - often contains ANGLE(...SwiftShader...) string
        if (param === 7937) {
            const val = nativeValue
            if (val && typeof val === 'string' && /SwiftShader/i.test(val)) {
                return desiredRenderer || val
            }
            return val
        }
        return nativeValue
    }
}

// There's more than one WebGL rendering context
// https://developer.mozilla.org/en-US/docs/Web/API/WebGL2RenderingContext#Browser_compatibility
if (typeof WebGLRenderingContext !== 'undefined') {
    utils.replaceWithProxy(WebGLRenderingContext.prototype, 'getParameter', getParameterProxyHandler)
}
if (typeof WebGL2RenderingContext !== 'undefined') {
    utils.replaceWithProxy(WebGL2RenderingContext.prototype, 'getParameter', getParameterProxyHandler)
}
