try {
    const targetVendor = opts.navigator_vendor || 'Google Inc.'
    if (navigator.vendor === targetVendor) {
        return
    }

    const proto = Object.getPrototypeOf(navigator)
    utils.replaceGetter(proto, 'vendor', targetVendor)
} catch (err) {
}
