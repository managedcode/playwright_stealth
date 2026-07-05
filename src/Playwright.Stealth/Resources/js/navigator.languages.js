try {
    if (opts.languages && opts.languages.length > 0) {
        utils.replaceGetter(Object.getPrototypeOf(navigator), 'languages', opts.languages)
    }
} catch (err) {
}
