using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.SceneManagement;
using Bellavalle.UI;
using Bellavalle.Voice;

namespace Bellavalle.Voice
{
    public class GestureSpeechInput : MonoBehaviour
    {
        public enum GestureMode { PuntaSu, CheVuoi }

        [Header("Riferimenti fissi (persistenti)")]
        [SerializeField] PushToTalkRecorder pushToTalk;

        FistDynamicMoveProvider moveProvider;
        InventoryUI inventoryUI;

        [Header("Quale gesto usare")]
        [SerializeField] GestureMode gestureMode = GestureMode.PuntaSu;

        [Header("Rilevamento — PuntaSu")]
        [Range(0f, 1f)]
        [SerializeField] float pointUpThreshold = 0.7f;
        [SerializeField] float curledDistance = 0.06f;

        [Header("Rilevamento — CheVuoi")]
        [SerializeField] float pinchDistance = 0.04f;
        [Range(0f, 1f)]
        [SerializeField] float cheVuoiUpThreshold = 0.6f;

        [Header("Rilevamento 'palmi in giù'")]
        [Range(0f, 1f)]
        [SerializeField] float palmDownThreshold = 0.7f;

        [Header("Delay avvio microfono")]
        [Tooltip("Secondi di attesa tra il gesto e l'avvio effettivo della registrazione")]
        [SerializeField] float micStartDelay = 1f;

        [Header("Debug")]
        [SerializeField] bool logToConsole = true;

        XRHandSubsystem _handSubsystem;
        bool _listening;
        Coroutine _startDelayCoroutine;

        [Header("Durata minima registrazione")]
        [Tooltip("Secondi minimi di registrazione prima che il gesto di stop venga accettato")]
        [SerializeField] float minRecordingTime = 2f;

        float _recordingStartTime;

        List<AudioSource> _ambientMusicSources = new();
        Dictionary<AudioSource, float> _ambientMusicVolumes = new();
        HashSet<AudioSource> _playingAudioSources = new();

#if UNITY_EDITOR
        bool _editorWasPressed;
#endif

        void Awake()
        {
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
                _handSubsystem = subsystems[0];
            else
                Debug.LogWarning("[GestureSpeechInput] XRHandSubsystem non trovato — in Editor usa Spazio per il microfono.");
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (pushToTalk != null) pushToTalk.OnRecordingStopped += HandleRecordingStopped;
            RefreshSceneReferences();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (pushToTalk != null) pushToTalk.OnRecordingStopped -= HandleRecordingStopped;
        }

        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // Questo componente è persistente (DontDestroyOnLoad) e sopravvive al cambio
            // scena. Se una registrazione era in corso, va fermata esplicitamente qui:
            // altrimenti _listening resterebbe bloccato a true e pushToTalk.StartRecording()
            // ignorerebbe silenziosamente ogni futuro tentativo (gesto, tastiera, VR) perché
            // crede che il microfono sia già occupato.
            if (_listening)
            {
                if (logToConsole) Debug.Log("[GestureSpeechInput] Cambio scena durante una registrazione attiva → fermo il microfono.");
                StopListening();
            }

            if (_startDelayCoroutine != null)
            {
                StopCoroutine(_startDelayCoroutine);
                _startDelayCoroutine = null;
            }

            RefreshSceneReferences();
        }

        void RefreshSceneReferences()
        {
            moveProvider = FindFirstObjectByType<FistDynamicMoveProvider>();
            inventoryUI = FindFirstObjectByType<InventoryUI>();

            _ambientMusicSources.Clear();
            _ambientMusicVolumes.Clear();
            var ambientObjects = GameObject.FindGameObjectsWithTag("AmbientMusic");
            foreach (var obj in ambientObjects)
            {
                var audioSource = obj.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    _ambientMusicSources.Add(audioSource);
                    _ambientMusicVolumes[audioSource] = audioSource.volume;
                }
            }

