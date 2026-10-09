try {
    if (opts.languages && opts.languages.length > 0) {
        const languages = Object.freeze([...opts.languages])
        utils.replaceGetter(Object.getPrototypeOf(navigator), 'languages', languages)
        utils.replaceGetter(Object.getPrototypeOf(navigator), 'language', languages[0])
    }
} catch (err) {
}
