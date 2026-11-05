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
        [Header("Scaffolding — supporto visivo sempre presente")]
        // Immagine/icona mostrata SEMPRE accanto al sottotitolo italiano
        // (es. l'NPC che indica se stessa, una freccia, l'oggetto di cui si parla).
        // Diverso dalla traduzione: non è "aiuto", è supporto contestuale di base.
        public Sprite visualAid;
        [Header("Scaffolding — vocabolario (nodo flashcard)")]
        // Se true, questo nodo non fa domande: mostra "parola + immagine + audio"
        // per introdurre un termine nuovo (es. "Pane"), poi avanza da solo.
        // options deve essere vuoto; usa linearNextNodeId per il nodo successivo.
        public bool isVocabIntro;
        public string vocabWord;
        public Sprite vocabImage;
        public AudioClip vocabAudio;
        public float vocabHoldSeconds = 3f;     // quanto restare sulla card prima di avanzare
        public string linearNextNodeId;         // nodo successivo (solo per isVocabIntro)
        [Header("Input vocale (alternativa ai bottoni)")]
        // Se true, questo nodo non mostra i bottoni: il player risponde a voce
        // (push-to-talk) e il testo trascritto viene confrontato con
        // PlayerOption.voiceTriggers per scegliere l'opzione.
        public bool useVoiceInput;
        // Se true (in combinazione con useVoiceInput), il nodo cattura testo
        // libero invece di fare pattern matching (es. "come ti chiami?").
        // In questo caso options deve essere vuoto: se ne occupa un handler
        // dedicato (es. NameInputHandler) che ascolta GameEvent.NodeEntered.
        public bool isNameCapture;
        // Messaggio mostrato sopra il prompt "parla ora" (vuoto = messaggio di default)
        [TextArea] public string voicePromptIT;
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
        // Pronuncia "modello" di questa risposta (es. doppiata dalla stessa voce
        // NPC, o da un doppiatore player) — opzionale. Se assegnata, compare nello
        // zaino così il player può risentire come si dice correttamente la frase.
        // Se vuota, la riga dello zaino resta solo testuale (nessun problema).
        public AudioClip modelAudioIT;
        // Parole/frasi chiave (italiano, minuscolo consigliato) che, se presenti
        // nella trascrizione Whisper del player, selezionano questa opzione.
        // Usato solo quando il nodo ha useVoiceInput = true. Es: {"si", "vengo da fuori"}
        public string[] voiceTriggers;
    }
}