using UnityEngine;
using Whisper;
using Whisper.Utils;

namespace Bellavalle.Voice
{
    /// <summary>
    /// Gestisce la registrazione push-to-talk del player.
    /// Tieni premuto il grip sinistro per parlare, rilascia per trascrivere.
    ///
    /// Setup:
    ///  1. Aggiungi un component MicrophoneRecord sullo stesso GameObject
    ///     (o assegnalo nell'Inspector se sta altrove)
    ///  2. Aggiungi questo script e assegna whisperManager + microphoneRecord
    ///  3. Collega StartRecording() / StopRecording() al grip sinistro
    ///  4. Ascolta l'evento OnTranscriptionReady per ricevere il testo
    /// </summary>
    public class PushToTalkRecorder : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] WhisperManager whisperManager;
        [SerializeField] MicrophoneRecord microphoneRecord;

        [Header("Debug")]
        [SerializeField] bool logToConsole = true;

        public bool IsRecording => microphoneRecord != null && microphoneRecord.IsRecording;

        /// <summary>
        /// Evento invocato quando la trascrizione e' pronta.
        /// AIDialogueService si aggancia qui per ricevere il testo del player.
        /// </summary>
        public event System.Action<string> OnTranscriptionReady;

        void Awake()
        {
            if (microphoneRecord == null)
            {
                Debug.LogError("[PushToTalkRecorder] MicrophoneRecord non assegnato!");
                return;
            }
            microphoneRecord.OnRecordStop += OnRecordStopHandler;
        }

        void OnDestroy()
        {
            if (microphoneRecord != null)
                microphoneRecord.OnRecordStop -= OnRecordStopHandler;
        }

        // ── API pubblica — collega questi due metodi al grip sinistro ───
        public void StartRecording()
        {
            if (microphoneRecord == null) return;
            if (microphoneRecord.IsRecording) return;

            if (whisperManager == null || !whisperManager.IsLoaded)
            {
                Debug.LogWarning("[PushToTalkRecorder] WhisperManager non pronto.");
                return;
            }

            microphoneRecord.StartRecord();
            if (logToConsole) Debug.Log("[PushToTalkRecorder] Registrazione avviata...");
        }

        public void StopRecording()
        {
            if (microphoneRecord == null) return;
            if (!microphoneRecord.IsRecording) return;

            microphoneRecord.StopRecord();
            if (logToConsole) Debug.Log("[PushToTalkRecorder] Registrazione fermata, trascrivo...");
        }

        // ── Callback quando MicrophoneRecord ha finito ─────────────────
        async void OnRecordStopHandler(AudioChunk recordedAudio)
        {
            if (recordedAudio.Length < 0.3f)
            {
                if (logToConsole) Debug.Log("[PushToTalkRecorder] Audio troppo corto, ignorato.");
                return;
            }

            var result = await whisperManager.GetTextAsync(
                recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);

            if (result == null || string.IsNullOrWhiteSpace(result.Result))
            {
                if (logToConsole) Debug.Log("[PushToTalkRecorder] Nessun testo riconosciuto.");
                return;
            }

            string text = result.Result.Trim();
            if (logToConsole) Debug.Log($"[PushToTalkRecorder] Trascritto: \"{text}\"");

            OnTranscriptionReady?.Invoke(text);
        }
    }
}