// Preserve native screen metrics, including available work-area dimensions.
try {
    const screenWidth = opts.screen_width || window.innerWidth || 1920
    const screenHeight = opts.screen_height || window.innerHeight || 1080
    const fallback = {
        width: screenWidth,
        height: screenHeight,
        availWidth: screenWidth,
        availHeight: screenHeight,
        colorDepth: 24,
        pixelDepth: 24
    }
    for (const [property, value] of Object.entries(fallback)) {
        if (!(screen[property] > 0)) {
            utils.replaceGetter(Screen.prototype, property, value)
        }
    }
} catch (err) {
}
