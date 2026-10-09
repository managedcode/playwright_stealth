// Current Chromium rounds a half-pixel layout height correctly. Only patch
// engines that still exhibit the old Modernizr hairline bug.
const probe = document.createElement('div')
probe.style.cssText = 'position:absolute;visibility:hidden;height:0.5px;'
const root = document.documentElement
if (!root) {
    return
}
root.appendChild(probe)
const supported = probe.offsetHeight === 1
probe.remove()
if (!supported) {
    utils.replaceGetterWithProxy(HTMLElement.prototype, 'offsetHeight', {
        apply(target, receiver, args) {
            const value = utils.cache.Reflect.apply(target, receiver, args)
            return receiver.id === 'modernizr' ? 1 : value
        }
    })
}
