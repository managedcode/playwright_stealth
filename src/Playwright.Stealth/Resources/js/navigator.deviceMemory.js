// Optionally spoof navigator.deviceMemory when explicitly configured.
try {
    const memoryValue = opts.navigator_device_memory
    if (memoryValue > 0) {
        utils.replaceGetter(Object.getPrototypeOf(navigator), 'deviceMemory', memoryValue)
    }
} catch (err) {
}
