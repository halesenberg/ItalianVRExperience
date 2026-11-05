using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bellavalle.Scene;
using Bellavalle.Core;
using Bellavalle.Data;

namespace Bellavalle.Voice
{
    /// <summary>
    /// Traccia quali parole il player ha incontrato nei dialoghi.
    /// Ascolta DialogueManager.OnOptionCompleted e estrae le parole da ogni nodo completato.
    /// Persiste tra le scene come singleton.
    /// </summary>
    public class DialogueWordTracker : MonoBehaviour
    {
        DialogueManager _dialogueManager;

        [Header("Debug")]
        [SerializeField] bool logToConsole = true;

        public static DialogueWordTracker Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            if (_dialogueManager == null)
                _dialogueManager = FindFirstObjectByType<DialogueManager>();

            if (_dialogueManager != null)
                _dialogueManager.OnOptionCompleted += OnOptionCompleted;
        }

        void OnDisable()
        {
            if (_dialogueManager != null)
                _dialogueManager.OnOptionCompleted -= OnOptionCompleted;
        }

        void OnOptionCompleted(DialogueNode node, PlayerOption option)
        {
            if (node == null) return;

            var npcLine = node.npcLine_IT;
            var playerOption = option.text_IT;

            // Estrai parole dal nodo
            var words = ExtractWords(npcLine, playerOption);

            // Salva in GameManager
            foreach (var word in words)
            {
                GameManager.Instance.LearnWord(word, node.nodeId ?? "unknown");

                if (logToConsole)
                    Debug.Log($"[DialogueWordTracker] Parola appresa: \"{word}\"");
            }
        }

        /// <summary>
        /// Estrae le parole rilevanti da una stringa di testo.
        /// Rimuove punteggiatura, converte in minuscolo, filtra articoli e preposizioni comuni.
        /// </summary>
        static List<string> ExtractWords(params string[] texts)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Parole molto comuni che non vale la pena tracciare
            var stopwords = new HashSet<string>
            {
                "il", "la", "i", "le", "lo", "gli", "le", "un", "una", "uno",
                "e", "o", "a", "di", "da", "in", "su", "per", "con", "senza",
                "è", "sei", "siamo", "siete", "sono", "ho", "hai", "ha", "abbiamo", "avete", "hanno",
                "mi", "ti", "si", "ci", "vi", "me", "te", "se", "lui", "lei", "noi", "voi", "loro",
                "che", "chi", "quale", "quanta", "quanto", "dove", "quando", "come",
                "scusa", "grazie", "prego", "per", "favore", "piacere"
            };

            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Rimuovi punteggiatura, converti in minuscolo
                var clean = System.Text.RegularExpressions.Regex.Replace(text, @"[^\w\s]", " ")
                    .ToLowerInvariant();

                var words = clean.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    if (!stopwords.Contains(word) && word.Length > 1)
                    {
                        result.Add(word);
                    }
                }
            }

            return result.ToList();
        }
    }
}
