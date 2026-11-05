using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Bellavalle.Missions
{
    /// <summary>
    /// Gestisce il respawn automatico degli oggetti di missione se cadono,
    /// se finiscono sotto il pavimento o se vengono lasciati cadere dal giocatore.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ItemRespawnManager : MonoBehaviour
    {
        [Header("Impostazioni Respawn")]
        [Tooltip("Punto della scena dove l'oggetto riappare. Se vuoto, usa la posizione iniziale di Start()")]
        [SerializeField] private Transform customSpawnPoint;

        [Tooltip("Quota Y minima: se l'oggetto cade sotto questa altezza, respawna (es. pavimento a Y=0, soglia a -0.5)")]
        [SerializeField] private float minimumYPosition = -0.5f;

        [Tooltip("Tempo in secondi da attendere prima di far respawnare l'oggetto dopo che � caduto a terra")]
        [SerializeField] private float respawnDelayOnDrop = 2.0f;

        [Tooltip("Se true, l'oggetto respawna automaticamente non appena viene lasciato andare dal player")]
        [SerializeField] private bool respawnImmediatelyWhenDropped = false;

        private XRGrabInteractable _grabInteractable;
        private Rigidbody _rigidbody;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Coroutine _respawnCoroutine;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _rigidbody = GetComponent<Rigidbody>();

            // Salva posizione e rotazione iniziali
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        private void OnEnable()
        {
            _grabInteractable.selectEntered.AddListener(OnItemGrabbed);
            _grabInteractable.selectExited.AddListener(OnItemDropped);
        }

        private void OnDisable()
        {
            _grabInteractable.selectEntered.RemoveListener(OnItemGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnItemDropped);
        }

        private void Update()
        {
            // Controlla se l'oggetto e' caduto sotto il pavimento (sotto la quota Y minima).
            // Passa dal coroutine con delay/guard (stesso usato da OnCollisionEnter) invece
            // di chiamare RespawnItem() direttamente: se il punto di respawn e' a sua volta
            // sotto minimumYPosition, una chiamata diretta qui si ritriggererebbe ogni singolo
            // frame senza mai fermarsi.
            if (!_grabInteractable.isSelected && transform.position.y < minimumYPosition && _respawnCoroutine == null)
            {
                _respawnCoroutine = StartCoroutine(RespawnAfterDelayRoutine());
            }
        }

        private void OnItemGrabbed(SelectEnterEventArgs args)
        {
            // Se l'oggetto viene ripreso in mano, annulla eventuale conto alla rovescia di respawn
            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = null;
            }
        }

        private void OnItemDropped(SelectExitEventArgs args)
        {
            if (respawnImmediatelyWhenDropped)
            {
                RespawnItem();
            }
            else
            {
                // Avvia il timer di respawn quando l'oggetto cade a terra
                if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = StartCoroutine(RespawnAfterDelayRoutine());
            }
        }

        private IEnumerator RespawnAfterDelayRoutine()
        {
            yield return new WaitForSeconds(respawnDelayOnDrop);

            // Respawn solo se non � stato afferrato nel frattempo
            if (!_grabInteractable.isSelected)
            {
                RespawnItem();
            }
        }

        public void RespawnItem()
        {
            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = null;
            }

            // Annulla velocit� e forze fisiche del Rigidbody
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            // Riposiziona l'oggetto al punto di spawn
            Vector3 targetPosition = (customSpawnPoint != null) ? customSpawnPoint.position : _initialPosition;
            Quaternion targetRotation = (customSpawnPoint != null) ? customSpawnPoint.rotation : _initialRotation;

            transform.position = targetPosition;
            transform.rotation = targetRotation;

            Debug.Log($"[RespawnManager] Oggetto '{gameObject.name}' riposizionato con successo a {targetPosition}.");
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Se tocca un oggetto con tag "Floor" o "Ground", avvia o forza il respawn
            if (collision.gameObject.CompareTag("Floor") || collision.gameObject.CompareTag("Ground"))
            {
                if (!_grabInteractable.isSelected && _respawnCoroutine == null)
                {
                    _respawnCoroutine = StartCoroutine(RespawnAfterDelayRoutine());
                }
            }
        }
    }
}