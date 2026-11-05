using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bellavalle.Core;
using Bellavalle.Data;

namespace Bellavalle.Scene
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] Canvas dialogueCanvas;
        [SerializeField] Transform optionsContainer;
        [SerializeField] TMP_Text npcLineText;
        [SerializeField] TMP_Text translationText;
        [SerializeField] GameObject optionButtonPrefab;

        // ── Stato ──────────────────────────────────────────────────────
        readonly List<GameObject> _spawnedButtons = new();
        Action<int> _onOptionChosen;
        Action _onHelpChosen;

        // ── Unity lifecycle ────────────────────────────────────────────
        void Start()
        {
            // Nascondi il contenuto ma tieni il canvas SEMPRE ATTIVO
            SetContentVisible(false);
        }

        // ── API pubblica ───────────────────────────────────────────────
        public void ShowOptions(DialogueNode node,
                                Action<int> onOption,
                                Action onHelp)
        {
            _onOptionChosen = onOption;
            _onHelpChosen = onHelp;

            ClearButtons();

            // Testo NPC
            if (npcLineText != null)
            {
                npcLineText.text = node.npcLine_IT;
                npcLineText.gameObject.SetActive(true);
            }

            // Traduzione EN — popolata da npcLine_EN del nodo
            if (translationText != null)
            {
                bool showTranslation = ShouldShowTranslation() && !string.IsNullOrEmpty(node.npcLine_EN);
                if (showTranslation)
                    translationText.text = $"({node.npcLine_EN})";
                translationText.gameObject.SetActive(showTranslation);
            }

            // Crea bottoni
            for (int i = 0; i < node.options.Length; i++)
                SpawnOptionButton(node.options[i], i);

            // Forza rebuild layout
            if (optionsContainer is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            SetContentVisible(true);
        }

        public void HideOptions()
        {
            ClearButtons();
            SetContentVisible(false);
        }

        // ── Bottoni ────────────────────────────────────────────────────
        void SpawnOptionButton(PlayerOption option, int index)
        {
            var go = Instantiate(optionButtonPrefab, optionsContainer);
            _spawnedButtons.Add(go);

            // Testo
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = option.text_IT;

            // Stile "Non ho capito"
            if (option.isHelpOption)
                StyleAsHelpButton(go);

            // Button standard Unity — funziona con TrackedDeviceGraphicRaycaster
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();

            int capturedIndex = index;
            bool capturedHelp = option.isHelpOption;

            btn.onClick.AddListener(() =>
            {
                if (capturedHelp) _onHelpChosen?.Invoke();
                else _onOptionChosen?.Invoke(capturedIndex);
            });
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

        void ClearButtons()
        {
            foreach (var b in _spawnedButtons)
                if (b != null) Destroy(b);
            _spawnedButtons.Clear();
        }

        void SetContentVisible(bool visible)
        {
            if (npcLineText) npcLineText.gameObject.SetActive(visible);
            if (translationText) translationText.gameObject.SetActive(visible);
            if (optionsContainer != null) optionsContainer.gameObject.SetActive(visible);
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