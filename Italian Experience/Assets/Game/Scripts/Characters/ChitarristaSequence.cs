using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Bellavalle.Core;
using Bellavalle.Scene;

namespace Bellavalle.Characters
{
    /// <summary>
    /// Il chitarrista non cammina come Lina o il controllore: resta fermo e
    /// risponde al pinch del player, scegliendo tra due alberi di dialogo a
    /// seconda che la missione "aiutalo a contare le monete" sia già stata
    /// completata o no.
    ///
    /// Condivide lo stesso DialogueManager della vicina in questa scena:
    /// prima di avviare un dialogo controlla che non ce ne sia già uno
    /// attivo, per non entrare in conflitto se il player pinza il
    /// chitarrista mentre sta parlando con Lina — altrimenti il SceneDirector
    /// coinvolto si segnerebbe come "avviato" senza che il dialogo sia mai
    /// davvero partito, restando bloccato per il resto della sessione.
    ///
    /// Il canvas di dialogo è ancorato (CanvasFollow) a un singolo target
    /// fisso — di norma Lina. Per farlo apparire vicino al chitarrista
    /// invece che rimanere ancorato a lei, lo si "reclama" temporaneamente
    /// all'inizio del dialogo e lo si restituisce a Lina quando finisce.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
    public class ChitarristaSequence : MonoBehaviour
    {
        [Header("Missione")]
        [SerializeField] string missionId = "musicista_distratto";

        [Header("Dialogo 1 — prima di aiutarlo a contare")]
        [SerializeField] SceneDirector beforeHelpDirector;
        [SerializeField] string beforeHelpTreeId = "chitarrista_grazie";

        [Header("Dialogo 2 — dopo l'aiuto")]
        [SerializeField] SceneDirector afterHelpDirector;
        [SerializeField] string afterHelpTreeId = "chitarrista_grazie_aiuto";

        [Header("Riferimenti condivisi")]
        [Tooltip("Lo stesso DialogueManager usato dalla vicina in questa scena — " +
                 "serve solo per controllare che non ci sia già un dialogo attivo.")]
        [SerializeField] DialogueManager dialogueManager;

        [Tooltip("Lo stesso CanvasFollow sul DialogueCanvas — va rimappato " +
                 "temporaneamente sul chitarrista mentre gli parli, e ripristinato " +
                 "sul suo target originale (di norma Lina) alla fine del dialogo.")]
        [SerializeField] CanvasFollow canvasFollow;

        [Header("Comportamento")]
        [SerializeField] float talkDistance = 2.5f;

        Transform _player;
        Transform _canvasPreviousTarget;

        void Awake()
        {
            var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnPlayerInteract);
        }

        void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        void OnEnable() => EventBus.On(GameEvent.DialogueEnded, OnDialogueEnded);
        void OnDisable() => EventBus.Off(GameEvent.DialogueEnded, OnDialogueEnded);

        void OnPlayerInteract(SelectEnterEventArgs args)
        {
            if (_player == null) return;
            if (Vector3.Distance(transform.position, _player.position) > talkDistance) return;

            // Non avviare un dialogo se ce n'è già uno in corso (es. con Lina).
            if (dialogueManager != null && dialogueManager.IsActive) return;

            bool helped = GameManager.Instance != null && GameManager.Instance.IsMissionDone(missionId);
            var director = helped ? afterHelpDirector : beforeHelpDirector;
            if (director == null) return;

            if (canvasFollow != null)
            {
                _canvasPreviousTarget = canvasFollow.target;
                canvasFollow.target = transform;
            }

            director.StartScene();
        }

        // Restituisce il canvas al suo target originale (Lina) a fine dialogo,
        // qualunque sia il modo in cui è terminato (completato o abbandonato).
        void OnDialogueEnded(object data)
        {
            string ended = data as string;
            if (ended != beforeHelpTreeId && ended != afterHelpTreeId) return;

            if (canvasFollow != null && _canvasPreviousTarget != null)
                canvasFollow.target = _canvasPreviousTarget;
        }
    }
}
