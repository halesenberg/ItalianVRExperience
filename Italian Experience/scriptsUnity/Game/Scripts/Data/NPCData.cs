using System;
using System.Collections.Generic;
using UnityEngine;
namespace Bellavalle.Data
{
    /// <summary>
    /// ScriptableObject per ogni NPC.
    /// Crea con: tasto destro in Project → Create → Bellavalle → NPC Data
    /// Uno per personaggio: Luca.asset, Enzo.asset, ecc.
    /// </summary>
    [CreateAssetMenu(menuName = "Bellavalle/NPC Data", fileName = "NPC_")]
    public class NPCData : ScriptableObject
    {
        [Header("Identità")]
        public string npcId;           // "luca" — deve corrispondere a GameManager.NPC.*
        public string displayName;     // "Luca"
        [TextArea] public string bio;
        [Header("Voce")]
        public AudioClip greetingFast; // versione naturale
        public AudioClip greetingSlow; // versione pedagogica (più scandita)
        public float speechRate = 1f;   // moltiplicatore pitch fallback
        [Header("Funzioni linguistiche insegnate")]
        public string[] languageFunctions;
        [Header("Dialoghi per capitolo")]
        public ChapterDialogues[] chapters;
    }
    [Serializable]
    public class ChapterDialogues
    {
        public int chapterIndex;
        public DialogueTree[] trees;
    }
    // ── Albero dialogo ─────────────────────────────────────────────────
    [Serializable]
    public class DialogueTree
    {
        public string treeId;
        public string entryNodeId;
        public DialogueNode[] nodes;
        public DialogueNode GetNode(string id)
        {
            foreach (var n in nodes)
                if (n.nodeId == id) return n;
            Debug.LogWarning($"[DialogueTree] Nodo '{id}' non trovato in '{treeId}'");
            return null;
        }
    }
    [Serializable]
    public class DialogueNode
    {
        [Header("Contenuto")]
        public string nodeId;
        [TextArea] public string npcLine_IT;       // testo italiano NPC
        [TextArea] public string npcLine_EN;       // traduzione inglese (per zaino/sottotitoli)
        public AudioClip npcVoice;                 // doppiaggio naturale
        public AudioClip npcVoiceSlow;             // doppiaggio lento
        [Header("Opzioni player")]
        public PlayerOption[] options;
        [Header("Timeout")]
        public float timeoutSeconds = 10f;
        public string timeoutNodeId;           // nodo se il player non risponde
        [Header("Animazione")]
        public string animationTrigger;        // nome trigger Animator NPC (es. "Point", "Wave") — vuoto = niente
        [Header("Pedagogia")]
        public string vocabularyTag;           // parola/frase chiave da registrare
        public string keyPhraseIT;             // frase mostrata nel recap contestuale
        public Transform worldAnchor;             // oggetto 3D su cui mostrare il recap
        [Header("Condizioni")]
        public string[] requiredFlags;           // flags che devono essere attivi
        public string[] requiredMemories;        // memories NPC necessarie
        [Header("Effetti al completamento")]
        public string[] setFlags;                // flag da attivare
        public string[] npcRememberFacts;        // fatti che l'NPC memorizza
        public string completeMissionId;       // missione da completare
    }
    [Serializable]
    public class PlayerOption
    {
        public string text_IT;          // testo del bottone
        public string text_EN;          // traduzione inglese (per zaino/sottotitoli)
        public string nextNodeId;       // nodo destinazione ("" = fine albero)
        public bool isCorrect;        // per RegisterAnswer
        public bool isHelpOption;     // "Non ho capito" — sempre visibile
        public string vocabularyTag;    // parola appresa con questa scelta
        public AudioClip correctSound;   // suono haptic/audio feedback
    }
}