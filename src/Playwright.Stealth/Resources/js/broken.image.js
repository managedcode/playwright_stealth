// Fix broken image dimensions detection
// Headless Chrome renders broken/missing images at 16x16 while real browsers use 0x0
try {
    const originalDescNW = Object.getOwnPropertyDescriptor(HTMLImageElement.prototype, 'naturalWidth')
    const originalDescNH = Object.getOwnPropertyDescriptor(HTMLImageElement.prototype, 'naturalHeight')

    if (originalDescNW && originalDescNW.get && originalDescNW.configurable) {
        const originalGetNW = originalDescNW.get
        Object.defineProperty(HTMLImageElement.prototype, 'naturalWidth', {
            get: function() {
                const val = originalGetNW.call(this)
                // If the image failed to load and shows the default broken icon (16x16),
                // return 0 to match real browser behavior
                if (val === 16 && !this.complete) return 0
                if (val === 16 && this.complete && !this.naturalHeight) return 0
                return val
            },
            configurable: true,
            enumerable: true
        })
    }

    if (originalDescNH && originalDescNH.get && originalDescNH.configurable) {
        const originalGetNH = originalDescNH.get
        Object.defineProperty(HTMLImageElement.prototype, 'naturalHeight', {
            get: function() {
                const val = originalGetNH.call(this)
                if (val === 16 && !this.complete) return 0
                if (val === 16 && this.complete && !this.naturalWidth) return 0
                return val
            },
            configurable: true,
            enumerable: true
        })
    }

    // Also patch width and height getters for broken images
    const originalDescW = Object.getOwnPropertyDescriptor(HTMLImageElement.prototype, 'width')
    const originalDescH = Object.getOwnPropertyDescriptor(HTMLImageElement.prototype, 'height')

    if (originalDescW && originalDescW.get && originalDescW.configurable) {
        const originalGetW = originalDescW.get
        Object.defineProperty(HTMLImageElement.prototype, 'width', {
            get: function() {
                const val = originalGetW.call(this)
                // Only patch when no explicit dimensions set and image is broken
                if (val === 16 && !this.getAttribute('width') && !this.complete) return 0
                return val
            },
            set: function(v) {
                this.setAttribute('width', v)
            },
            configurable: true,
            enumerable: true
        })
    }

    if (originalDescH && originalDescH.get && originalDescH.configurable) {
        const originalGetH = originalDescH.get
        Object.defineProperty(HTMLImageElement.prototype, 'height', {
            get: function() {
                const val = originalGetH.call(this)
                if (val === 16 && !this.getAttribute('height') && !this.complete) return 0
                return val
            },
            set: function(v) {
                this.setAttribute('height', v)
            },
            configurable: true,
            enumerable: true
        })
    }
} catch (err) {
}
