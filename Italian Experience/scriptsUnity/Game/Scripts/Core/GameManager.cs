using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Bellavalle.Core;

namespace Bellavalle
{
    /// <summary>
    /// Singleton persistente tra scene. Gestisce stato globale, capitoli e salvataggio.
    /// Va su un GameObject "GameManager" nella prima scena (00_Tutorial o 00_Prologo).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Stato ──────────────────────────────────────────────────────
        public GameState State { get; private set; }

        // ── Nomi scene (corrispondono a Build Settings) ─────────────────
        static readonly string[] ChapterScenes =
        {
            "00_Tutorial",
            "01_Prologo",
            "02_Quartiere",
            "03_Conoscenze",
            "04_Guai",
            "05_Festa"
        };

        // ── NPC id costanti (evita magic strings nel resto del codice) ──
        public static class NPC
        {
            public const string Luca = "luca";
            public const string Carla = "carla";
            public const string Giuseppe = "giuseppe";
            public const string Giulia = "giulia";
            public const string Enzo = "enzo";
            public const string Bigliettaio = "bigliettaio";
            public const string Chitarrista = "chitarrista";
        }


        // ── Unity lifecycle ────────────────────────────────────────────
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
            InitAllNpcs();
        }

        void OnApplicationQuit() => SaveGame();

        // ── Salvataggio ────────────────────────────────────────────────
        public void SaveGame() => SaveSystem.Save(State);
        public void LoadGame() => State = SaveSystem.Load();
        public void NewGame() { SaveSystem.Delete(); State = new GameState(); InitAllNpcs(); }

        // ── Navigazione capitoli ───────────────────────────────────────
        public void LoadChapter(int chapter)
        {
            State.currentChapter = chapter;
            State.currentScene = 0;
            SaveGame();
            SceneManager.LoadScene(ChapterScenes[Mathf.Clamp(chapter, 0, ChapterScenes.Length - 1)]);
        }

        public void NextChapter() => LoadChapter(State.currentChapter + 1);

        // ── Lingua ─────────────────────────────────────────────────────
        public void LearnWord(string word)
        {
            if (State.learnedWords.Contains(word)) return;
            State.learnedWords.Add(word);
            UpdateLanguageProgress();
            EventBus.Emit(GameEvent.WordLearned, word);
        }

        /// <summary>
        /// Registra una frase completa nell'inventario (zaino), categorizzata
        /// come Domanda, Risposta o Vocabolario, con traduzione EN.
        /// Chiamalo da DialogueManager/MissionManager quando il player
        /// completa uno scambio.
        /// </summary>
        public void LearnPhrase(string textIT, string textEN, PhraseCategory category,
                                 string audioClipName = null, string sourceNpcId = null)
        {
            var phrase = new LearnedPhrase(textIT, textEN, category, audioClipName, sourceNpcId);
            State.AddLearnedPhrase(phrase);
            EventBus.Emit(GameEvent.WordLearned, textIT);
        }

        public void RegisterAnswer(bool correct)
        {
            State.totalAnswers++;
            if (correct) State.correctAnswers++;

            // Aggiorna confidence per subtitling adattivo
            State.confidenceScore = Mathf.Clamp01(
                State.confidenceScore + (correct ? 0.15f : -0.1f));

            UpdateLanguageProgress();
        }

        void UpdateLanguageProgress()
        {
            // Progresso = media pesata tra parole apprese e success rate
            float wordProgress = Mathf.Clamp01(State.learnedWords.Count / 120f); // ~120 parole A1-A2
            float answerProgress = State.GetSuccessRate();
            State.languageProgress = wordProgress * 0.6f + answerProgress * 0.4f;
            EventBus.Emit(GameEvent.LanguageProgressChanged, State.languageProgress);
        }

        // ── NPC ────────────────────────────────────────────────────────
        void InitAllNpcs()
        {
            State.InitNpc(NPC.Luca);
            State.InitNpc(NPC.Carla);
            State.InitNpc(NPC.Giuseppe);
            State.InitNpc(NPC.Giulia);
            State.InitNpc(NPC.Enzo);
            State.InitNpc(NPC.Bigliettaio);
            State.InitNpc(NPC.Chitarrista);
        }

        public void NpcRemember(string npcId, string fact)
        {
            if (!State.npcMemories.ContainsKey(npcId))
                State.npcMemories[npcId] = new();
            if (!State.npcMemories[npcId].Contains(fact))
                State.npcMemories[npcId].Add(fact);
        }

        public bool NpcKnows(string npcId, string fact) =>
            State.npcMemories.TryGetValue(npcId, out var m) && m.Contains(fact);

        // ── Flag narrativi ─────────────────────────────────────────────
        public void SetFlag(string flag) => State.SetFlag(flag);
        public bool HasFlag(string flag) => State.HasFlag(flag);

        // ── Missioni ───────────────────────────────────────────────────
        public void CompleteMission(string id)
        {
            if (!State.completedMissions.Contains(id))
                State.completedMissions.Add(id);
            EventBus.Emit(GameEvent.MissionCompleted, id);
        }
        public bool IsMissionDone(string id) => State.completedMissions.Contains(id);
    }
}