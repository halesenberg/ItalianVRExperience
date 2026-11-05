using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Systems;
using Bellavalle.Voice;

namespace Bellavalle.Scene
{
    /// <summary>
    /// Singleton di scena. Esegue gli alberi di dialogo:
    ///  - riproduce audio NPC (fast/slow in base al livello)
    ///  - mostra le opzioni al player: a bottoni (default) OPPURE a voce
    ///    quando il nodo ha useVoiceInput = true (il canvas mostra solo il
    ///    testo da dire, il player risponde parlando — PushToTalk + Whisper)
    ///  - per i nodi con isNameCapture = true lascia il campo libero:
    ///    mostra solo il prompt "parla ora" e aspetta che un handler esterno
    ///    (es. NameInputHandler) faccia avanzare il dialogo
    ///  - per i nodi senza opzioni (battute di chiusura, isVocabIntro, ecc.)
    ///    mostra la battuta e avanza da solo dopo una breve pausa
    ///  - gestisce timeout → FreezeDetected
    ///  - applica effetti del nodo (mood, memoria, flag, vocabolario)
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        // ── Eventi pubblici ────────────────────────────────────────────
        /// <summary>Emesso quando il player ha completato un nodo di dialogo con l'opzione corretta.</summary>
        public event System.Action<DialogueNode, PlayerOption> OnOptionCompleted;

        // ── Riferimenti ────────────────────────────────────────────────
        [SerializeField] DialogueUI ui;
        [SerializeField] AudioSource npcAudioSource;
        [SerializeField] WorldFeedback worldFeedback;
        [SerializeField] HapticFeedback haptic;
        [SerializeField] WordFlashUI wordFlash;

        [Header("Input vocale (nodi con useVoiceInput = true)")]
        [Tooltip("Lo stesso PushToTalkRecorder collegato al grip sinistro del player. " +
                 "Serve per i nodi useVoiceInput (scelta tra frasi) — i nodi isNameCapture " +
                 "restano gestiti da NameInputHandler.")]
        [SerializeField] PushToTalkRecorder pushToTalkRecorder;

        [Tooltip("Se il player parla e nessuna voiceTrigger combacia, quante volte " +
                 "mostrare il suggerimento prima di lasciar scattare il timeout normale.")]
        [SerializeField] int maxVoiceRetriesBeforeHint = 2;

        [Tooltip("Volume dei toni che segnalano l'apertura ('puoi parlare') e la chiusura per " +
                 "timeout ('tempo scaduto') della finestra di ascolto vocale.")]
        [Range(0f, 1f)]
        [SerializeField] float voiceReadyChimeVolume = 0.6f;

        [Header("Debug")]
        [Tooltip("Logga in Console quando un nodo entra in ascolto vocale, cosa arriva da Whisper, " +
                 "e se combacia con una voiceTrigger. Utile per capire perché il riconoscimento " +
                 "non sembra funzionare senza dover indovinare.")]
        [SerializeField] bool logVoiceDebug = true;

        Animator npcAnimator;   // Animator dell'NPC in dialogo (per gesti sui nodi)

        // ── Stato corrente ─────────────────────────────────────────────
        DialogueTree _tree;
        DialogueNode _currentNode;
        NPCData _npcData;
        Coroutine _timeoutCoroutine;

        bool _listeningForVoiceOption;
        int _voiceRetryCount;

        public bool IsActive => _currentNode != null;

        // ── Unity lifecycle ────────────────────────────────────────────
        void OnEnable()
        {
            if (pushToTalkRecorder == null && PersistentWhisper.Instance != null)
                pushToTalkRecorder = PersistentWhisper.Instance.PushToTalkRecorder;

            if (pushToTalkRecorder != null)
            {
                pushToTalkRecorder.OnTranscriptionReady += OnVoiceTranscribed;
                pushToTalkRecorder.OnRecordingStarted += OnMicRecordingStarted;
            }
        }

        void OnDisable()
        {
            if (pushToTalkRecorder != null)
            {
                pushToTalkRecorder.OnTranscriptionReady -= OnVoiceTranscribed;
                pushToTalkRecorder.OnRecordingStarted -= OnMicRecordingStarted;
            }
        }

