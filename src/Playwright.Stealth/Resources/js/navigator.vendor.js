try {
    const proto = Object.getPrototypeOf(navigator)
    const descriptor = Object.getOwnPropertyDescriptor(proto, 'vendor')

    if (descriptor && descriptor.configurable) {
        Object.defineProperty(proto, 'vendor', {
            get: () => opts.navigator_vendor || 'Google Inc.',
        })
    }
} catch (err) {
}
