// Mock navigator.maxTouchPoints
// Headless Chrome returns 0 for maxTouchPoints, real desktop browsers return 0-1
// but many detection scripts flag 0 as suspicious on platforms that should support touch
try {
    const touchPoints = opts.max_touch_points !== undefined ? opts.max_touch_points : 1
    const proto = Object.getPrototypeOf(navigator)
    const descriptor = Object.getOwnPropertyDescriptor(proto, 'maxTouchPoints')
    if (!descriptor || descriptor.configurable) {
        Object.defineProperty(proto, 'maxTouchPoints', {
            get: () => touchPoints,
            configurable: true,
            enumerable: true
        })
    }

    // Also ensure ontouchstart is not present on desktop (consistent with maxTouchPoints=1)
    // Desktop Chrome with touch support has maxTouchPoints >= 1 but no ontouchstart
} catch (err) {
}