        // ── API pubblica ───────────────────────────────────────────────
        public void Begin(DialogueTree tree, string entryNodeId, NPCData npc, Animator npcAnim = null)
        {
            if (IsActive)
            {
                Debug.LogWarning($"[DialogueManager] Begin('{tree.treeId}') IGNORATO — c'è già un dialogo " +
                    $"attivo (nodo corrente: '{_currentNode?.nodeId}', albero: '{_tree?.treeId}'). " +
                    "Il dialogo precedente non è mai arrivato a EndDialogue() — controlla come è finito " +
                    "(o non è mai finito) quel dialogo prima di questo.");
                return;
            }
            _tree = tree;
            _npcData = npc;
            npcAnimator = npcAnim;

            if (logVoiceDebug)
            {
                Debug.Log($"[DialogueManager] Begin('{tree.treeId}', entry='{entryNodeId}') — " +
                    $"pushToTalkRecorder {(pushToTalkRecorder != null ? "assegnato" : "NON assegnato!")}, " +
                    $"ui {(ui != null ? "assegnata" : "NON assegnata!")}");
            }

            EventBus.Emit(GameEvent.DialogueStarted, tree.treeId);
            GoToNode(entryNodeId);
        }

        /// <summary>
        /// Permette a sistemi esterni (es. TicketDeliveryHandler, NameInputHandler)
        /// di forzare l'avanzamento del dialogo a un nodo specifico.
        /// Usato per meccaniche fisiche o input vocale libero che sostituiscono
        /// la scelta a bottoni (es. il nome del player, o la consegna fisica di un
        /// biglietto invece di scegliere "Ecco il biglietto").
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
            StopListeningForVoice();

            if (string.IsNullOrEmpty(nodeId)) { EndDialogue(); return; }
            var node = _tree.GetNode(nodeId);
            if (node == null) { EndDialogue(); return; }

            EventBus.Emit(GameEvent.NodeEntered, node);   // ←

