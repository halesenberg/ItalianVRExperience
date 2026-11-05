using System;
using System.Collections.Generic;

namespace Bellavalle.Core
{
    [Serializable]
    public class GameState
    {
        // ── Progressione narrativa ──────────────────────────────────────
        public int currentChapter = 0;       // 0=Prologo, 1-4=Capitoli
        public int currentScene = 0;
        public string playerName = "Marco";  // scelto all'inizio

        // ── Lingua ─────────────────────────────────────────────────────
        public List<string> learnedWords = new();
        public List<LearnedPhrase> learnedPhrases = new();  // per l'inventario (zaino)
        public float languageProgress = 0f;   // 0-1 (A1→A2)
        public int correctAnswers = 0;
        public int totalAnswers = 0;

        // ── Relazioni NPC (chiave = npcId) ─────────────────────────────
        public Dictionary<string, int> npcRelLevel = new();   // 0-3
        public Dictionary<string, List<string>> npcMemories = new();

        // ── Missioni ───────────────────────────────────────────────────
        public List<string> completedMissions = new();
        public List<string> activeFlags = new();  // flag narrativi generici

        // ── Preferenze player ──────────────────────────────────────────
        public bool subtitlesEN = true;
        public float confidenceScore = 0f;   // usato per l'analisi di progresso (non più per mostrare/nascondere sottotitoli in automatico)
        public bool tutorialDone = false;
        public int shoppingDifficulty = 0;   // 0=visuale, 1=solo audio

        // ── Analisi scaffolding (aiuto richiesto) ───────────────────────
        // Quante volte il player ha premuto "Aiuto" (traduzione on-demand) o
        // "Ripeti audio", e su quali nodi. Utile per capire quali dialoghi/
        // parole creano più difficoltà e se il player diventa più autonomo.
        public int helpRequestsCount = 0;
        public int repeatAudioCount = 0;
        public List<string> helpRequestedNodeIds = new();

        // ── Helper ─────────────────────────────────────────────────────
        public void SetFlag(string flag)
        {
            if (!activeFlags.Contains(flag)) activeFlags.Add(flag);
        }

        public bool HasFlag(string flag) => activeFlags.Contains(flag);

        public void InitNpc(string npcId)
        {
            if (!npcRelLevel.ContainsKey(npcId)) npcRelLevel[npcId] = 0;
            if (!npcMemories.ContainsKey(npcId)) npcMemories[npcId] = new List<string>();
        }

        public float GetSuccessRate() =>
            totalAnswers == 0 ? 0f : (float)correctAnswers / totalAnswers;

        // ── Inventario frasi (zaino) ─────────────────────────────────────
        public void AddLearnedPhrase(LearnedPhrase phrase)
        {
            // Evita voci duplicate nello zaino: stessa frase italiana + stessa categoria.
            // Se la stessa frase arriva da un NPC diverso da quello registrato la
            // prima volta, aggiorna l'attribuzione (fonte + audio) invece di
            // scartarla silenziosamente — altrimenti lo zaino resterebbe per
            // sempre attribuito al primo NPC incontrato anche se il player l'ha
            // effettivamente imparata di nuovo da un altro.
            foreach (var p in learnedPhrases)
            {
                if (p.textIT != phrase.textIT || p.category != phrase.category) continue;

                p.sourceNpcId = phrase.sourceNpcId;
                p.audioClip = phrase.audioClip;
                p.textEN = phrase.textEN;
                return;
            }

            learnedPhrases.Add(phrase);
        }

        public List<LearnedPhrase> GetPhrasesByCategory(PhraseCategory category)
        {
            var result = new List<LearnedPhrase>();
            foreach (var p in learnedPhrases)
                if (p.category == category) result.Add(p);
            return result;
        }


    }
}