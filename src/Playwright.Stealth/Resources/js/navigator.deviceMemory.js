// Mock navigator.deviceMemory
// Headless Chrome may report unusual or missing device memory values
try {
    const memoryValue = opts.navigator_device_memory || 8
    const proto = Object.getPrototypeOf(navigator)
    const descriptor = Object.getOwnPropertyDescriptor(proto, 'deviceMemory')
    if (!descriptor || descriptor.configurable) {
        Object.defineProperty(proto, 'deviceMemory', {
            get: () => memoryValue,
            configurable: true,
            enumerable: true
        })
    }
} catch (err) {
}
