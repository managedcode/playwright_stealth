// replace headless references in default useragent
try {
    const current_ua = navigator.userAgent
    const current_app_version = navigator.appVersion
    const proto = Object.getPrototypeOf(navigator)
    const ua_descriptor = Object.getOwnPropertyDescriptor(proto, 'userAgent')
    const app_version_descriptor = Object.getOwnPropertyDescriptor(proto, 'appVersion')
    const prefix = opts.ua_patch_prefix || ''
    const suffix = opts.ua_patch_suffix || '/'
    const browser_name = String.fromCharCode(67,104,114,111,109,101)
    const search_str = prefix + browser_name + suffix
    const replace_str = browser_name + suffix
    const patched_ua = opts.navigator_user_agent || current_ua.split(search_str).join(replace_str)
    const patched_app_version = opts.navigator_user_agent
        ? opts.navigator_user_agent.replace(/Mozilla\//, '')
        : current_app_version.split(search_str).join(replace_str)

    if (patched_ua !== current_ua && ua_descriptor && ua_descriptor.configurable) {
        utils.replaceGetter(proto, 'userAgent', patched_ua)
    }

    if (patched_app_version !== current_app_version && app_version_descriptor && app_version_descriptor.configurable) {
        utils.replaceGetter(proto, 'appVersion', patched_app_version)
    }
} catch (err) {
}
