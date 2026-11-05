using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Systems;

namespace Bellavalle.Scene
{
    /// <summary>
    /// Singleton di scena. Esegue gli alberi di dialogo:
    ///  - riproduce audio NPC (fast/slow in base al livello)
    ///  - mostra opzioni al player via DialogueUI
    ///  - gestisce timeout → FreezeDetected
    ///  - applica effetti del nodo (mood, memoria, flag, vocabolario)
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        // ── Riferimenti ────────────────────────────────────────────────
        [SerializeField] DialogueUI ui;
        [SerializeField] AudioSource npcAudioSource;
        [SerializeField] WorldFeedback worldFeedback;
        [SerializeField] HapticFeedback haptic;
        [SerializeField] WordFlashUI wordFlash;
        [SerializeField] Animator npcAnimator;   // Animator dell'NPC in dialogo (per gesti sui nodi)

        // ── Stato corrente ─────────────────────────────────────────────
        DialogueTree _tree;
        DialogueNode _currentNode;
        NPCData _npcData;
        Coroutine _timeoutCoroutine;

        bool IsActive => _currentNode != null;

        // ── API pubblica ───────────────────────────────────────────────
        public void Begin(DialogueTree tree, string entryNodeId, NPCData npc)
        {
            if (IsActive) return;
            _tree = tree;
            _npcData = npc;
            EventBus.Emit(GameEvent.DialogueStarted, tree.treeId);
            GoToNode(entryNodeId);
        }
        /// <summary>
        /// Permette a sistemi esterni (es. TicketDeliveryHandler) di forzare
        /// l'avanzamento del dialogo a un nodo specifico.
        /// Usato per meccaniche fisiche che sostituiscono la scelta a bottoni
        /// (es. consegna fisica del biglietto invece di scegliere "Ecco il biglietto").
        /// </summary>
        public void GoToNodePublic(string nodeId)
        {
            if (_tree == null)
            {
                Debug.LogWarning("[DialogueManager] GoToNodePublic chiamato ma nessun dialogo attivo.");
                return;
            }
            GoToNode(nodeId);
        }
        // ── Navigazione nodi ───────────────────────────────────────────
        void GoToNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) { EndDialogue(); return; }
            var node = _tree.GetNode(nodeId);
            if (node == null) { EndDialogue(); return; }
            StartCoroutine(ExecuteNode(node));
        }

        IEnumerator ExecuteNode(DialogueNode node)
        {
            _currentNode = node;

            // 0. Gesto animato del nodo (se impostato) — non deve MAI bloccare il dialogo
            if (npcAnimator != null && !string.IsNullOrEmpty(node.animationTrigger))
            {
                bool exists = false;
                foreach (var p in npcAnimator.parameters)
                    if (p.type == AnimatorControllerParameterType.Trigger && p.name == node.animationTrigger)
                    { exists = true; break; }

                if (exists) npcAnimator.SetTrigger(node.animationTrigger);
                else Debug.LogWarning($"[Dialogue] Trigger '{node.animationTrigger}' non esiste nell'Animator — ignorato");
            }
            // 1. Riproduci audio NPC (lento se A1, veloce se A2+)
            PlayNpcAudio(node);

            // 2. Aspetta la fine dell'audio (o un minimo di 1s)
            float waitTime = npcAudioSource.clip != null
                ? npcAudioSource.clip.length + 0.3f
                : 1.5f;
            yield return new WaitForSeconds(waitTime);

            // 3. Mostra le opzioni player
            ui.ShowOptions(node, OnOptionChosen, OnHelpChosen);

            // 4. Avvia timer timeout
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = StartCoroutine(TimeoutRoutine(node));
        }

        void PlayNpcAudio(DialogueNode node)
        {
            // Prototipo A1: sempre la versione lenta/scandita se disponibile
            var clip = node.npcVoiceSlow != null
                ? node.npcVoiceSlow
                : node.npcVoice;

            if (clip == null) return;
            npcAudioSource.pitch = 1f;
            npcAudioSource.clip = clip;
            npcAudioSource.Play();
        }

        // ── Risposta player ────────────────────────────────────────────
        void OnOptionChosen(int optionIndex)
        {
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            ui.HideOptions();

            var option = _currentNode.options[optionIndex];

            // Registra risposta per confidence/progress
            GameManager.Instance.RegisterAnswer(option.isCorrect);
            EventBus.Emit(GameEvent.PlayerAnswered, option.isCorrect);

            // Vocabolario
            FlashWord(option.vocabularyTag);

            // Feedback aptico
            haptic?.Give(option.isCorrect);

            // Feedback mondo (recap contestuale)
            if (!string.IsNullOrEmpty(_currentNode.keyPhraseIT))
                worldFeedback?.Show(_currentNode.keyPhraseIT,
                                    _currentNode.worldAnchor,
                                    option.isCorrect, 4f);

            // Inventario (zaino) — salva domanda e risposta solo se corretta
            if (option.isCorrect)
            {
                if (!string.IsNullOrEmpty(_currentNode.npcLine_IT))
                    GameManager.Instance.LearnPhrase(
                        _currentNode.npcLine_IT, _currentNode.npcLine_EN, PhraseCategory.Domanda,
                        sourceNpcId: _npcData.npcId);

                if (!string.IsNullOrEmpty(option.text_IT))
                    GameManager.Instance.LearnPhrase(
                        option.text_IT, option.text_EN, PhraseCategory.Risposta,
                        sourceNpcId: _npcData.npcId);
            }

            ApplyNodeEffects(_currentNode);
            GoToNode(option.nextNodeId);
        }

        void OnHelpChosen()
        {
            // "Non ho capito" — ripete il nodo da capo (stessa voce, lenta se non lo era)
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            ui.HideOptions();

            // Forza audio lento al secondo ascolto
            var slowClip = _currentNode.npcVoiceSlow ?? _currentNode.npcVoice;
            if (slowClip != null)
            {
                npcAudioSource.clip = slowClip;
                npcAudioSource.pitch = 0.85f;   // leggermente più lento se manca il clip slow
                npcAudioSource.Play();
            }

            StartCoroutine(ReshowOptionsAfterAudio(slowClip));
        }

        IEnumerator ReshowOptionsAfterAudio(AudioClip clip)
        {
            float wait = clip != null ? clip.length + 0.3f : 1.5f;
            yield return new WaitForSeconds(wait);
            ui.ShowOptions(_currentNode, OnOptionChosen, OnHelpChosen);
            _timeoutCoroutine = StartCoroutine(TimeoutRoutine(_currentNode));
        }

        // ── Timeout (freeze player) ────────────────────────────────────
        IEnumerator TimeoutRoutine(DialogueNode node)
        {
            yield return new WaitForSeconds(node.timeoutSeconds);

            EventBus.Emit(GameEvent.FreezeDetected, _npcData.npcId);
            ui.HideOptions();

            // Vai al nodo di timeout se esiste, altrimenti ripete
            string next = string.IsNullOrEmpty(node.timeoutNodeId)
                ? node.nodeId
                : node.timeoutNodeId;

            GoToNode(next);
        }

        // ── Effetti al completamento nodo ──────────────────────────────
        void ApplyNodeEffects(DialogueNode node)
        {
            if (node.setFlags != null)
                foreach (var f in node.setFlags)
                    GameManager.Instance.SetFlag(f);

            if (node.npcRememberFacts != null)
                foreach (var fact in node.npcRememberFacts)
                    GameManager.Instance.NpcRemember(_npcData.npcId, fact);

            if (!string.IsNullOrEmpty(node.completeMissionId))
                GameManager.Instance.CompleteMission(node.completeMissionId);

            FlashWord(node.vocabularyTag);
        }

        // ── Word flash + registrazione parola (ex LanguageTracker) ──────
        void FlashWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            GameManager.Instance.LearnWord(word);
            wordFlash?.Show(word);
        }

        // ── Fine dialogo ───────────────────────────────────────────────
        void EndDialogue()
        {
            string treeId = _tree?.treeId;
            _currentNode = null;
            _tree = null;
            _npcData = null;
            ui.HideOptions();
            GameManager.Instance.SaveGame();
            EventBus.Emit(GameEvent.DialogueEnded, treeId);
        }
    }
}