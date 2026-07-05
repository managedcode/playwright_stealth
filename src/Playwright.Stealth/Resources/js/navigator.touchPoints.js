// Optionally spoof navigator.maxTouchPoints when explicitly configured.
try {
    const touchPoints = opts.max_touch_points
    if (touchPoints >= 0) {
        utils.replaceGetter(Object.getPrototypeOf(navigator), 'maxTouchPoints', touchPoints)
    }

    // Desktop Chrome with touch support has maxTouchPoints >= 1 but no ontouchstart.
} catch (err) {
}
