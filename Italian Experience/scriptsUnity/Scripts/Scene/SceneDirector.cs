using UnityEngine;
using UnityEngine.Events;
using Bellavalle.Core;
using Bellavalle.Data;

namespace Bellavalle.Scene
{
    /// <summary>
    /// Un SceneDirector per ogni scena narrativa (es. Bar_Cap1_Scena1).
    /// Gestisce:
    ///  - condizioni di ingresso (flag necessari)
    ///  - quale albero dialogo avviare
    ///  - evento onSceneComplete per la progressione
    /// </summary>
    public class SceneDirector : MonoBehaviour
    {
        // ── Setup ──────────────────────────────────────────────────────
        [Header("Identificazione")]
        [SerializeField] string sceneId;             // es. "bar_cap1_sc1"
        [SerializeField] int    chapterIndex;
        [SerializeField] int    sceneIndex;

        [Header("Dialogo")]
        [SerializeField] NPCData   npcData;          // NPC protagonista di questa scena
        [SerializeField] string    dialogueTreeId;   // treeId nell'NPCData
        [SerializeField] string    entryNodeOverride; // lascia vuoto per usare il default del tree

        [Header("Condizioni")]
        [SerializeField] string[]  requiredFlags;    // scene bloccate finché non hai i flag
        [SerializeField] bool      skipIfDone = true; // salta se già completata

        [Header("Trigger")]
        [SerializeField] float     triggerRadius = 2f;
        [SerializeField] Transform npcSpawnPoint;

        [Header("Events")]
        public UnityEvent onSceneStarted;
        public UnityEvent onSceneComplete;

        // ── Stato ──────────────────────────────────────────────────────
        bool _started   = false;
        bool _completed = false;

        // ── Riferimento al DialogueManager (trovato in scena) ──────────
        DialogueManager _dialogueManager;

        void Start()
        {
            _dialogueManager = FindObjectOfType<DialogueManager>();

            // Se già completata in una sessione precedente, salta
            if (skipIfDone && GameManager.Instance.IsMissionDone(sceneId))
            {
                _completed = true;
                return;
            }

            EventBus.On(GameEvent.DialogueEnded, OnDialogueEnded);
        }

        void OnDestroy() => EventBus.Off(GameEvent.DialogueEnded, OnDialogueEnded);

        // ── Trigger di prossimità ──────────────────────────────────────
        void OnTriggerEnter(Collider other)
        {
            if (_started || _completed) return;
            if (!other.CompareTag("Player"))  return;
            if (!ConditionsMet())             return;
            StartScene();
        }

        bool ConditionsMet()
        {
            foreach (var flag in requiredFlags)
                if (!GameManager.Instance.HasFlag(flag)) return false;
            return true;
        }

        // ── Avvio scena ────────────────────────────────────────────────
        public void StartScene()
        {
            if (_started || _completed || _dialogueManager == null) return;
            _started = true;

            // Recupera il tree dall'NPCData
            DialogueTree tree = GetTree();
            if (tree == null) { Debug.LogError($"[SceneDirector] Tree '{dialogueTreeId}' non trovato."); return; }

            string entryNode = string.IsNullOrEmpty(entryNodeOverride)
                ? tree.entryNodeId
                : entryNodeOverride;

            onSceneStarted?.Invoke();
            _dialogueManager.Begin(tree, entryNode, npcData);
        }

        // ── Fine scena (chiamato dall'EventBus) ────────────────────────
        void OnDialogueEnded(object data)
        {
            // data = treeId della conversazione appena finita
            if ((string)data != dialogueTreeId) return;
            CompleteScene();
        }

        void CompleteScene()
        {
            if (_completed) return;
            _completed = true;
            GameManager.Instance.CompleteMission(sceneId);
            GameManager.Instance.State.currentScene = sceneIndex + 1;
            onSceneComplete?.Invoke();
        }

        // ── Helper ─────────────────────────────────────────────────────
        DialogueTree GetTree()
        {
            if (npcData == null) return null;
            foreach (var ch in npcData.chapters)
            {
                if (ch.chapterIndex != chapterIndex) continue;
                foreach (var t in ch.trees)
                    if (t.treeId == dialogueTreeId) return t;
            }
            return null;
        }

        // ── Gizmo per il trigger radius ────────────────────────────────
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
