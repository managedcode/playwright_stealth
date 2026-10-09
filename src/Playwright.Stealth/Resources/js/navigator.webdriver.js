// AutomationControlled already exposes a native false getter on current Chromium.
if (navigator.webdriver === true) {
    utils.replaceGetter(Object.getPrototypeOf(navigator), 'webdriver', false)
}
