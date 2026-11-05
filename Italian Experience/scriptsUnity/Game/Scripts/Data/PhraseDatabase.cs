using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellavalle.Data
{
    [CreateAssetMenu(menuName = "Bellavalle/Phrase Database")]
    public class PhraseDatabase : ScriptableObject
    {
        public PhraseEntry[] entries;
        Dictionary<string, PhraseEntry> _byId;

        public PhraseEntry Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _byId ??= BuildIndex();
            return _byId.TryGetValue(id, out var e) ? e : null;
        }

        Dictionary<string, PhraseEntry> BuildIndex()
        {
            var d = new Dictionary<string, PhraseEntry>();
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.phraseId)) continue;
                d[e.phraseId] = e;
            }
            return d;
        }
    }

    [Serializable]
    public class PhraseEntry
    {
        public string phraseId;       // "enzo_chiavi_01" — univoco, stabile per sempre
        [TextArea] public string textIT;
        [TextArea] public string textEN;
        public AudioClip voiceClip;    // riferimento diretto, non stringa
        public Bellavalle.Core.PhraseCategory category;
        public string sourceNpcId;
    }
}