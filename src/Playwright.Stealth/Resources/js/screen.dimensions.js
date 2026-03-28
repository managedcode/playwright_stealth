// Ensure screen dimensions are consistent and realistic
// Headless Chrome may report 0 or inconsistent screen values
try {
    const screenWidth = opts.screen_width || window.innerWidth || 1920
    const screenHeight = opts.screen_height || window.innerHeight || 1080
    const colorDepth = opts.screen_color_depth || 24
    const pixelDepth = opts.screen_pixel_depth || 24

    const screenProps = {
        width: { value: screenWidth, writable: false },
        height: { value: screenHeight, writable: false },
        availWidth: { value: screenWidth, writable: false },
        availHeight: { value: screenHeight - 40, writable: false }, // taskbar offset
        colorDepth: { value: colorDepth, writable: false },
        pixelDepth: { value: pixelDepth, writable: false }
    }

    for (const [prop, config] of Object.entries(screenProps)) {
        const descriptor = Object.getOwnPropertyDescriptor(Screen.prototype, prop) ||
                           Object.getOwnPropertyDescriptor(screen, prop)
        if (!descriptor || descriptor.configurable || descriptor.writable) {
            try {
                Object.defineProperty(Screen.prototype, prop, {
                    get: () => config.value,
                    configurable: true,
                    enumerable: true
                })
            } catch (e) {
                // Fallback: try setting on screen directly
                try {
                    Object.defineProperty(screen, prop, {
                        get: () => config.value,
                        configurable: true,
                        enumerable: true
                    })
                } catch (e2) {}
            }
        }
    }
} catch (err) {
}
