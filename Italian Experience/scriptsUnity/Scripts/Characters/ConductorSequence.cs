using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Bellavalle.Core;
using Bellavalle.Scene;

namespace Bellavalle.Characters
{
    /// <summary>
    /// Regista della sequenza guidata del bigliettaio (prologo):
    /// 1) cammina verso il player e chiede il biglietto (dialogo 1)
    /// 2) gira a destra ed esce (cammina il percorso)
    /// 3) si ferma, si gira verso il player e indica la strada (dialogo 2) -> fine
    /// </summary>
    public class ConductorSequence : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] Animator animator;
        [SerializeField] Transform player;                 // vuoto = trovato via tag "Player"

        [Header("Dialogo 1 - biglietto")]
        [SerializeField] SceneDirector ticketDirector;
        [SerializeField] string ticketTreeId;       // stesso treeId del SceneDirector biglietto

        [Header("Dialogo 2 - indicazioni")]
        [SerializeField] SceneDirector directionsDirector;
        [SerializeField] string directionsTreeId;   // treeId del SceneDirector indicazioni

        [Header("Percorso")]
        [SerializeField] Transform ticketPoint;          // WP1 - davanti al player
        [SerializeField] Transform[] exitPath;             // WP2, WP2b, WP2c, WP3 (in ordine)
        [SerializeField] Transform[] byebyePath;            // WP4_Byebye
        [SerializeField] int turnRightIndex = -1;          // indice waypoint dove scatta TurnRight

        [Header("Movimento")]
        [SerializeField] float walkSpeed = 1.2f;
        [SerializeField] float turnSpeed = 6f;
        [SerializeField] float arriveDistance = 0.08f;

        [Header("Fine")]
        [SerializeField] float endDelay = 1f;
        public UnityEvent onFinished;                      // collega qui la dissolvenza -> Quartiere

        static readonly int H_Walk = Animator.StringToHash("IsWalking");
        static readonly int H_TurnRight = Animator.StringToHash("TurnRight");

        bool _started;

        void Start()
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            EventBus.On(GameEvent.DialogueEnded, OnDialogueEnded);
        }
        void OnDestroy() => EventBus.Off(GameEvent.DialogueEnded, OnDialogueEnded);

        // Chiamato dalla zona-trigger quando il player arriva
        public void BeginSequence()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Phase1_Approach());
        }

        // 1) cammina fino al player e chiede il biglietto
        IEnumerator Phase1_Approach()
        {
            yield return WalkTo(ticketPoint);
            yield return FaceTarget(player);
            if (ticketDirector != null) ticketDirector.StartScene();
        }

        // instradamento in base a QUALE dialogo è finito
        void OnDialogueEnded(object data)
        {
            string ended = data as string;
            if (ended == ticketTreeId)
                StartCoroutine(Phase2_WalkOut());
            else if (ended == directionsTreeId)
                StartCoroutine(Phase3_Finish());
        }

        // 2) esce girando a destra, poi si rigira e indica
        IEnumerator Phase2_WalkOut()
        {
            for (int i = 0; i < exitPath.Length; i++)
            {
                if (i == turnRightIndex && animator != null)
                    animator.SetTrigger(H_TurnRight);
                yield return WalkTo(exitPath[i]);
            }
            yield return FaceTarget(player);
            if (directionsDirector != null) directionsDirector.StartScene();
        }

        // 3) fine -> dissolvenza
        IEnumerator Phase3_Finish()
        {
            yield return new WaitForSeconds(endDelay);

            for (int i = 0; i < byebyePath.Length; i++)
                yield return WalkTo(byebyePath[i]);

            onFinished?.Invoke();
        }

        // ── movimento ──────────────────────────────────────────────────
        IEnumerator WalkTo(Transform target)
        {
            if (target == null) yield break;
            if (animator != null) animator.SetBool(H_Walk, true);

            while (true)
            {
                Vector3 to = target.position - transform.position;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (flat.magnitude <= arriveDistance) break;

                if (flat.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(flat.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look,
                                                          Time.deltaTime * turnSpeed);
                }
                transform.position += to.normalized * walkSpeed * Time.deltaTime;
                yield return null;
            }
            transform.position = target.position;
            if (animator != null) animator.SetBool(H_Walk, false);
        }

        IEnumerator FaceTarget(Transform target)
        {
            if (target == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                Vector3 to = target.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(to.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look,
                                                          Time.deltaTime * turnSpeed);
                }
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}