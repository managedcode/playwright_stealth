// Remove automation-specific global properties
// ChromeDriver, Puppeteer, Playwright and other tools leave detectable markers
try {
    // ChromeDriver markers (cdc_ prefixed)
    const windowKeys = Object.getOwnPropertyNames(window)
    for (const key of windowKeys) {
        if (/^cdc_/.test(key) || /^__webdriver/.test(key) || /^__selenium/.test(key) ||
            /^__fxdriver/.test(key) || /^__autonium/.test(key) ||
            /^calledSelenium/.test(key) || /^_phantom/.test(key) ||
            /^_WEBDRIVER_ELEM/.test(key) || /^webdriver/.test(key)) {
            try {
                delete window[key]
            } catch (e) {
                try {
                    Object.defineProperty(window, key, { value: undefined, configurable: true })
                } catch (e2) {}
            }
        }
    }

    // Document markers
    try {
        const docKeys = Object.getOwnPropertyNames(document)
        for (const key of docKeys) {
            if (/^\$cdc_/.test(key) || /^__webdriver/.test(key) ||
                /^__driver/.test(key) || /^__selenium/.test(key)) {
                try {
                    delete document[key]
                } catch (e) {
                    try {
                        Object.defineProperty(document, key, { value: undefined, configurable: true })
                    } catch (e2) {}
                }
            }
        }
    } catch (e) {}

    // Remove domAutomation and domAutomationController
    try {
        if (typeof window.domAutomation !== 'undefined') {
            delete window.domAutomation
        }
        if (typeof window.domAutomationController !== 'undefined') {
            delete window.domAutomationController
        }
    } catch (e) {}

    // Intercept future property additions (protect against runtime injection)
    const automationPatterns = /^(cdc_|\$cdc_|__webdriver|__selenium|__fxdriver|__playwright|__puppeteer|domAutomation)/

    // Use a MutationObserver to clean up dynamically added automation properties
    try {
        const cleanAutomationProps = () => {
            const keys = Object.getOwnPropertyNames(window)
            for (const key of keys) {
                if (automationPatterns.test(key)) {
                    try { delete window[key] } catch (e) {}
                }
            }
        }
        // Run cleanup periodically during page load
        const cleanupInterval = setInterval(cleanAutomationProps, 50)
        setTimeout(() => clearInterval(cleanupInterval), 5000)
    } catch (e) {}
} catch (err) {
}
