using UnityEngine;
using Whisper;
using Whisper.Utils;

namespace Bellavalle.Voice
{
    public class PushToTalkRecorder : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] WhisperManager whisperManager;
        [SerializeField] MicrophoneRecord microphoneRecord;

        [Header("Debug")]
        [SerializeField] bool logToConsole = true;

        public bool IsRecording => microphoneRecord != null && microphoneRecord.IsRecording;

        /// <summary>
        /// Testo trascritto — usato da DialogueManager, NameInputHandler, ecc.
        /// </summary>
        public event System.Action<string> OnTranscriptionReady;

        /// <summary>
        /// Risultato completo Whisper con token e confidenza — usato da VoiceRecognitionLogger.
        /// </summary>
        public event System.Action<WhisperResult> OnTranscriptionResultReady;

        /// <summary>
        /// Scatta appena la registrazione finisce (gesto manuale di stop O VAD-stop per
        /// silenzio), PRIMA che Whisper trascriva — a differenza degli eventi sopra, spara
        /// sempre, anche se l'audio risulta troppo corto o senza testo riconosciuto. Usato
        /// da GestureSpeechInput per sincronizzare il proprio stato quando è il VAD a fermare
        /// il microfono da solo, non il gesto "palmi in giù".
        /// </summary>
        public event System.Action OnRecordingStopped;

        /// <summary>
        /// Scatta appena la registrazione parte DAVVERO (microfono acceso), non quando
        /// il gesto viene rilevato o durante il delay che lo precede. Usato per sincronizzare
        /// il "via libera" sonoro con l'istante reale in cui il microfono inizia a catturare,
        /// invece di anticiparlo e rischiare di tagliare le prime sillabe di chi parla.
        /// </summary>
        public event System.Action OnRecordingStarted;

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
            OnRecordingStarted?.Invoke();
        }

        public void StopRecording()
        {
            if (microphoneRecord == null) return;
            if (!microphoneRecord.IsRecording) return;

            microphoneRecord.StopRecord();
            if (logToConsole) Debug.Log("[PushToTalkRecorder] Registrazione fermata, trascrivo...");
        }

        async void OnRecordStopHandler(AudioChunk recordedAudio)
        {
            OnRecordingStopped?.Invoke();

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

            OnTranscriptionResultReady?.Invoke(result);
            OnTranscriptionReady?.Invoke(text);
        }
    }
}