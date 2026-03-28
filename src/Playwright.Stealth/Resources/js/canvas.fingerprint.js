// Add subtle noise to canvas fingerprinting
// Headless Chrome produces identical canvas fingerprints every time
// Real browsers have slight variations due to GPU rendering differences
try {
    const originalToDataURL = HTMLCanvasElement.prototype.toDataURL
    const originalToBlob = HTMLCanvasElement.prototype.toBlob
    const originalGetImageData = CanvasRenderingContext2D.prototype.getImageData

    // Generate a session-stable noise seed so fingerprint is consistent within session
    // but different across sessions (like a real browser)
    const noiseSeed = Math.floor(Math.random() * 256)

    // Simple hash function for deterministic noise based on pixel position
    const noise = (x, y, channel) => {
        const n = ((x * 374761393 + y * 668265263 + channel * 1274126177 + noiseSeed) & 0x7fffffff)
        return (n % 3) - 1 // returns -1, 0, or 1
    }

    CanvasRenderingContext2D.prototype.getImageData = function(sx, sy, sw, sh) {
        const imageData = originalGetImageData.call(this, sx, sy, sw, sh)
        // Only apply noise to canvases that look like fingerprint attempts
        // (small canvases used for fingerprinting, not large ones used for rendering)
        if (sw * sh < 500 * 500) {
            const data = imageData.data
            for (let i = 0; i < data.length; i += 4) {
                const pixelIdx = i / 4
                const x = pixelIdx % sw
                const y = Math.floor(pixelIdx / sw)
                // Apply tiny noise to RGB channels (not alpha)
                data[i] = Math.max(0, Math.min(255, data[i] + noise(x, y, 0)))
                data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + noise(x, y, 1)))
                data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + noise(x, y, 2)))
            }
        }
        return imageData
    }

    HTMLCanvasElement.prototype.toDataURL = function() {
        const ctx = this.getContext('2d')
        if (ctx && this.width * this.height < 500 * 500) {
            // Trigger getImageData noise by reading and writing back
            try {
                const imageData = originalGetImageData.call(ctx, 0, 0, this.width, this.height)
                const data = imageData.data
                for (let i = 0; i < data.length; i += 4) {
                    const pixelIdx = i / 4
                    const x = pixelIdx % this.width
                    const y = Math.floor(pixelIdx / this.width)
                    data[i] = Math.max(0, Math.min(255, data[i] + noise(x, y, 0)))
                    data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + noise(x, y, 1)))
                    data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + noise(x, y, 2)))
                }
                ctx.putImageData(imageData, 0, 0)
            } catch (e) {}
        }
        return originalToDataURL.apply(this, arguments)
    }

    HTMLCanvasElement.prototype.toBlob = function(callback, type, quality) {
        const ctx = this.getContext('2d')
        if (ctx && this.width * this.height < 500 * 500) {
            try {
                const imageData = originalGetImageData.call(ctx, 0, 0, this.width, this.height)
                const data = imageData.data
                for (let i = 0; i < data.length; i += 4) {
                    const pixelIdx = i / 4
                    const x = pixelIdx % this.width
                    const y = Math.floor(pixelIdx / this.width)
                    data[i] = Math.max(0, Math.min(255, data[i] + noise(x, y, 0)))
                    data[i + 1] = Math.max(0, Math.min(255, data[i + 1] + noise(x, y, 1)))
                    data[i + 2] = Math.max(0, Math.min(255, data[i + 2] + noise(x, y, 2)))
                }
                ctx.putImageData(imageData, 0, 0)
            } catch (e) {}
        }
        return originalToBlob.call(this, callback, type, quality)
    }

    try {
        utils.patchToString(CanvasRenderingContext2D.prototype.getImageData)
        utils.patchToString(HTMLCanvasElement.prototype.toDataURL)
        utils.patchToString(HTMLCanvasElement.prototype.toBlob)
    } catch (e) {}
} catch (err) {
}
