// replace Headless references in default useragent
try {
    const current_ua = navigator.userAgent
    const current_app_version = navigator.appVersion
    const proto = Object.getPrototypeOf(navigator)
    const ua_descriptor = Object.getOwnPropertyDescriptor(proto, 'userAgent')
    const app_version_descriptor = Object.getOwnPropertyDescriptor(proto, 'appVersion')
    const patched_ua = opts.navigator_user_agent || current_ua.replace('HeadlessChrome/', 'Chrome/')
    const patched_app_version = opts.navigator_user_agent
        ? opts.navigator_user_agent.replace('Mozilla/', '')
        : current_app_version.replace('HeadlessChrome/', 'Chrome/')

    if (ua_descriptor && ua_descriptor.configurable) {
        Object.defineProperty(proto, 'userAgent', {
            get: () => patched_ua
        })
    }

    if (app_version_descriptor && app_version_descriptor.configurable) {
        Object.defineProperty(proto, 'appVersion', {
            get: () => patched_app_version
        })
    }
} catch (err) {
}
