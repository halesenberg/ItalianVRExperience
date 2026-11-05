using UnityEngine;
using TMPro;
using Bellavalle.Core;
using Bellavalle.Scene;
using Bellavalle.Voice;

namespace Bellavalle.Missions
{
    /// <summary>
    /// Gestisce il momento in cui un NPC chiede il nome al player.
    /// Invece di mostrare bottoni A/B/C, il DialogueManager mostra un prompt
    /// "parla ora" (tramite DialogueUI.ShowNamePrompt) e questo script ascolta
    /// il push-to-talk. Il nome trascritto viene salvato in GameState.playerName
    /// e il dialogo avanza al nodo successivo.
    ///
    /// Ora è DATA-DRIVEN: si attiva su QUALSIASI nodo con isNameCapture = true
    /// nel NPCData (non serve più configurare un nodeId fisso a mano), e il
    /// nodo successivo è node.linearNextNodeId (lo stesso campo già usato per
    /// i nodi isVocabIntro). Funziona quindi per la vicina come per qualunque
    /// altro NPC che in futuro chieda il nome.
    ///
    /// Setup:
    ///  1. Metti questo script su un GameObject in scena (es. vicino al
    ///     DialogueManager o al WhisperManager)
    ///  2. Assegna pushToTalkRecorder, dialogueManager
    ///  3. (Opzionale ma consigliato) assegna dialogueUI: il prompt "parla ora"
    ///     verrà mostrato nel canvas di dialogo invece che in un pannello a parte
    ///  4. Se dialogueUI non è assegnato, puoi ancora usare speakPromptPanel/Text
    ///     come pannello separato (comportamento legacy)
    ///  5. Nel NPCData, il nodo dove l'NPC chiede il nome deve avere
    ///     isNameCapture = true, options vuoto, e linearNextNodeId = il nodo
    ///     a cui andare dopo aver ricevuto il nome
    /// </summary>
    public class NameInputHandler : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] PushToTalkRecorder pushToTalkRecorder;
        [SerializeField] DialogueManager dialogueManager;

        [Header("UI — preferito: passa dal canvas di dialogo")]
        [Tooltip("Se assegnato, i messaggi di stato (parla ora / ascolto / nome ricevuto) " +
                 "vengono mostrati tramite DialogueUI.SetVoicePrompt, cioè nello stesso " +
                 "canvas del dialogo. Consigliato per tenere un solo sistema visivo.")]
        [SerializeField] DialogueUI dialogueUI;

        [Header("UI legacy — pannello separato (usato solo se dialogueUI è vuoto)")]
        [SerializeField] GameObject speakPromptPanel;
        [SerializeField] TMP_Text speakPromptText;

        [Header("Messaggi")]
        [SerializeField] string promptMessage = "Tieni premuto il grip e di' il tuo nome.";
        [SerializeField] string retryMessage = "Non ho capito. Riprova: tieni premuto il grip e di' il tuo nome.";
        [SerializeField] string confirmMessage = "Piacere di conoscerti, {name}!";

        [Header("Fallback legacy (usati solo se il nodo non ha isNameCapture/linearNextNodeId)")]
        [SerializeField] string legacyListenNodeId = "";
        [SerializeField] string legacyNextNodeId = "";

        bool _isListening;
        bool _nameReceived;
        string _pendingNextNodeId;

        void OnEnable()
        {
            EventBus.On(GameEvent.NodeEntered, OnNodeEntered);

            // Vedi il commento in DialogueManager.OnEnable(): dalla seconda scena in poi
            // il campo assegnato in Inspector può puntare alla copia locale (già distrutta)
            // del prefab WhisperManager — risolviamo sempre dall'istanza sopravvissuta.
            if (PersistentWhisper.Instance != null)
                pushToTalkRecorder = PersistentWhisper.Instance.PushToTalkRecorder;

            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionReady += OnNameTranscribed;
        }

        void OnDisable()
        {
            EventBus.Off(GameEvent.NodeEntered, OnNodeEntered);

            if (pushToTalkRecorder != null)
                pushToTalkRecorder.OnTranscriptionReady -= OnNameTranscribed;
        }

        void Start()
        {
            if (speakPromptPanel != null)
                speakPromptPanel.SetActive(false);
        }

        // ── Quando il dialogo raggiunge un nodo "chiedi nome" ──────────
        void OnNodeEntered(object data)
        {
            var node = data as Bellavalle.Data.DialogueNode;
            if (node == null) return;

            bool isTarget = node.isNameCapture || (!string.IsNullOrEmpty(legacyListenNodeId) && node.nodeId == legacyListenNodeId);
            if (!isTarget) return;
            if (_nameReceived) return;

            _pendingNextNodeId = !string.IsNullOrEmpty(node.linearNextNodeId)
                ? node.linearNextNodeId
                : legacyNextNodeId;

            if (string.IsNullOrEmpty(_pendingNextNodeId))
                Debug.LogWarning($"[NameInputHandler] Nodo '{node.nodeId}' non ha un nodo successivo " +
                                  "(linearNextNodeId vuoto e nessun legacyNextNodeId): il dialogo finirà qui dopo il nome.");

            _isListening = true;
            ShowPrompt(promptMessage);
        }

        // ── Quando Whisper trascrive il nome ────────────────────────────
        void OnNameTranscribed(string transcribedText)
        {
            if (!_isListening) return;
            _isListening = false;
            _nameReceived = true;

            string cleanName = CleanTranscribedName(transcribedText);

            if (string.IsNullOrWhiteSpace(cleanName))
            {
                // Non ha capito — riprova
                ShowPrompt(retryMessage);
                _isListening = true;
                _nameReceived = false;
                return;
            }

            Debug.Log($"[NameInputHandler] Nome ricevuto: \"{cleanName}\"");

            GameManager.Instance.State.playerName = cleanName;
            GameManager.Instance.SaveGame();

            ShowPrompt(confirmMessage.Replace("{name}", cleanName));

            StartCoroutine(AdvanceAfterDelay(2f));
        }

        System.Collections.IEnumerator AdvanceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HidePrompt();

            if (string.IsNullOrEmpty(_pendingNextNodeId))
            {
                Debug.LogWarning("[NameInputHandler] Nessun nodo successivo impostato — il dialogo non avanza.");
                yield break;
            }

            dialogueManager.GoToNodePublic(_pendingNextNodeId);
        }

        // ── UI prompt ──────────────────────────────────────────────────
        void ShowPrompt(string message)
        {
            if (dialogueUI != null)
            {
                dialogueUI.SetVoicePrompt(message);
                return;
            }

            if (speakPromptText != null) speakPromptText.text = message;
            if (speakPromptPanel != null) speakPromptPanel.SetActive(true);
        }

        void HidePrompt()
        {
            if (dialogueUI != null) return; // il DialogueManager nasconderà tutto al cambio nodo
            if (speakPromptPanel != null) speakPromptPanel.SetActive(false);
        }

        // ── Pulizia testo trascritto ────────────────────────────────────
        string CleanTranscribedName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string clean = raw.Trim()
                .TrimEnd('.', ',', '!', '?', ';', ':')
                .Trim();

            string lower = clean.ToLower();
            string[] prefixes = {
                "mi chiamo ", "sono ", "il mio nome è ", "il mio nome e ",
                "my name is ", "i'm ", "i am "
            };

            foreach (var prefix in prefixes)
            {
                if (lower.StartsWith(prefix))
                {
                    clean = clean.Substring(prefix.Length).Trim();
                    break;
                }
            }

            if (clean.Length > 0)
                clean = char.ToUpper(clean[0]) + clean.Substring(1);

            if (clean.Length > 30 || clean.StartsWith("["))
                return null;

            return clean;
        }
    }
}