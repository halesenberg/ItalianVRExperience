using System.Collections;
using UnityEngine;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Scene;

namespace Bellavalle.Missions
{
    /// <summary>
    /// Orchestratore della missione "Il musicista distratto".
    ///
    /// Flusso attuale (fase 1 — dialogo, il conteggio vocale si aggiunge dopo):
    ///  1. Il player consegna la banconota a GiveToNPCZone (gia' costruito)
    ///  2. Questo manager riceve OnItemDelivered
    ///  3. Avvia il dialogo del chitarrista (DialogueManager esistente)
    ///  4. Alla fine del dialogo, completa la missione
    ///
    /// Setup:
    ///  1. Metti questo script su un GameObject in scena (es. vicino al
    ///     chitarrista, o sullo stesso GameObject di GiveToNPCZone)
    ///  2. Assegna giveToNPCZone (lo stesso GameObject/zona consegna)
    ///  3. Assegna npcData (NPCData del chitarrista) e dialogueTreeId
    ///  4. Assegna dialogueManager (quello in scena)
    /// </summary>
    public class MusicianMissionManager : MonoBehaviour
    {
        [Header("Identificazione missione")]
        [SerializeField] string missionId = "musicista_distratto";

        [Header("Consegna")]
        [SerializeField] GiveToNPCZone giveToNPCZone;
        [SerializeField] string expectedItemId = "banconota_10";

        [Header("Dialogo")]
        [SerializeField] NPCData npcData;
        [SerializeField] string dialogueTreeId = "chitarrista_grazie";
        [SerializeField] DialogueManager dialogueManager;

        bool _missionActive;

        void OnEnable()
        {
            if (giveToNPCZone != null)
                giveToNPCZone.OnItemDelivered += OnItemDelivered;

            EventBus.On(GameEvent.DialogueEnded, OnDialogueEnded);
        }

        void OnDisable()
        {
            if (giveToNPCZone != null)
                giveToNPCZone.OnItemDelivered -= OnItemDelivered;

            EventBus.Off(GameEvent.DialogueEnded, OnDialogueEnded);
        }

        // ── Consegna riuscita ────────────────────────────────────────────
        void OnItemDelivered(string itemId)
        {
            if (itemId != expectedItemId) return;
            if (GameManager.Instance.IsMissionDone(missionId)) return;

            StartCoroutine(StartMusicianDialogue());
        }

        IEnumerator StartMusicianDialogue()
        {
            _missionActive = true;

            // piccola pausa prima che il chitarrista reagisca
            yield return new WaitForSeconds(0.3f);

            var tree = GetTree();
            if (tree == null)
            {
                Debug.LogError($"[MusicianMissionManager] Tree '{dialogueTreeId}' non trovato in npcData.");
                yield break;
            }

            dialogueManager.Begin(tree, tree.entryNodeId, npcData);
        }

        // ── Fine dialogo → completa missione ───────────────────────────
        void OnDialogueEnded(object data)
        {
            if (!_missionActive) return;
            if ((string)data != dialogueTreeId) return;

            _missionActive = false;
            CompleteMission();
        }

        void CompleteMission()
        {
            GameManager.Instance.CompleteMission(missionId);
            GameManager.Instance.NpcRemember(GameManager.NPC.Chitarrista, "il_player_ha_trovato_i_soldi");
        }

        // ── Helper ─────────────────────────────────────────────────────
        DialogueTree GetTree()
        {
            if (npcData == null) return null;
            foreach (var chapter in npcData.chapters)
                foreach (var t in chapter.trees)
                    if (t.treeId == dialogueTreeId) return t;
            return null;
        }
    }
}
