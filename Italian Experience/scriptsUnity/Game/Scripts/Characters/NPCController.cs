using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Bellavalle.Core;
using Bellavalle.Data;
using Bellavalle.Scene;

namespace Bellavalle.Characters
{
    /// <summary>
    /// Attacca questo component ai tuoi NPC placeholder esistenti.
    /// Si occupa di:
    ///  - intercettare l'interazione XRI (player si avvicina o punta)
    ///  - girare l'NPC verso il player durante il dialogo
    ///  - aggiornare animazioni in base al mood
    ///  - saluto automatico al livello relazione 2+
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
    public class NPCController : MonoBehaviour
    {
        [Header("Dati")]
        [SerializeField] NPCData npcData;

        [Header("Riferimenti scena")]
        [SerializeField] SceneDirector sceneDirector;   // SceneDirector della scena corrente
        [SerializeField] Animator      animator;

        [Header("Comportamento")]
        [SerializeField] float lookAtSpeed  = 3f;
        [SerializeField] float talkDistance = 2.5f;

        // ── Animator parameter hash ────────────────────────────────────
        static readonly int _hashTalking = Animator.StringToHash("IsTalking");
        static readonly int _hashGreet   = Animator.StringToHash("Greet");

        Transform _playerTransform;
        bool _isTalking;
        bool _hasAnimator;

        // ── Unity lifecycle ────────────────────────────────────────────
        void Awake()
        {
            var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnPlayerInteract);

            EventBus.On(GameEvent.DialogueStarted, OnDialogueStarted);
            EventBus.On(GameEvent.DialogueEnded, OnDialogueEnded);
        }

        void OnDestroy()
        {
            EventBus.Off(GameEvent.DialogueStarted, OnDialogueStarted);
            EventBus.Off(GameEvent.DialogueEnded, OnDialogueEnded);
        }

        void Start()
        {
            _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (animator == null) animator = GetComponentInChildren<Animator>();
            _hasAnimator = animator != null && animator.runtimeAnimatorController != null;

            CheckAutoGreet();
        }

        void Update()
        {
            if (_isTalking && _playerTransform != null)
                FacePlayer();
        }

        // ── Interazione ────────────────────────────────────────────────
        void OnPlayerInteract(SelectEnterEventArgs _)
        {
            if (_playerTransform == null) return;
            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            if (dist > talkDistance) return;

            sceneDirector?.StartScene();
        }

        // ── Event handlers ─────────────────────────────────────────────
        void OnDialogueStarted(object data)
        {
            _isTalking = true;
            if (_hasAnimator) animator.SetBool(_hashTalking, true);
        }

        void OnDialogueEnded(object data)
        {
            _isTalking = false;
            if (_hasAnimator) animator.SetBool(_hashTalking, false);
        }

        

        // ── Saluto automatico (relazione livello 2+) ───────────────────
        void CheckAutoGreet()
        {
            if (npcData == null) return;
            int relLevel = GameManager.Instance.State.npcRelLevel
                               .TryGetValue(npcData.npcId, out int v) ? v : 0;

            if (relLevel >= 2)
                StartCoroutine(AutoGreetRoutine());
        }

        System.Collections.IEnumerator AutoGreetRoutine()
        {
            yield return new WaitForSeconds(1f);
            if (_hasAnimator) animator.SetTrigger(_hashGreet);

            // Riproduci clip di saluto
            var audio = GetComponent<AudioSource>();
            if (audio != null && npcData.greetingSlow != null)
                audio.PlayOneShot(npcData.greetingSlow, 0.6f);
        }

        // ── Utilità ────────────────────────────────────────────────────
        void FacePlayer()
        {
            Vector3 dir = (_playerTransform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target,
                                                  Time.deltaTime * lookAtSpeed);
        }

        
    }
}
