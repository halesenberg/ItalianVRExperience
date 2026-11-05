using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Bellavalle.Scene;
using Bellavalle.Voice;
using Whisper;

namespace Bellavalle.UI
{
    /// <summary>
    /// Aggiunge la modalità listen-and-repeat a un oggetto interattabile del bar.
    /// Va messo sullo stesso GameObject di ObjectCanvasActivator e PanelAudioPlayer.
    ///
    /// Flusso:
    ///  1. Hover sull'oggetto → canvas si apre (ObjectCanvasActivator fa il suo)
    ///     ma il testo del nome è nascosto
    ///  2. Player preme il bottone audio quante volte vuole (PanelAudioPlayer)
    ///  3. Player fa PuntaSu → microfono attivo → ripete → abbassa le mani
    ///  4. Whisper trascrive → confronto con targetWord
    ///  5. Match → il testo appare + feedback visivo verde
    ///     No match → feedback visivo rosso + può riprovare
    ///  6. Ogni tentativo viene loggato nel CSV tramite VoiceRecognitionLogger
    ///
    /// Setup:
    ///  1. Metti questo script sullo stesso GameObject di ObjectCanvasActivator
    ///  2. targetWord = la parola da ripetere (es. "sgabello") — solo il nome,
    ///     senza articolo, in minuscolo
    ///  3. wordText = il TMP_Text del canvas che mostra "UNA Sgabello" —
    ///     viene nascosto all'apertura e rivelato al match
    ///  4. pushToTalkRecorder, dialogueManager, logger = riferimenti ai
    ///     componenti persistenti (assegnali nell'Inspector)
    ///  5. Il canvas si apre/chiude ancora tramite ObjectCanvasActivator —
    ///     questo script gestisce solo la logica listen-and-repeat
    /// </summary>
    public class ListenAndRepeatActivator : MonoBehaviour
    {
        [Header("Parola target")]
        [Tooltip("Frase completa con articolo, in minuscolo (es. 'una tazza') — l'articolo " +
                 "esatto non deve essere indovinato: al confronto basta un articolo qualsiasi " +
                 "(un/una/il/la/lo/l'/i/gli/le), perché Whisper li confonde facilmente tra loro")]
        [SerializeField] string targetWord;

        // Articoli italiani accettati al posto di quello configurato in targetWord: Whisper
        // (soprattutto con un modello piccolo) confonde spesso un articolo con l'altro, quindi
        // pretendere quello grammaticalmente esatto renderebbe il match troppo fragile. Basta
        // che il player ne dica uno qualsiasi insieme al nome giusto.
        static readonly string[] KnownArticles = { "un", "una", "uno", "il", "lo", "la", "l", "i", "gli", "le" };

        [Header("Riferimenti canvas")]
        [Tooltip("Il TMP_Text che mostra il nome — viene nascosto all'inizio e rivelato al match")]
        [SerializeField] TMP_Text wordText;

        [Header("Feedback visivo")]
        [Tooltip("Colore del testo quando il match è corretto")]
        [SerializeField] Color successColor = Color.green;
        [Tooltip("Colore del testo quando il match è sbagliato")]
        [SerializeField] Color failColor = Color.red;
        [Tooltip("Colore normale del testo")]
        [SerializeField] Color normalColor = Color.white;
        [Tooltip("Secondi in cui resta visibile il feedback prima di tornare normale")]
        [SerializeField] float feedbackDuration = 1.5f;

        [Header("Riferimenti persistenti")]
        [SerializeField] PushToTalkRecorder pushToTalkRecorder;
        [SerializeField] DialogueManager dialogueManager;
        [SerializeField] VoiceRecognitionLogger logger;

        // Stato interno
        bool _isOpen;
        bool _waitingForTranscription;
        int _attemptNumber;
        WhisperResult _pendingResult;
        Coroutine _feedbackCoroutine;

        // ── Singleton leggero per "uno alla volta" ──────────────────────
        static ListenAndRepeatActivator _activeInstance;

        // Espone l'istanza attualmente aperta (hover in corso) a chi deve
        // notificarle l'inizio della registrazione (GestureSpeechInput) —
        // senza questo, FindAnyObjectByType prenderebbe un'istanza arbitraria
        // tra le tante presenti in scena, non necessariamente quella aperta.
        public static ListenAndRepeatActivator ActiveInstance => _activeInstance;

        void OnEnable()
        {
            // Ascolta l'apertura/chiusura del canvas tramite XRInteractable
            var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (interactable != null)
            {
                interactable.hoverEntered.AddListener(OnHoverEntered);
                interactable.hoverExited.AddListener(OnHoverExited);
            }

            // Come in DialogueManager/NameInputHandler: dalla seconda scena in poi il campo
            // assegnato in Inspector può puntare alla copia locale (già distrutta) del prefab
            // WhisperManager — risolviamo sempre dall'istanza sopravvissuta.
            if (PersistentWhisper.Instance != null)
                pushToTalkRecorder = PersistentWhisper.Instance.PushToTalkRecorder;

            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionResultReady += OnTranscriptionResultReady;
        }

