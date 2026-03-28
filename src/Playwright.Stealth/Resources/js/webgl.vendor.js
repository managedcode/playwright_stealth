const desiredVendor = opts.webgl_vendor || 'Intel Inc.'
const desiredRenderer = opts.webgl_renderer || 'Intel Iris OpenGL Engine'

const getParameterProxyHandler = {
    apply: function (target, ctx, args) {
        const param = (args || [])[0]
        // UNMASKED_VENDOR_WEBGL
        if (param === 37445) {
            return desiredVendor
        }
        // UNMASKED_RENDERER_WEBGL
        if (param === 37446) {
            return desiredRenderer
        }
        // VENDOR (7936) - may also leak GPU info
        if (param === 7936) {
            const val = utils.cache.Reflect.apply(target, ctx, args)
            if (val && typeof val === 'string' && /SwiftShader/i.test(val)) {
                return desiredVendor
            }
            return val
        }
        // RENDERER (7937) - often contains ANGLE(...SwiftShader...) string
        if (param === 7937) {
            const val = utils.cache.Reflect.apply(target, ctx, args)
            if (val && typeof val === 'string' && /SwiftShader/i.test(val)) {
                return desiredRenderer
            }
            return val
        }
        return utils.cache.Reflect.apply(target, ctx, args)
    }
}

// There's more than one WebGL rendering context
// https://developer.mozilla.org/en-US/docs/Web/API/WebGL2RenderingContext#Browser_compatibility
utils.replaceWithProxy(WebGLRenderingContext.prototype, 'getParameter', getParameterProxyHandler)
utils.replaceWithProxy(WebGL2RenderingContext.prototype, 'getParameter', getParameterProxyHandler)
