using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class VRDoorHandle : MonoBehaviour
{
    [Header("Riferimenti Porta")]
    [SerializeField] private Rigidbody doorRigidbody;
    [SerializeField] private HingeJoint doorHinge;
    [SerializeField] private Transform handleVisual; // Opzionale: il modello 3D della maniglia che si abbassa

    [Header("Impostazioni Interazione")]
    [SerializeField] private string handTag = "Hand";
    [SerializeField] private float handleDownRotation = 30f; // Gradi di abbassamento maniglia
    [SerializeField] private float openForceMultiplier = 5f;

    private bool isGrabbed = false;
    private Transform activeHand;
    private Quaternion initialHandleRotation;
    private XRSimpleInteractable simpleInteractable;

    private void Awake()
    {
        if (handleVisual != null)
        {
            initialHandleRotation = handleVisual.localRotation;
        }

        // Recupera o aggiunge il componente di interazione XR
        simpleInteractable = GetComponent<XRSimpleInteractable>();
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.AddListener(OnXRGrab);
            simpleInteractable.selectExited.AddListener(OnXRRelease);
        }

        // Mantiene la porta bloccata finché non si abbassa la maniglia
        LockDoor(true);
    }

    private void OnDestroy()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.RemoveListener(OnXRGrab);
            simpleInteractable.selectExited.RemoveListener(OnXRRelease);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsHandCollider(other) && !isGrabbed)
        {
            StartInteracting(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsHandCollider(other) && isGrabbed && activeHand == other.transform)
        {
            StopInteracting();
        }
    }

    private void OnXRGrab(SelectEnterEventArgs args)
    {
        StartInteracting(args.interactorObject.transform);
    }

    private void OnXRRelease(SelectExitEventArgs args)
    {
        StopInteracting();
    }

    private void StartInteracting(Transform handTransform)
    {
        isGrabbed = true;
        activeHand = handTransform;
        LockDoor(false);

        // SPEGNE IL TUTORIAL PER SEMPRE QUANDO IL GIOCATORE APRE LA PORTA
        DoorTutorialPanel tutorial = GetComponentInParent<DoorTutorialPanel>();
        if (tutorial != null)
        {
            tutorial.OnDoorOpened();
        }
    }

    private void StopInteracting()
    {
        isGrabbed = false;
        activeHand = null;

        // Ripristina l'inclinazione visiva della maniglia
        if (handleVisual != null)
        {
            handleVisual.localRotation = initialHandleRotation;
        }

        // Opzionale: blocca nuovamente la porta se si chiude completamente
        if (Mathf.Abs(doorHinge.angle) < 2f)
        {
            LockDoor(true);
        }
    }

    private void Update()
    {
        if (isGrabbed && activeHand != null)
        {
            // Abbassa visivamente la maniglia
            if (handleVisual != null)
            {
                handleVisual.localRotation = initialHandleRotation * Quaternion.Euler(handleDownRotation, 0, 0);
            }

            // Calcola la spinta sulla porta in base al movimento della mano
            ApplyHandForceToDoor();
        }
    }

    private void ApplyHandForceToDoor()
    {
        if (doorRigidbody == null) return;

        // Calcola la direzione tra l'asse della porta e la posizione attuale della mano
        Vector3 doorToHand = activeHand.position - doorHinge.transform.position;
        Vector3 pushDirection = Vector3.Cross(doorHinge.axis, doorToHand).normalized;

        // Applica una forza fisica al Rigidbody dell'anta per farla ruotare realisticamente
        doorRigidbody.AddForceAtPosition(pushDirection * openForceMultiplier, transform.position, ForceMode.Acceleration);
    }

    private void LockDoor(bool isLocked)
    {
        if (doorRigidbody != null)
        {
            doorRigidbody.isKinematic = isLocked;
        }
    }

    private bool IsHandCollider(Collider other)
    {
        return other.CompareTag(handTag) ||
               other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>() != null;
    }
}