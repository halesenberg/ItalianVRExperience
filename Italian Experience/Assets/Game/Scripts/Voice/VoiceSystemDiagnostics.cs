using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using Whisper;
using Whisper.Utils;
using Bellavalle.AI;

namespace Bellavalle.Voice
{
    /// <summary>
    /// Verifica la disponibilità di tutti i componenti necessari per il riconoscimento vocale
    /// al startup del gioco. Logga uno status report comprensivo:
    /// - Dispositivo microfono selezionato
    /// - Modello Whisper caricato
    /// - Hand tracking disponibile
    /// - Componenti audio pronti
    /// - Lingua impostata (it/en)
    /// </summary>
    public class VoiceSystemDiagnostics : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("\n" + new string('=', 70));
            Debug.Log("█ VOICE SYSTEM DIAGNOSTICS — STARTUP CHECK");
            Debug.Log(new string('=', 70));

            CheckMicrophone();
            CheckWhisperModel();
            CheckHandTracking();
            CheckAudioComponents();
            CheckLanguageSettings();
            CheckGestureInput();

            Debug.Log(new string('=', 70) + "\n");
        }

        void CheckMicrophone()
        {
            Debug.Log("\n📻 MICROPHONE STATUS:");
            var devices = Microphone.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("  ✗ NO MICROPHONES FOUND");
                return;
            }

            Debug.Log($"  ✓ {devices.Length} device(s) available:");
            foreach (var d in devices)
                Debug.Log($"     • {d}");

            var micRecord = FindFirstObjectByType<MicrophoneRecord>();
            if (micRecord != null)
                Debug.Log($"  ✓ Selected: \"{micRecord.SelectedMicDevice}\"");
            else
                Debug.LogWarning("  ⚠ MicrophoneRecord component not found");
        }

        void CheckWhisperModel()
        {
            Debug.Log("\n🎤 WHISPER MODEL STATUS:");
            var whisperManager = FindFirstObjectByType<WhisperManager>();
            if (whisperManager == null)
            {
                Debug.LogError("  ✗ WhisperManager not found");
                return;
            }

            Debug.Log($"  ✓ WhisperManager found");
            Debug.Log($"  • Language: \"{whisperManager.language}\"");
            Debug.Log($"  ℹ Whisper model will initialize on first recording");
        }

        void CheckHandTracking()
        {
            Debug.Log("\n✋ HAND TRACKING STATUS:");

#if UNITY_EDITOR
            Debug.Log("  ℹ Running in EDITOR MODE");
            Debug.Log("  • Hand gestures disabled → use SPACE key to record");
            Debug.Log("  • (Hold Space 2+ seconds, then release)");
            return;
#endif

            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            if (subsystems.Count == 0)
            {
                Debug.LogError("  ✗ XRHandSubsystem not available");
                Debug.LogError("  • Hand tracking disabled or VR headset not connected");
                return;
            }

            var handSubsystem = subsystems[0];
            bool leftTracked = handSubsystem.leftHand.isTracked;
            bool rightTracked = handSubsystem.rightHand.isTracked;

            Debug.Log($"  ✓ XRHandSubsystem available");
            Debug.Log($"  • Left hand:  {(leftTracked ? "✓ TRACKED" : "✗ NOT TRACKED")}");
            Debug.Log($"  • Right hand: {(rightTracked ? "✓ TRACKED" : "✗ NOT TRACKED")}");
        }

        void CheckAudioComponents()
        {
            Debug.Log("\n🔊 AUDIO COMPONENTS:");
            var pushToTalk = FindFirstObjectByType<PushToTalkRecorder>();
            if (pushToTalk == null)
            {
                Debug.LogError("  ✗ PushToTalkRecorder not found");
                return;
            }

            Debug.Log($"  ✓ PushToTalkRecorder found");

            var persistentWhisper = PersistentWhisper.Instance;
            if (persistentWhisper == null)
            {
                Debug.LogWarning("  ⚠ PersistentWhisper singleton not active yet");
                return;
            }

            if (persistentWhisper.PushToTalkRecorder != null)
                Debug.Log($"  ✓ PushToTalkRecorder accessible via PersistentWhisper.Instance");
        }

        void CheckLanguageSettings()
        {
            Debug.Log("\n🌐 LANGUAGE SETTINGS:");
            var whisperManager = FindFirstObjectByType<WhisperManager>();
            if (whisperManager == null)
            {
                Debug.LogWarning("  ⚠ WhisperManager not found");
                return;
            }

            Debug.Log($"  • Default language: \"{whisperManager.language}\"");

            if (whisperManager.language == "it")
                Debug.Log($"  ✓ Italian mode (normal gameplay)");
            else if (whisperManager.language == "en")
                Debug.Log($"  ⚠ English mode (AI panel or switched)");
            else
                Debug.Log($"  ⚠ Language: {whisperManager.language}");

            var aiHelper = FindFirstObjectByType<AIGrammarHelperService>();
            if (aiHelper != null)
                Debug.Log($"  ✓ AI Grammar Helper available (switches EN ↔ IT)");
        }

        void CheckGestureInput()
        {
            Debug.Log("\n👆 GESTURE INPUT:");
            var gestureInput = FindFirstObjectByType<GestureSpeechInput>();
            if (gestureInput == null)
            {
                Debug.LogError("  ✗ GestureSpeechInput not found");
                return;
            }

            Debug.Log($"  ✓ GestureSpeechInput found");

#if UNITY_EDITOR
            Debug.Log("  📝 EDITOR TESTING:");
            Debug.Log("     1. Press PLAY");
            Debug.Log("     2. Hold SPACE (2+ seconds) while speaking");
            Debug.Log("     3. Release SPACE");
            Debug.Log("     4. Check console for \"Trascritto: [your words]\"");
#else
            Debug.Log("  📝 VR TESTING:");
            Debug.Log("     1. Make Index Pointing Up gesture");
            Debug.Log("     2. Wait 1 second (mic activates)");
            Debug.Log("     3. Speak Italian");
            Debug.Log("     4. Palms down (mic stops, transcription starts)");
            Debug.Log("     5. Check console for \"Trascritto: [your words]\"");
#endif
        }
    }
}
