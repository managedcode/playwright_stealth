// Mock speechSynthesis.getVoices()
// Headless Chrome returns empty voices array, real browsers have system voices
try {
    if (typeof window !== 'undefined' && window.speechSynthesis) {
        const defaultVoices = [
            { voiceURI: 'Alex', name: 'Alex', lang: 'en-US', localService: true, default: true },
            { voiceURI: 'Samantha', name: 'Samantha', lang: 'en-US', localService: true, default: false },
            { voiceURI: 'Daniel', name: 'Daniel', lang: 'en-GB', localService: true, default: false },
            { voiceURI: 'Google US English', name: 'Google US English', lang: 'en-US', localService: false, default: false },
            { voiceURI: 'Google UK English Female', name: 'Google UK English Female', lang: 'en-GB', localService: false, default: false }
        ]

        // Create proper SpeechSynthesisVoice-like objects
        const voices = defaultVoices.map(v => {
            const voice = Object.create(null)
            Object.defineProperties(voice, {
                voiceURI: { get: () => v.voiceURI, enumerable: true },
                name: { get: () => v.name, enumerable: true },
                lang: { get: () => v.lang, enumerable: true },
                localService: { get: () => v.localService, enumerable: true },
                default: { get: () => v.default, enumerable: true }
            })
            return voice
        })

        const originalGetVoices = window.speechSynthesis.getVoices.bind(window.speechSynthesis)

        window.speechSynthesis.getVoices = function() {
            const realVoices = originalGetVoices()
            if (realVoices && realVoices.length > 0) {
                return realVoices
            }
            return voices
        }

        // Ensure toString looks native
        try {
            utils.patchToString(window.speechSynthesis.getVoices, 'function getVoices() { [native code] }')
        } catch (e) {}
    }
} catch (err) {
}
