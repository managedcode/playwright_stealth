const getNotificationPermission = () => {
    try {
        if (typeof Notification !== 'undefined' && Notification.permission) {
            return Notification.permission
        }
    } catch (err) {
    }
    return 'default'
}

const normalizePermission = (permission) => permission === 'denied' ? 'default' : permission

const normalizedPermission = normalizePermission(getNotificationPermission())

try {
    if (typeof Notification !== 'undefined') {
        const descriptor = Object.getOwnPropertyDescriptor(Notification, 'permission')
        if (descriptor && descriptor.configurable) {
            Object.defineProperty(Notification, 'permission', {
                get: () => normalizedPermission
            })
        }
    }
} catch (err) {
}

const handler = {
    apply: function (target, ctx, args) {
        const param = (args || [])[0]

        if (param && param.name && param.name === 'notifications') {
            const state = normalizedPermission === 'default' ? 'prompt' : normalizedPermission
            const result = {state}
            Object.setPrototypeOf(result, PermissionStatus.prototype)
            return Promise.resolve(result)
        }

        return utils.cache.Reflect.apply(...arguments)
    }
}

utils.replaceWithProxy(
    window.navigator.permissions.__proto__, // eslint-disable-line no-proto
    'query',
    handler
)
