// Add subtle jitter to performance.now() and Date.now()
// Headless Chrome provides too-precise timing; real browsers have OS-level variance
try {
    const originalPerformanceNow = Performance.prototype.now

    Performance.prototype.now = function() {
        const val = originalPerformanceNow.call(this)
        // Add ±0.1ms jitter (real browsers have this from OS scheduling)
        const jitter = (Math.random() - 0.5) * 0.2
        return val + jitter
    }

    try {
        utils.patchToString(Performance.prototype.now)
    } catch (e) {}

    // Also add jitter to requestAnimationFrame timestamps
    const originalRAF = window.requestAnimationFrame
    if (originalRAF) {
        window.requestAnimationFrame = function(callback) {
            return originalRAF.call(window, function(timestamp) {
                // Add ±0.5ms jitter to rAF timestamp
                const jitter = (Math.random() - 0.5) * 1.0
                callback(timestamp + jitter)
            })
        }
        try {
            utils.patchToString(window.requestAnimationFrame)
        } catch (e) {}
    }
} catch (err) {
}
