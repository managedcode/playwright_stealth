// Add noise to AudioContext fingerprinting
// Headless Chrome produces identical audio fingerprints across sessions
try {
    if (typeof AudioContext !== 'undefined' || typeof webkitAudioContext !== 'undefined') {
        const AudioCtx = typeof AudioContext !== 'undefined' ? AudioContext : webkitAudioContext

        // Session-stable noise for consistent fingerprint within session
        const audioNoiseSeed = Math.random() * 0.0001

        const originalCreateAnalyser = AudioCtx.prototype.createAnalyser
        if (originalCreateAnalyser) {
            AudioCtx.prototype.createAnalyser = function() {
                const analyser = originalCreateAnalyser.call(this)
                const originalGetFloatFrequencyData = analyser.getFloatFrequencyData.bind(analyser)
                const originalGetByteFrequencyData = analyser.getByteFrequencyData.bind(analyser)

                analyser.getFloatFrequencyData = function(array) {
                    originalGetFloatFrequencyData(array)
                    // Add tiny noise to frequency data
                    for (let i = 0; i < array.length; i++) {
                        array[i] = array[i] + (audioNoiseSeed * (i % 7))
                    }
                }

                analyser.getByteFrequencyData = function(array) {
                    originalGetByteFrequencyData(array)
                    for (let i = 0; i < array.length; i++) {
                        const n = Math.floor(audioNoiseSeed * 1000000 * (i % 13)) % 2
                        array[i] = Math.max(0, Math.min(255, array[i] + n))
                    }
                }

                return analyser
            }
            try {
                utils.patchToString(AudioCtx.prototype.createAnalyser)
            } catch (e) {}
        }

        // Patch getChannelData to add slight noise
        if (typeof AudioBuffer !== 'undefined') {
            const originalGetChannelData = AudioBuffer.prototype.getChannelData
            AudioBuffer.prototype.getChannelData = function(channel) {
                const data = originalGetChannelData.call(this, channel)
                // Only add noise to small buffers (fingerprinting uses short samples)
                if (data.length < 50000) {
                    for (let i = 0; i < data.length; i++) {
                        data[i] = data[i] + (audioNoiseSeed * ((i * 7 + channel) % 11) * 0.00001)
                    }
                }
                return data
            }
            try {
                utils.patchToString(AudioBuffer.prototype.getChannelData)
            } catch (e) {}
        }
    }

    // Also handle OfflineAudioContext
    if (typeof OfflineAudioContext !== 'undefined') {
        const originalStartRendering = OfflineAudioContext.prototype.startRendering
        if (originalStartRendering) {
            OfflineAudioContext.prototype.startRendering = function() {
                return originalStartRendering.call(this).then(function(buffer) {
                    // The AudioBuffer.getChannelData patch above will handle noise
                    return buffer
                })
            }
            try {
                utils.patchToString(OfflineAudioContext.prototype.startRendering)
            } catch (e) {}
        }
    }
} catch (err) {
}