            if (logToConsole)
                Debug.Log($"[GestureSpeechInput] Riferimenti scena aggiornati — moveProvider: {(moveProvider != null ? "trovato" : "assente")}, inventoryUI: {(inventoryUI != null ? "trovato" : "assente")}, AmbientMusic sources: {_ambientMusicSources.Count}");
        }

        void Update()
        {
#if UNITY_EDITOR
            // Fallback da tastiera: Spazio = tieni premuto per registrare, rilascia per trascrivere.
            // Si attiva solo se il hand subsystem non è disponibile O le mani non sono tracciate,
            // così non interferisce se per qualche motivo il tracking funzionasse in Editor.
            bool handsUnavailable = _handSubsystem == null
                || (!_handSubsystem.leftHand.isTracked && !_handSubsystem.rightHand.isTracked);

            if (handsUnavailable)
            {
                if (logToConsole && _handSubsystem == null)
                    Debug.Log("[GestureSpeechInput] XRHandSubsystem not available → using SPACE key fallback");
                UpdateEditorFallback();
                return;
            }
#endif
            if (_handSubsystem == null) return;

            if (!_listening)
            {
                if (IsTargetGesture(_handSubsystem.leftHand) || IsTargetGesture(_handSubsystem.rightHand))
                {
                    if (_startDelayCoroutine == null)
                        _startDelayCoroutine = StartCoroutine(StartListeningDelayed());
                }
                else
                {
                    if (_startDelayCoroutine != null)
                    {
                        StopCoroutine(_startDelayCoroutine);
                        _startDelayCoroutine = null;
                        if (logToConsole) Debug.Log("[GestureSpeechInput] Gesto rilasciato durante il delay — microfono annullato.");
                    }
                }
            }
            else
            {
                if (IsPalmDown(_handSubsystem.leftHand) && IsPalmDown(_handSubsystem.rightHand))
                {
                    if (Time.time - _recordingStartTime >= minRecordingTime)
                        StopListening();
                }
            }
        }

#if UNITY_EDITOR
        void UpdateEditorFallback()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;

            bool isPressed = UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;

            if (isPressed && !_editorWasPressed)
            {
                if (logToConsole) Debug.Log("[GestureSpeechInput] [EDITOR] Spazio premuto → StartListening()");
                StartListening();
                _recordingStartTime = Time.time;
            }

            if (!isPressed && _editorWasPressed && _listening)
            {
                if (Time.time - _recordingStartTime >= minRecordingTime)
                {
                    if (logToConsole) Debug.Log("[GestureSpeechInput] [EDITOR] Spazio rilasciato → StopListening()");
                    StopListening();
                }
                else
                {
                    if (logToConsole) Debug.Log($"[GestureSpeechInput] [EDITOR] Spazio rilasciato troppo presto — tienilo premuto almeno {minRecordingTime}s.");
                }
            }

            _editorWasPressed = isPressed;
        }
