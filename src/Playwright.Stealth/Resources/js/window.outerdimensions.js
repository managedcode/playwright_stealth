'use strict'

try {
    if (!window.outerWidth || !window.outerHeight) {
        const windowFrame = 85 // probably OS and WM dependent
        Object.defineProperty(window, 'outerWidth', {
            get: () => window.innerWidth,
            configurable: true
        })
        Object.defineProperty(window, 'outerHeight', {
            get: () => window.innerHeight + windowFrame,
            configurable: true
        })
    }
} catch (err) {
}
