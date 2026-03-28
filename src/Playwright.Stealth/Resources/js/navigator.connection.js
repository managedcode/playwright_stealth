// Mock navigator.connection (NetworkInformation API)
// Headless Chrome may expose missing or inconsistent connection info
try {
    if (typeof navigator !== 'undefined') {
        const connectionData = {
            effectiveType: '4g',
            downlink: 10,
            rtt: 50,
            saveData: false,
            type: 'wifi',
            downlinkMax: Infinity,
            ontypechange: null,
            onchange: null
        }

        const connectionProto = {
            get effectiveType() { return connectionData.effectiveType },
            get downlink() { return connectionData.downlink },
            get rtt() { return connectionData.rtt },
            get saveData() { return connectionData.saveData },
            get type() { return connectionData.type },
            get downlinkMax() { return connectionData.downlinkMax },
            get ontypechange() { return connectionData.ontypechange },
            set ontypechange(v) { connectionData.ontypechange = v },
            get onchange() { return connectionData.onchange },
            set onchange(v) { connectionData.onchange = v },
            addEventListener: function() {},
            removeEventListener: function() {},
            dispatchEvent: function() { return true }
        }

        // Make toString return [object NetworkInformation]
        const handler = {
            get: function(target, prop) {
                if (prop === Symbol.toStringTag) return 'NetworkInformation'
                return target[prop]
            }
        }

        const connection = new Proxy(connectionProto, handler)

        const proto = Object.getPrototypeOf(navigator)
        const existingDescriptor = Object.getOwnPropertyDescriptor(proto, 'connection')
        if (!existingDescriptor || existingDescriptor.configurable) {
            Object.defineProperty(proto, 'connection', {
                get: () => connection,
                configurable: true,
                enumerable: true
            })
        }
    }
} catch (err) {
}
