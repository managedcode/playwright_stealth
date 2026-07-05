try {
    if (opts.navigator_platform && navigator.platform !== opts.navigator_platform) {
        const proto = Object.getPrototypeOf(navigator)
        utils.replaceGetter(proto, 'platform', opts.navigator_platform)
    }
} catch (err) {
}