            StartCoroutine(ExecuteNode(node));
        }

        IEnumerator ExecuteNode(DialogueNode node)
        {
            _currentNode = node;
            _voiceRetryCount = 0;

            // 0. Gesto animato del nodo (se impostato) — non deve MAI bloccare il dialogo
            if (npcAnimator != null && !string.IsNullOrEmpty(node.animationTrigger))
            {
                bool exists = false;
                foreach (var p in npcAnimator.parameters)
                    if (p.type == AnimatorControllerParameterType.Trigger && p.name == node.animationTrigger)
                    { exists = true; break; }

                if (exists)
                {
                    Debug.Log($"[Dialogue] Setto trigger '{node.animationTrigger}' su {npcAnimator.gameObject.name}");
                    npcAnimator.SetTrigger(node.animationTrigger);
                }
                else
                {
                    Debug.LogWarning($"[Dialogue] Trigger '{node.animationTrigger}' non esiste nell'Animator — ignorato");
                }
            }

            // 1. Riproduci audio NPC (lento se A1, veloce se A2+)
            PlayNpcAudio(node);

            // 2. Aspetta la fine dell'audio (o un minimo di 1s)
            float waitTime = npcAudioSource.clip != null
                ? npcAudioSource.clip.length + 0.3f
                : 1.5f;
            yield return new WaitForSeconds(waitTime);

            bool hasOptions = node.options != null && node.options.Length > 0;

            // ── Caso 1: cattura nome libera — il campo lo gestisce un handler esterno
            //    (es. NameInputHandler), qui mostriamo solo la battuta + il prompt "parla ora".
            if (node.isNameCapture)
            {
                if (logVoiceDebug) Debug.Log($"[DialogueManager] Nodo '{node.nodeId}' → modalità CATTURA NOME (isNameCapture).");

                ui.ShowNamePrompt(node);

                // Gli effetti del nodo (flag/vocabolario/missione) non dipendono da
                // un'opzione qui — se ci sono, applicali subito alla comparsa del nodo.
                ApplyNodeEffects(node);

                // Timeout di sicurezza: se per qualche motivo nessun handler esterno
                // fa avanzare il dialogo (es. NameInputHandler mancante in scena),
                // non restiamo bloccati per sempre.
                StartNodeTimeoutIfNeeded(node);
                yield break;
            }

            // ── Caso 2: nodo senza opzioni CHE NON aspetta nessuno — battuta di
            //    chiusura / passaggio lineare. Mostra solo la riga e avanza da solo.
            //
            //    ATTENZIONE: un nodo senza opzioni può anche essere un nodo che
            //    aspetta un evento ESTERNO (es. TicketDeliveryHandler, che chiama
            //    GoToNodePublic() quando il player consegna fisicamente un
            //    oggetto — vedi "controllore_biglietto/node_01"). Quei nodi
            //    NON vanno fatti avanzare da soli: usano un timeoutSeconds reale
            //    (>0) apposta per aspettare ed eventualmente ripetersi, esattamente
            //    come il vecchio comportamento a bottoni con zero bottoni.
            //    Il modo per distinguerli senza aggiungere nuovi campi: chi ha
            //    scritto questi dialoghi ha sempre usato timeoutSeconds = 0 per
            //    dire "battuta di chiusura, non aspettare nessuno" e un valore
            //    reale (5, 10...) quando invece serve aspettare qualcosa. Quindi:
            //    avanza da solo SOLO se timeoutSeconds <= 0.
            bool isLinearClosingLine = !hasOptions && node.timeoutSeconds <= 0f;

            if (isLinearClosingLine)
            {
                if (logVoiceDebug) Debug.Log($"[DialogueManager] Nodo '{node.nodeId}' → battuta LINEARE, avanzo da solo.");

                ui.ShowLinearLine(node);

                float hold = node.isVocabIntro ? Mathf.Max(node.vocabHoldSeconds, 0.5f) : 1.2f;
                yield return new WaitForSeconds(hold);

                ui.HideOptions();
                ApplyNodeEffects(node);
                GoToNode(node.linearNextNodeId);
                yield break;
            }

            // ── Caso 3: risposta a voce — il canvas mostra le frasi che il player
            //    deve DIRE (non sono bottoni cliccabili), poi si ascolta il microfono.
            if (node.useVoiceInput)
            {
                if (logVoiceDebug)
                {
                    string triggerList = hasOptions
                        ? string.Join("  |  ", node.options.Select(o =>
                            $"\"{o.text_IT}\" → [{string.Join(", ", o.voiceTriggers ?? new string[0])}]"))
                        : "(nessuna opzione)";
                    Debug.Log($"[DialogueManager] Nodo '{node.nodeId}' → IN ASCOLTO VOCALE. Trigger validi: {triggerList}");
                }

                ui.ShowVoiceOptions(node);
                _listeningForVoiceOption = true;
                // Il chime "via libera" NON parte qui: partirebbe prima che il microfono sia
                // davvero acceso (il player deve ancora fare il gesto + aspettare il suo delay),
                // rischiando di far iniziare a parlare mentre la registrazione non è ancora
                // attiva e tagliare le prime sillabe. Parte invece in OnMicRecordingStarted,
                // agganciato a PushToTalkRecorder.OnRecordingStarted — sincrono con l'istante
                // reale in cui il microfono comincia a catturare.
                StartNodeTimeoutIfNeeded(node);
                yield break;
            }

            // ── Caso 4 (default/legacy): bottoni cliccabili ─────────────────
            if (logVoiceDebug) Debug.Log($"[DialogueManager] Nodo '{node.nodeId}' → modalità BOTTONI (legacy).");

            ui.ShowOptions(node, OnOptionChosen, OnHelpChosen);
            StartNodeTimeoutIfNeeded(node);
        }

        void PlayNpcAudio(DialogueNode node)
        {
            // Prototipo A1: sempre la versione lenta/scandita se disponibile
            var clip = node.npcVoiceSlow != null
                ? node.npcVoiceSlow
                : node.npcVoice;

            npcAudioSource.clip = clip;
            if (clip == null) return;
            npcAudioSource.pitch = 1f;
            npcAudioSource.Play();
        }

        // ── Toni "puoi parlare ora" / "tempo scaduto" ───────────────────
        // Sorgente audio dedicata e separata da npcAudioSource: i toni non
        // devono mai interrompere o essere interrotti dalla battuta dell'NPC.
        AudioSource _chimeSource;
        AudioClip _voiceReadyChime;   // ascendente: A5 → D6, "via libera"
        AudioClip _voiceTimeoutChime; // discendente: D6 → A5, "tempo scaduto"

        void PlayVoiceReadyChime() => PlayChime(_voiceReadyChime ??= BuildTwoNoteChime(880f, 1174.66f));

        void PlayVoiceTimeoutChime() => PlayChime(_voiceTimeoutChime ??= BuildTwoNoteChime(1174.66f, 880f));

        void PlayChime(AudioClip clip)
        {
            if (_chimeSource == null)
            {
                _chimeSource = gameObject.AddComponent<AudioSource>();
                _chimeSource.playOnAwake = false;
                _chimeSource.spatialBlend = 0f; // 2D: sempre udibile, indipendentemente da dove guarda il player in VR
            }

            _chimeSource.PlayOneShot(clip, voiceReadyChimeVolume);
        }

        // Genera un breve tono a due note (stile walkie-talkie) senza bisogno di
        // importare un file audio: segnale non verbale, riconoscibile a colpo
        // d'orecchio. L'ordine delle due frequenze distingue "via libera"
        // (ascendente) da "tempo scaduto" (discendente).
        static AudioClip BuildTwoNoteChime(float firstHz, float secondHz)
        {
            const int sampleRate = 44100;
            float[] notesHz = { firstHz, secondHz };
            const float noteLength = 0.09f;
            const float gap = 0.02f;
            const float fadeSeconds = 0.01f;

            int noteSamples = Mathf.RoundToInt(sampleRate * noteLength);
            int gapSamples = Mathf.RoundToInt(sampleRate * gap);
            int fadeSamples = Mathf.RoundToInt(sampleRate * fadeSeconds);
            int totalSamples = noteSamples * notesHz.Length + gapSamples * (notesHz.Length - 1);

            var data = new float[totalSamples];
            int offset = 0;
            foreach (float freq in notesHz)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    float envelope = Mathf.Min(1f, Mathf.Min(i, noteSamples - i) / (float)fadeSamples);
                    data[offset + i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * envelope * 0.5f;
                }
                offset += noteSamples + gapSamples;
            }

            var clip = AudioClip.Create("TwoNoteChime", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ── Timeout ─────────────────────────────────────────────────────
        void StartNodeTimeoutIfNeeded(DialogueNode node)
        {
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);

            // timeoutSeconds <= 0 significa "nessun timeout" per questo nodo.
            if (node.timeoutSeconds <= 0f) return;

            _timeoutCoroutine = StartCoroutine(TimeoutRoutine(node));
        }

        IEnumerator TimeoutRoutine(DialogueNode node)
        {
            yield return new WaitForSeconds(node.timeoutSeconds);

            // Il dialogo potrebbe essere già avanzato nel frattempo (risposta vocale,
            // NameInputHandler, GoToNodePublic esterno...): se il nodo corrente è
            // cambiato questa coroutine è ormai obsoleta, non deve fare nulla.
            if (_currentNode == null || _currentNode.nodeId != node.nodeId) yield break;

            if (logVoiceDebug) Debug.Log($"[DialogueManager] Nodo '{node.nodeId}' → TIMEOUT ({node.timeoutSeconds}s) scaduto senza risposta valida.");

            // Il player non ha risposto in tempo alla finestra di ascolto vocale: un tono
            // discendente segnala "tempo scaduto", altrimenti scoprirebbe di aver perso il
            // turno solo quando l'NPC ripete la domanda da capo. Controlla node.useVoiceInput
            // (non _listeningForVoiceOption) perché il timeout può scadere anche nei pochi
            // istanti in cui l'ascolto è momentaneamente chiuso per un replay in corso
            // dopo un tentativo sbagliato (vedi ReplayQuestionAfterMismatch).
            if (node.useVoiceInput)
                PlayVoiceTimeoutChime();

            EventBus.Emit(GameEvent.FreezeDetected, _npcData.npcId);
            StopListeningForVoice();
            ui.HideOptions();

            // Vai al nodo di timeout se esiste, altrimenti ripete
            string next = string.IsNullOrEmpty(node.timeoutNodeId)
                ? node.nodeId
                : node.timeoutNodeId;

            GoToNode(next);
        }

        void StopListeningForVoice()
        {
            _listeningForVoiceOption = false;
            _voiceRetryCount = 0;
        }

        // ── Risposta player — bottoni ──────────────────────────────────
        void OnOptionChosen(int optionIndex)
        {
            if (_currentNode == null) return;
            SelectOption(_currentNode.options[optionIndex]);
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
            StartNodeTimeoutIfNeeded(_currentNode);
        }

        // Suona il "via libera" solo quando il microfono è DAVVERO acceso (evento da
        // PushToTalkRecorder), non quando il nodo apre la finestra di ascolto: tra le due
        // cose c'è il gesto del player + il suo delay di avvio, e anticipare il chime
        // induceva a parlare troppo presto tagliando le prime sillabe della risposta.
        void OnMicRecordingStarted()
        {
            if (!_listeningForVoiceOption) return;
            PlayVoiceReadyChime();
        }

        // ── Risposta player — voce ─────────────────────────────────────
        void OnVoiceTranscribed(string transcribedText)
        {
            if (!_listeningForVoiceOption || _currentNode == null)
            {
                if (logVoiceDebug)
                    Debug.Log($"[DialogueManager] Trascrizione ricevuta (\"{transcribedText}\") ma NON sto ascoltando in questo momento — ignorata.");
                return;
            }

            if (logVoiceDebug)
                Debug.Log($"[DialogueManager] In ascolto sul nodo '{_currentNode.nodeId}' — testo ricevuto da Whisper: \"{transcribedText}\"");

            int matchIndex = VoiceMatchUtils.FindMatchingOption(transcribedText, _currentNode.options);

            if (matchIndex < 0)
            {
                _voiceRetryCount++;
                Debug.Log($"[DialogueManager] Voce non riconosciuta: \"{transcribedText}\" — nessuna voiceTrigger combacia (tentativo {_voiceRetryCount}).");
                ui.ShowVoiceRetryHint(_voiceRetryCount >= maxVoiceRetriesBeforeHint);
                StartCoroutine(ReplayQuestionAfterMismatch(_currentNode));
                return; // il timeout del nodo continua a scorrere invariato: nessun reset sui tentativi
            }

            if (logVoiceDebug)
                Debug.Log($"[DialogueManager] Match trovato → opzione {matchIndex} (\"{_currentNode.options[matchIndex].text_IT}\"), avanzo il dialogo.");

            StopListeningForVoice();
            SelectOption(_currentNode.options[matchIndex]);
        }

        // Dopo un tentativo vocale non riconosciuto, ripete la domanda dell'NPC e poi
        // riapre l'ascolto (con lo stesso ding di "via libera"). Il timeout del nodo
        // NON viene toccato di proposito: continua a scorrere invariato sullo sfondo,
        // niente reset ad ogni tentativo (finestra fissa, come richiesto).
        IEnumerator ReplayQuestionAfterMismatch(DialogueNode node)
        {
            _listeningForVoiceOption = false;

            PlayNpcAudio(node);
            float waitTime = npcAudioSource.clip != null
                ? npcAudioSource.clip.length + 0.3f
                : 1.5f;
            yield return new WaitForSeconds(waitTime);

            // Il dialogo potrebbe essere avanzato nel frattempo (es. il timeout è scaduto
            // durante il replay): se il nodo è cambiato, non riaprire l'ascolto a vuoto.
            if (_currentNode == null || _currentNode.nodeId != node.nodeId) yield break;

            // Stesso discorso di ExecuteNode: il chime parte in OnMicRecordingStarted,
            // non qui, per restare sincrono con l'accensione reale del microfono.
            _listeningForVoiceOption = true;
        }

        // ── Logica condivisa di selezione opzione (bottone o voce) ─────
        void SelectOption(PlayerOption option)
        {
            if (_timeoutCoroutine != null) StopCoroutine(_timeoutCoroutine);
            StopListeningForVoice();
            ui.HideOptions();

            var node = _currentNode;

            // Registra risposta per confidence/progress
            GameManager.Instance.RegisterAnswer(option.isCorrect);
            EventBus.Emit(GameEvent.PlayerAnswered, option.isCorrect);

            // Vocabolario
            FlashWord(option.vocabularyTag);

            // Feedback aptico
            haptic?.Give(option.isCorrect);

            // Feedback mondo (recap contestuale)
            if (!string.IsNullOrEmpty(node.keyPhraseIT))
                worldFeedback?.Show(node.keyPhraseIT, node.worldAnchor, option.isCorrect, 4f);

            // Inventario (zaino) — salva domanda e risposta solo se corretta
            if (option.isCorrect)
            {
                if (!string.IsNullOrEmpty(node.npcLine_IT))
                    GameManager.Instance.LearnPhrase(
                        node.npcLine_IT, node.npcLine_EN, PhraseCategory.Domanda,
                        sourceNpcId: _npcData.npcId);

                if (!string.IsNullOrEmpty(option.text_IT))
                    GameManager.Instance.LearnPhrase(
                        option.text_IT, option.text_EN, PhraseCategory.Risposta,
                        sourceNpcId: _npcData.npcId);

                // Notifica ai listener che il nodo è stato completato con successo
                OnOptionCompleted?.Invoke(node, option);
            }

            ApplyNodeEffects(node);
            GoToNode(option.nextNodeId);
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
            StopListeningForVoice();
            ui.HideOptions();
            GameManager.Instance.SaveGame();
            EventBus.Emit(GameEvent.DialogueEnded, treeId);
        }
    }
}