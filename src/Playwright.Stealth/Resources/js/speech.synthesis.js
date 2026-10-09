// Native voice lists load asynchronously and can legitimately be empty.
// Preserve the browser's SpeechSynthesisVoice objects and getVoices descriptor;
// manufactured voices cannot satisfy the interface's native brand checks.
if (typeof speechSynthesis !== 'undefined' && typeof speechSynthesis.getVoices === 'function') {
    return
}
