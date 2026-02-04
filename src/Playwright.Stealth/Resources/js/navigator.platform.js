try {
    if (opts.navigator_platform) {
        const proto = Object.getPrototypeOf(navigator)
        const descriptor = Object.getOwnPropertyDescriptor(proto, 'platform')

        if (descriptor && descriptor.configurable) {
            Object.defineProperty(proto, 'platform', {
                get: () => opts.navigator_platform,
            })
        }
    }
} catch (err) {
}
