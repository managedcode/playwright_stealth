try {
    const proto = Object.getPrototypeOf(navigator)
    const descriptor = Object.getOwnPropertyDescriptor(proto, 'languages')

    if (descriptor && descriptor.configurable) {
        Object.defineProperty(proto, 'languages', {
            get: () => opts.languages || ['en-US', 'en']
        })
    }
} catch (err) {
}
