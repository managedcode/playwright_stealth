try {
    const value = opts.navigator_hardware_concurrency
    if (value > 0 && (typeof window === 'undefined' || navigator.hardwareConcurrency !== value)) {
        utils.replaceGetter(Object.getPrototypeOf(navigator), 'hardwareConcurrency', value)
    }
} catch (err) {
}
