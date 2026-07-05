// Preserve native navigator.pdfViewerEnabled when present; mock it only when missing.
try {
    if ('pdfViewerEnabled' in navigator) {
        return
    }

    const proto = Object.getPrototypeOf(navigator)
    utils.replaceGetter(proto, 'pdfViewerEnabled', true)
} catch (err) {
}
