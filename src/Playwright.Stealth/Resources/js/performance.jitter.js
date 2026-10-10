// Normalize CDP's error-stack timing overhead with fixed 10ms clock precision.
// Preserve monotonicity, native receiver errors and animation-frame timestamps.
try {
    if (typeof Performance !== 'undefined') {
        utils.replaceWithProxy(Performance.prototype, 'now', {
            apply(target, receiver, args) {
                return Math.floor(utils.cache.Reflect.apply(target, receiver, args) / 10) * 10
            }
        })
    }
} catch (err) {
}
