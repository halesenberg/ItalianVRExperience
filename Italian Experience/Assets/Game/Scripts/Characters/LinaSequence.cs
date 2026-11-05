using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Bellavalle.Core;
using Bellavalle.Scene;

namespace Bellavalle.Characters
{
    public class LinaSequence : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] Animator animator;
        [SerializeField] Transform player;
        [SerializeField] Transform linaTransform;

        [Header("Dialogo 1 — Presentazione")]
        [SerializeField] SceneDirector presentazioneDirector;
        [SerializeField] string presentazioneTreeId = "carla_presentazione";

        [Header("Dialogo 2 — Casa")]
        [SerializeField] SceneDirector casaDirector;
        [SerializeField] string casaTreeId = "carla_casa";

        [Header("Waypoint")]
        [SerializeField] Transform spawnPoint;
        [SerializeField] Transform housePoint;
        [SerializeField] Transform idlePoint;
        [SerializeField] Transform[] approachWaypoints;  // spawn→spawn1→2→3→talk1
        [SerializeField] Transform[] returnWaypoints;    // spawn3→2→1→spawn→house1→...→house9

        [Header("Fase 1 — Incontro")]
        [SerializeField] float delayBeforeApproach = 60f;
        [SerializeField] float bumpDistance = 1.5f;

        [Header("Movimento")]
        [SerializeField] float walkSpeed = 1.2f;
        [SerializeField] float turnSpeed = 6f;
        [SerializeField] float arriveDistance = 0.15f;

        [Header("Fine")]
        [SerializeField] float pauseBeforeDialogue = 0.5f;
        public UnityEvent onAllDialoguesFinished;

        static readonly int H_Walk = Animator.StringToHash("IsWalking");
        static readonly int H_Talk = Animator.StringToHash("IsTalking");

        bool _started;
        int _currentPhase;

        void Start()
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (linaTransform == null)
                linaTransform = transform;

            if (spawnPoint != null)
                linaTransform.position = spawnPoint.position;

            EventBus.On(GameEvent.DialogueEnded, OnDialogueEnded);

            BeginSequence();
        }

        void OnDestroy()
        {
            EventBus.Off(GameEvent.DialogueEnded, OnDialogueEnded);
        }

        [ContextMenu("DEBUG: Avvia sequenza (con attesa e camminata)")]
        public void BeginSequence()
        {
            if (_started) return;
            _started = true;
            _currentPhase = 1;
            StartCoroutine(Phase1_WalkToBump());
        }

        [ContextMenu("DEBUG: Salta dritto al dialogo presentazione")]
        void DebugStartPresentazione()
        {
            if (presentazioneDirector != null)
                presentazioneDirector.StartScene();
        }

        IEnumerator Phase1_WalkToBump()
        {
            Debug.Log($"[Lina] Fase 1: aspetto {delayBeforeApproach}s");
            yield return new WaitForSeconds(delayBeforeApproach);

            // Segue i waypoint tranne l'ultimo
            Debug.Log("[Lina] Fase 1: seguo approachWaypoints");
            for (int i = 0; i < approachWaypoints.Length - 1; i++)
                yield return WalkTo(approachWaypoints[i]);

            // L'ultimo tratto: raggiunge il player ovunque sia
            Debug.Log("[Lina] Fase 1: cammino verso il player");
            yield return WalkToPlayer();

            yield return FaceTarget(player);
            yield return new WaitForSeconds(pauseBeforeDialogue);

            Debug.Log("[Lina] Fase 1: avvio dialogo presentazione");
            if (presentazioneDirector != null)
                presentazioneDirector.StartScene();
        }

        IEnumerator WalkToPlayer()
        {
            if (player == null) yield break;
            if (animator != null) animator.SetBool(H_Walk, true);

            while (true)
            {
                Vector3 to = player.position - linaTransform.position;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (flat.magnitude <= bumpDistance) break;

                if (flat.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(flat.normalized);
                    linaTransform.rotation = Quaternion.Slerp(
                        linaTransform.rotation, look, Time.deltaTime * turnSpeed);
                }
                linaTransform.position += flat.normalized * walkSpeed * Time.deltaTime;
                yield return null;
            }

            if (animator != null) animator.SetBool(H_Walk, false);
        }

        IEnumerator Phase2_WalkToHouse()
        {
            Debug.Log("[Lina] Fase 2: seguo i waypoint verso casa");

            foreach (var wp in returnWaypoints)
                yield return WalkTo(wp);

            yield return FaceTarget(player);
            yield return new WaitForSeconds(pauseBeforeDialogue);

            Debug.Log("[Lina] Fase 2: avvio dialogo casa");
            if (casaDirector != null)
                casaDirector.StartScene();
        }

        IEnumerator Phase3_WalkToIdleAndFinish()
        {
            Debug.Log("[Lina] Fase 3: cammino verso il punto idle");

            yield return WalkTo(idlePoint);
            yield return FaceTarget(player);
            SetIdle();

            Debug.Log("[Lina] Sequenza completata — Lina ha accompagnato il player a casa.");
            yield return new WaitForSeconds(1f);
            onAllDialoguesFinished?.Invoke();
        }

        void OnDialogueEnded(object data)
        {
            string ended = data as string;

            if (ended == presentazioneTreeId && _currentPhase == 1)
            {
                _currentPhase = 2;
                StartCoroutine(Phase2_WalkToHouse());
            }
            else if (ended == casaTreeId && _currentPhase == 2)
            {
                _currentPhase = 3;
                StartCoroutine(Phase3_WalkToIdleAndFinish());
            }
        }

        IEnumerator WalkTo(Transform target)
        {
            if (target == null) yield break;
            if (animator != null) animator.SetBool(H_Walk, true);

            int groundLayer = LayerMask.GetMask("Ground");

            while (true)
            {
                Vector3 to = target.position - linaTransform.position;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (flat.magnitude <= arriveDistance) break;

                if (flat.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(flat.normalized);
                    linaTransform.rotation = Quaternion.Slerp(
                        linaTransform.rotation, look, Time.deltaTime * turnSpeed);
                }

                linaTransform.position += flat.normalized * walkSpeed * Time.deltaTime;

                // Aggancia al pavimento
                if (Physics.Raycast(linaTransform.position + Vector3.up * 2f,
                                    Vector3.down, out RaycastHit hit, 3f, groundLayer))
                {
                    Vector3 pos = linaTransform.position;
                    pos.y = hit.point.y;
                    linaTransform.position = pos;
                }

                yield return null;
            }

            
            Vector3 finalPos = target.position;
            if (Physics.Raycast(finalPos + Vector3.up * 2f, Vector3.down, out RaycastHit finalHit, 5f, groundLayer))
                finalPos.y = finalHit.point.y;
            linaTransform.position = finalPos;
            if (animator != null) animator.SetBool(H_Walk, false);
        }

        IEnumerator FaceTarget(Transform target)
        {
            if (target == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                Vector3 to = target.position - linaTransform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(to.normalized);
                    linaTransform.rotation = Quaternion.Slerp(
                        linaTransform.rotation, look, Time.deltaTime * turnSpeed);
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        void SetIdle()
        {
            if (animator != null)
            {
                animator.SetBool(H_Walk, false);
                animator.SetBool(H_Talk, false);
            }
        }
    }
}