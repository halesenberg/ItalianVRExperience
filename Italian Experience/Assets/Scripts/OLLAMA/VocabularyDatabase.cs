using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellavalle.Data
{
    /// <summary>
    /// ScriptableObject con il vocabolario italiano dei giochi.
    /// Contiene tutte le parole incontrate nei dialoghi, con traduzioni verificate a mano.
    /// Anti-allucinazione: il tutor AI NON genera mai definizioni, le legge solo da qui.
    /// </summary>
    [CreateAssetMenu(menuName = "Bellavalle/Vocabulary Database")]
    public class VocabularyDatabase : ScriptableObject
    {
        [Tooltip("Una voce per parola. Le parole vanno in minuscolo.")]
        public VocabularyEntry[] entries;

        Dictionary<string, VocabularyEntry> _byWord;

        /// <summary>Cerca una parola per forma scritta italiana (case-insensitive).</summary>
        public VocabularyEntry Get(string italianWord)
        {
            if (string.IsNullOrWhiteSpace(italianWord)) return null;
            _byWord ??= BuildIndex();
            return _byWord.TryGetValue(Normalize(italianWord), out var e) ? e : null;
        }

        public bool Contains(string italianWord) => Get(italianWord) != null;

        static string Normalize(string s) => s.Trim().ToLowerInvariant();

        Dictionary<string, VocabularyEntry> BuildIndex()
        {
            var d = new Dictionary<string, VocabularyEntry>();
            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.word)) continue;
                d[Normalize(e.word)] = e;
            }
            return d;
        }
    }

    [Serializable]
    public class VocabularyEntry
    {
        [Tooltip("La parola italiana in minuscolo, univoca nel database.")]
        public string word;

        [Tooltip("Traduzione in inglese, per mostrare al player principiante.")]
        public string translation_EN;

        [Tooltip("Parte del discorso (noun, verb, adj, adv, prep, etc.) — informativo.")]
        public string partOfSpeech;

        [Tooltip("Breve spiegazione in inglese (opzionale), es. 'a hot drink made from coffee'.")]
        public string definition_EN;

        public string GetDefinition()
        {
            if (!string.IsNullOrEmpty(definition_EN))
                return $"\"{word}\" = \"{translation_EN}\" ({partOfSpeech}).\n{definition_EN}";
            else
                return $"\"{word}\" = \"{translation_EN}\" ({partOfSpeech}).";
        }
    }
}
