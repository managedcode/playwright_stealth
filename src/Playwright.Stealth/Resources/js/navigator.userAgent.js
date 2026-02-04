// replace Headless references in default useragent
try {
    const current_ua = navigator.userAgent
    const proto = Object.getPrototypeOf(navigator)
    const descriptor = Object.getOwnPropertyDescriptor(proto, 'userAgent')

    if (descriptor && descriptor.configurable) {
        Object.defineProperty(proto, 'userAgent', {
            get: () => opts.navigator_user_agent || current_ua.replace('HeadlessChrome/', 'Chrome/')
        })
    }
} catch (err) {
}
