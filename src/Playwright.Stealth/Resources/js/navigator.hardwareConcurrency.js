try {
    const value = opts.navigator_hardware_concurrency
    if (value > 0) {
        utils.replaceGetter(Object.getPrototypeOf(navigator), 'hardwareConcurrency', value)
    }
} catch (err) {
}