        void OnDisable()
        {
            var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (interactable != null)
            {
                interactable.hoverEntered.RemoveListener(OnHoverEntered);
                interactable.hoverExited.RemoveListener(OnHoverExited);
            }

            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionResultReady -= OnTranscriptionResultReady;

            if (_activeInstance == this)
                _activeInstance = null;
        }

        // ── Apertura canvas ────────────────────────────────────────────
        void OnHoverEntered(HoverEnterEventArgs args)
        {
            // Se c'è già un altro oggetto aperto, chiudilo prima
            if (_activeInstance != null && _activeInstance != this)
                _activeInstance.CloseCanvas();

            _activeInstance = this;
            _isOpen = true;
            _attemptNumber = 0;

            // Nascondi il testo del nome — il player deve sentirlo prima di vederlo
            if (wordText != null)
            {
                wordText.gameObject.SetActive(false);
                wordText.color = normalColor;
            }
        }

        void OnHoverExited(HoverExitEventArgs args)
        {
            CloseCanvas();
        }

        void CloseCanvas()
        {
            _isOpen = false;
            _waitingForTranscription = false;

            if (_activeInstance == this)
                _activeInstance = null;

            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
                _feedbackCoroutine = null;
            }
        }

        // ── Ricezione trascrizione ─────────────────────────────────────
        void OnTranscriptionResultReady(WhisperResult result)
        {
            // Ignora se il canvas non è aperto o se c'è un dialogo attivo
            if (!_isOpen) return;
            if (dialogueManager != null && dialogueManager.IsActive) return;
            if (!_waitingForTranscription) return;

            _waitingForTranscription = false;
            _attemptNumber++;

            // Normalizzato come nel dialogo (accenti/punteggiatura tolti) — prima non c'era
            // nessuna normalizzazione qui, quindi "caffè" trascritto con l'accento non
            // combaciava mai con "un caffe" scritto senza in targetWord.
            string transcribed = VoiceMatchUtils.Normalize(result.Result ?? "");

            // Confronto tollerante — nome giusto E un articolo qualsiasi, non necessariamente
            // quello esatto configurato in targetWord (vedi KnownArticles).
            bool isMatch = MatchesTargetWord(transcribed);

            // Log nel CSV — preferisci sempre l'istanza sopravvissuta (VoiceRecognitionLogger
            // è anch'esso persistente/singleton): il campo assegnato in Inspector può puntare
            // a una copia locale già distrutta, e "logger?." da solo non lo rileverebbe perché
            // l'operatore ?. non usa il controllo di uguaglianza "fake null" di Unity.
            var activeLogger = VoiceRecognitionLogger.Instance != null ? VoiceRecognitionLogger.Instance : logger;
            if (activeLogger != null)
                activeLogger.LogListenAndRepeat(targetWord, _attemptNumber, isMatch, result);

            // Feedback visivo
            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(ShowFeedback(isMatch));
        }

        // targetWord è "articolo nome" (es. "una tazza"). Il nome deve combaciare per intero,
        // ma per l'articolo basta uno qualsiasi tra KnownArticles — non necessariamente quello
        // scritto in targetWord, per assorbire le confusioni tipiche di Whisper (un/una/il/la...).
        bool MatchesTargetWord(string transcribedNormalized)
        {
            var targetWords = VoiceMatchUtils.Normalize(targetWord)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (targetWords.Length == 0) return false;

            string noun = targetWords.Length > 1
                ? string.Join(" ", targetWords.Skip(1))
                : targetWords[0];

            var bounded = " " + transcribedNormalized + " ";
            bool nounFound = bounded.Contains(" " + noun + " ");
            bool articleFound = KnownArticles.Any(a => bounded.Contains(" " + a + " "));

            return nounFound && articleFound;
        }

        IEnumerator ShowFeedback(bool isMatch)
        {
            if (wordText == null) yield break;

            // Mostra sempre il testo durante il feedback
            wordText.gameObject.SetActive(true);
            wordText.color = isMatch ? successColor : failColor;

            yield return new WaitForSeconds(feedbackDuration);

            if (isMatch)
            {
                // Match corretto — il testo resta visibile col colore normale
                wordText.color = normalColor;
            }
            else
            {
                // Match sbagliato — nasconde di nuovo il testo, il player può riprovare
                wordText.gameObject.SetActive(false);
                _waitingForTranscription = false; // reset per il prossimo tentativo
            }

            _feedbackCoroutine = null;
        }

        // ── API pubblica — chiamata da GestureSpeechInput indirettamente ─
        // Non serve chiamarla direttamente: il sistema funziona perché
        // OnTranscriptionResultReady è abbonato all'evento del recorder.
        // Questo metodo serve solo per segnalare che il player ha fatto
        // il gesto e sta per parlare — imposta il flag di attesa.
        public void NotifyRecordingStarted()
        {
            if (!_isOpen) return;
            if (dialogueManager != null && dialogueManager.IsActive) return;
            _waitingForTranscription = true;
        }
    }
}