#endif

        bool IsTargetGesture(XRHand hand) =>
            gestureMode == GestureMode.PuntaSu ? IsPointingUp(hand) : IsCheVuoi(hand);

        IEnumerator StartListeningDelayed()
        {
            if (logToConsole) Debug.Log($"[GestureSpeechInput] Gesto '{gestureMode}' rilevato → attendo {micStartDelay}s prima di attivare il microfono...");

            yield return new WaitForSeconds(micStartDelay);

            _startDelayCoroutine = null;
            StartListening();
            _recordingStartTime = Time.time;
        }

        void StartListening()
        {
            if (pushToTalk == null)
            {
                Debug.LogWarning("[GestureSpeechInput] Push To Talk non assegnato — il gesto non fa nulla.");
                return;
            }

            _listening = true;
            if (moveProvider != null) moveProvider.enabled = false;

            // ✅ Prima ferma la musica...
            _playingAudioSources.Clear();
            var allAudioSources = FindObjectsOfType<AudioSource>();
            foreach (var audioSource in allAudioSources)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    _playingAudioSources.Add(audioSource);
                    audioSource.Pause();
                }
            }

            // ✅ ...poi avvia il microfono
            pushToTalk.StartRecording();
            ListenAndRepeatActivator.ActiveInstance?.NotifyRecordingStarted();

            _recordingStartTime = Time.time;
            if (logToConsole)
            {
                Debug.Log($"[GestureSpeechInput] ════════════════════════════════════════");
                Debug.Log($"[GestureSpeechInput] ✓ GESTO RICONOSCIUTO: '{gestureMode}'");
                Debug.Log($"[GestureSpeechInput] • Microfono ATTIVATO");
                Debug.Log($"[GestureSpeechInput] • Movimento disattivato");
                Debug.Log($"[GestureSpeechInput] • {_playingAudioSources.Count} audio source(s) silenziato/i");
                Debug.Log($"[GestureSpeechInput] • In attesa: PARLA ORA");
                Debug.Log($"[GestureSpeechInput] ════════════════════════════════════════");
            }
        }
        // Chiamato solo dal gesto manuale "palmi in giù" (o rilascio Spazio in Editor):
        // ferma ATTIVAMENTE la registrazione. Il cleanup vero e proprio (musica, movimento,
        // stato _listening) avviene sempre in HandleRecordingStopped, che scatta da
        // pushToTalk.OnRecordingStopped — così funziona identico anche quando è il VAD a
        // fermare da solo il microfono per silenzio, senza bisogno del gesto di stop.
        void StopListening()
        {
            pushToTalk.StopRecording();
        }

        void HandleRecordingStopped()
        {
            if (!_listening) return; // già ripulito (es. gesto manuale appena passato di qui)
            _listening = false;

            int restoredCount = _playingAudioSources.Count;
            foreach (var audioSource in _playingAudioSources)
            {
                if (audioSource != null)
                    audioSource.Play();
            }
            _playingAudioSources.Clear();

            bool inventoryStillOpen = inventoryUI != null && inventoryUI.IsOpen;
            if (moveProvider != null && !inventoryStillOpen) moveProvider.enabled = true;

            if (logToConsole)
            {
                Debug.Log($"[GestureSpeechInput] ════════════════════════════════════════");
                Debug.Log($"[GestureSpeechInput] ✓ REGISTRAZIONE COMPLETATA");
                Debug.Log($"[GestureSpeechInput] • Microfono FERMATO");
                Debug.Log($"[GestureSpeechInput] • {restoredCount} audio source(s) ripristinato/i");
                Debug.Log($"[GestureSpeechInput] • In elaborazione: Whisper sta trascrivendo...");
                Debug.Log($"[GestureSpeechInput] ════════════════════════════════════════");
            }
        }

        bool IsPointingUp(XRHand hand)
        {
            if (!hand.isTracked) return false;
            try
            {
                var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
                var indexProximal = hand.GetJoint(XRHandJointID.IndexProximal);
                if (!indexTip.TryGetPose(out Pose tipPose)) return false;
                if (!indexProximal.TryGetPose(out Pose proxPose)) return false;

                Vector3 indexDir = (tipPose.position - proxPose.position).normalized;
                if (Vector3.Dot(indexDir, Vector3.up) <= pointUpThreshold) return false;

                var palmJoint = hand.GetJoint(XRHandJointID.Palm);
                if (!palmJoint.TryGetPose(out Pose palmPose)) return false;

                return IsFingerCurled(hand, XRHandJointID.MiddleTip, palmPose.position)
                    && IsFingerCurled(hand, XRHandJointID.RingTip, palmPose.position)
                    && IsFingerCurled(hand, XRHandJointID.LittleTip, palmPose.position);
            }
            catch { return false; }
        }

        bool IsFingerCurled(XRHand hand, XRHandJointID tipId, Vector3 palmPosition)
        {
            var tipJoint = hand.GetJoint(tipId);
            if (!tipJoint.TryGetPose(out Pose tipPose)) return false;
            return Vector3.Distance(tipPose.position, palmPosition) < curledDistance;
        }

        bool IsCheVuoi(XRHand hand)
        {
            if (!hand.isTracked) return false;
            try
            {
                var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
                var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
                var middleTip = hand.GetJoint(XRHandJointID.MiddleTip);
                var ringTip = hand.GetJoint(XRHandJointID.RingTip);
                var littleTip = hand.GetJoint(XRHandJointID.LittleTip);
                var wristJoint = hand.GetJoint(XRHandJointID.Wrist);

                if (!thumbTip.TryGetPose(out Pose thumbPose)) return false;
                if (!indexTip.TryGetPose(out Pose indexPose)) return false;
                if (!middleTip.TryGetPose(out Pose middlePose)) return false;
                if (!ringTip.TryGetPose(out Pose ringPose)) return false;
                if (!littleTip.TryGetPose(out Pose littlePose)) return false;
                if (!wristJoint.TryGetPose(out Pose wristPose)) return false;

                Vector3 centroid = (thumbPose.position + indexPose.position + middlePose.position
                                   + ringPose.position + littlePose.position) / 5f;

                bool allPinched =
                    Vector3.Distance(thumbPose.position, centroid) < pinchDistance &&
                    Vector3.Distance(indexPose.position, centroid) < pinchDistance &&
                    Vector3.Distance(middlePose.position, centroid) < pinchDistance &&
                    Vector3.Distance(ringPose.position, centroid) < pinchDistance &&
                    Vector3.Distance(littlePose.position, centroid) < pinchDistance;

                if (!allPinched) return false;

                Vector3 handDir = (centroid - wristPose.position).normalized;
                return Vector3.Dot(handDir, Vector3.up) > cheVuoiUpThreshold;
            }
            catch { return false; }
        }

        bool IsPalmDown(XRHand hand)
        {
            if (!hand.isTracked) return false;
            try
            {
                var palmJoint = hand.GetJoint(XRHandJointID.Palm);
                if (!palmJoint.TryGetPose(out Pose palmPose)) return false;

                float dot = Vector3.Dot(palmPose.rotation * Vector3.down, Vector3.down);
                return dot > palmDownThreshold;
            }
            catch { return false; }
        }
    }
}