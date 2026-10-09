// Reduce clock precision without allowing time to run backwards. Preserve
// native receiver validation and requestAnimationFrame's browser timestamps.
try {
    if (typeof Performance !== 'undefined') {
        utils.replaceWithProxy(Performance.prototype, 'now', {
            apply(target, receiver, args) {
                return Math.floor(utils.cache.Reflect.apply(target, receiver, args))
            }
        })
    }
} catch (err) {
}
