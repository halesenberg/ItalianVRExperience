using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bellavalle.Core;
using Bellavalle.Data;

namespace Bellavalle.Scene
{
    /// <summary>
    /// Canvas di dialogo. Fa SOLO da supporto visivo:
    ///  - la battuta dell'NPC (+ traduzione adattiva)
    ///  - per i nodi a bottoni: le opzioni cliccabili (comportamento legacy)
    ///  - per i nodi useVoiceInput: le stesse frasi ma SOLO come testo — il
    ///    player le legge e le dice ad alta voce, non le clicca — più
    ///    l'indicatore "parla ora"
    ///  - per i nodi isNameCapture: solo la battuta + il prompt "di' il tuo nome"
    ///  - per i nodi lineari (senza opzioni): solo la battuta, nessun input
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] Canvas dialogueCanvas;
        [SerializeField] Transform optionsContainer;
        [SerializeField] TMP_Text npcLineText;
        [SerializeField] TMP_Text translationText;
        [SerializeField] GameObject optionButtonPrefab;

        [Header("Input vocale — supporto visivo")]
        [Tooltip("Riga sotto la battuta, tipo: 'Parla tu!' oppure il prompt del nodo (voicePromptIT).")]
        [SerializeField] TMP_Text voicePromptText;
        [Tooltip("Icona/testo che indica che il microfono sta ascoltando (es. un puntino rosso o '🎙️').")]
        [SerializeField] GameObject listeningIndicator;
        [Tooltip("Messaggio breve mostrato se la voce non combacia con nessuna frase suggerita.")]
        [SerializeField] TMP_Text voiceRetryHintText;
        [SerializeField] string defaultVoicePrompt = "Parla tu!";
        [SerializeField] string retryHintMessage = "Non ho capito, riprova a dire una delle frasi.";
        [SerializeField] string retryHintMessageStrong = "Prova a dire una di queste frasi, ad alta voce.";

        // ── Stato ──────────────────────────────────────────────────────
        readonly List<GameObject> _spawnedButtons = new();
        Action<int> _onOptionChosen;
        Action _onHelpChosen;
        Coroutine _retryHintCoroutine;

        // ── Unity lifecycle ────────────────────────────────────────────
        void Start()
        {
            // Nascondi il contenuto ma tieni il canvas SEMPRE ATTIVO
            SetContentVisible(false);
            SetVoiceUIVisible(false);
        }

        // ── API pubblica — nodi a bottoni (legacy) ──────────────────────
        public void ShowOptions(DialogueNode node, Action<int> onOption, Action onHelp)
        {
            _onOptionChosen = onOption;
            _onHelpChosen = onHelp;

            ClearButtons();
            SetVoiceUIVisible(false);
            ShowNpcLine(node);

            for (int i = 0; i < node.options.Length; i++)
                SpawnOptionEntry(node.options[i], i, interactive: true);

            RebuildLayout();
            SetContentVisible(true);
        }

        // ── API pubblica — nodi useVoiceInput (risposta a voce) ─────────
        // Le frasi sono mostrate come testo, NON come bottoni: sono un
        // promemoria visivo, la risposta arriva sempre dal microfono.
        public void ShowVoiceOptions(DialogueNode node)
        {
            _onOptionChosen = null;
            _onHelpChosen = null;

            ClearButtons();
            ShowNpcLine(node);

            for (int i = 0; i < node.options.Length; i++)
                SpawnOptionEntry(node.options[i], i, interactive: false);

            RebuildLayout();

            string prompt = string.IsNullOrEmpty(node.voicePromptIT) ? defaultVoicePrompt : node.voicePromptIT;
            SetVoicePrompt(prompt);
            SetVoiceUIVisible(true);
            HideRetryHint();

            SetContentVisible(true);
        }

        // ── API pubblica — nodo isNameCapture (campo libero, gestito da
        //    un handler esterno tipo NameInputHandler) ───────────────────
        public void ShowNamePrompt(DialogueNode node)
        {
            _onOptionChosen = null;
            _onHelpChosen = null;

            ClearButtons();
            ShowNpcLine(node);
            RebuildLayout();

            string prompt = string.IsNullOrEmpty(node.voicePromptIT)
                ? "Tieni premuto il grip e parla."
                : node.voicePromptIT;
            SetVoicePrompt(prompt);
            SetVoiceUIVisible(true);
            HideRetryHint();

            SetContentVisible(true);
        }

        /// <summary>Aggiorna solo il prompt vocale (usato da NameInputHandler per i messaggi di stato).</summary>
        public void SetVoicePrompt(string message)
        {
            if (voicePromptText != null) voicePromptText.text = message;
        }

        // ── API pubblica — nodo lineare (solo battuta, nessun input) ────
        public void ShowLinearLine(DialogueNode node)
        {
            _onOptionChosen = null;
            _onHelpChosen = null;

            ClearButtons();
            SetVoiceUIVisible(false);
            ShowNpcLine(node);
            SetContentVisible(true);
        }

        // ── Feedback "non ho capito" durante l'ascolto vocale ───────────
        public void ShowVoiceRetryHint(bool strong)
        {
            if (voiceRetryHintText == null) return;

            voiceRetryHintText.text = strong ? retryHintMessageStrong : retryHintMessage;
            voiceRetryHintText.gameObject.SetActive(true);

            if (_retryHintCoroutine != null) StopCoroutine(_retryHintCoroutine);
            _retryHintCoroutine = StartCoroutine(HideRetryHintAfter(2.5f));
        }

        void HideRetryHint()
        {
            if (_retryHintCoroutine != null) StopCoroutine(_retryHintCoroutine);
            if (voiceRetryHintText != null) voiceRetryHintText.gameObject.SetActive(false);
        }

        IEnumerator HideRetryHintAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (voiceRetryHintText != null) voiceRetryHintText.gameObject.SetActive(false);
        }

        // ── Nascondi tutto ───────────────────────────────────────────────
        public void HideOptions()
        {
            ClearButtons();
            SetVoiceUIVisible(false);
            SetContentVisible(false);
        }

        // ── Battuta NPC + traduzione adattiva ────────────────────────────
        void ShowNpcLine(DialogueNode node)
        {
            if (npcLineText != null)
            {
                npcLineText.text = ReplacePlaceholders(node.npcLine_IT);
                npcLineText.gameObject.SetActive(true);
            }

            if (translationText != null)
            {
                bool showTranslation = ShouldShowTranslation() && !string.IsNullOrEmpty(node.npcLine_EN);
                translationText.gameObject.SetActive(showTranslation);
                if (showTranslation)
                    translationText.text = $"({ReplacePlaceholders(node.npcLine_EN)})";
            }
        }

        // ── Righe/bottoni opzione ─────────────────────────────────────────
        // interactive = true  → bottone cliccabile (flusso a bottoni, legacy)
        // interactive = false → solo testo, promemoria di cosa dire a voce
        void SpawnOptionEntry(PlayerOption option, int index, bool interactive)
        {
            var go = Instantiate(optionButtonPrefab, optionsContainer);
            _spawnedButtons.Add(go);

            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                // Bottoni cliccabili (interactive = true, flusso legacy): identico
                // all'originale, solo text_IT — non tocco l'aspetto delle altre scene.
                // Suggerimenti vocali (interactive = false): aggiungo la traduzione
                // tra parentesi quando i sottotitoli adattivi sono attivi, per aiutare
                // chi non sa ancora cosa dire.
                label.text = (!interactive && ShouldShowTranslation() && !string.IsNullOrEmpty(option.text_EN))
                    ? $"{option.text_IT}  ({option.text_EN})"
                    : option.text_IT;
            }

            if (option.isHelpOption)
                StyleAsHelpButton(go);

            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();

            if (interactive)
            {
                btn.interactable = true;
                int capturedIndex = index;
                bool capturedHelp = option.isHelpOption;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (capturedHelp) _onHelpChosen?.Invoke();
                    else _onOptionChosen?.Invoke(capturedIndex);
                });
            }
            else
            {
                // Solo testo: nessuna interazione, stile leggermente attenuato
                // per far capire che non è un bottone da premere.
                btn.onClick.RemoveAllListeners();
                btn.interactable = false;
                StyleAsVoiceHint(go);
            }
        }

        void StyleAsHelpButton(GameObject go)
        {
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = new Color(0.5f, 0.5f, 0.5f);
                label.fontSize *= 0.85f;
            }
        }

        void StyleAsVoiceHint(GameObject go)
        {
            var image = go.GetComponent<Image>();
            if (image != null)
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0.35f);

            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.fontStyle |= FontStyles.Italic;
        }

        void ClearButtons()
        {
            foreach (var b in _spawnedButtons)
                if (b != null) Destroy(b);
            _spawnedButtons.Clear();
        }

        void RebuildLayout()
        {
            if (optionsContainer is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        // ── Visibilità ──────────────────────────────────────────────────
        void SetContentVisible(bool visible)
        {
            if (npcLineText) npcLineText.gameObject.SetActive(visible);
            if (optionsContainer != null) optionsContainer.gameObject.SetActive(visible);

            // translationText, quando si MOSTRA il contenuto, resta gestita da
            // ShowNpcLine (che sa, nodo per nodo, se c'è davvero una traduzione
            // da mostrare) — qui la tocco solo per NASCONDERLA insieme al resto,
            // altrimenti rischierei di riaccenderla forzatamente sopra alla sua
            // decisione e mostrare testo di un nodo precedente.
            if (!visible && translationText) translationText.gameObject.SetActive(false);
        }

        void SetVoiceUIVisible(bool visible)
        {
            if (voicePromptText != null) voicePromptText.gameObject.SetActive(visible);
            if (listeningIndicator != null) listeningIndicator.SetActive(visible);
            if (!visible) HideRetryHint();
        }

        string ReplacePlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (GameManager.Instance == null) return text;
            return text.Replace("{playerName}", GameManager.Instance.State.playerName);
        }

        // ── Subtitling adattivo ────────────────────────────────────────
        bool ShouldShowTranslation()
        {
            if (GameManager.Instance == null) return true;
            var state = GameManager.Instance.State;
            return state.confidenceScore < 0.5f || state.subtitlesEN;
        }
    }
}