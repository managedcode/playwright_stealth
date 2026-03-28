// Ensure navigator.pdfViewerEnabled returns true
// Headless Chrome may report false or undefined for PDF viewer support
try {
    const proto = Object.getPrototypeOf(navigator)
    const descriptor = Object.getOwnPropertyDescriptor(proto, 'pdfViewerEnabled')
    if (!descriptor || descriptor.configurable) {
        Object.defineProperty(proto, 'pdfViewerEnabled', {
            get: () => true,
            configurable: true,
            enumerable: true
        })
    }
} catch (err) {
}
