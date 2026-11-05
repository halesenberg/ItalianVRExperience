using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class LabelInteractable : MonoBehaviour
{
    [Header("Impostazioni XR")]
    [SerializeField] private string handTag = "Hand";

    // FIX: prima non era [SerializeField] — il collegamento all'etichetta si
    // perdeva ad ogni domain reload (ricompilazione script) o riapertura del
    // progetto, perché un campo privato non serializzato non viene salvato.
    [SerializeField] private ObjectLabeler associatedLabel;
    private XRGrabInteractable grabInteractable;

    // NUOVO: permette a EditorLabelGenerator di controllare se questo oggetto
    // ha già un'etichetta collegata, per aggiornarla invece di duplicarla.
    public ObjectLabeler AssociatedLabel => associatedLabel;

    public void RegisterLabel(ObjectLabeler label)
    {
        associatedLabel = label;
    }

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsHand(other))
        {
            ShowLabel();
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        ShowLabel();
    }

    private void ShowLabel()
    {
        if (associatedLabel != null)
        {
            associatedLabel.ShowLabelAndPlayAudio();
        }
    }

    private bool IsHand(Collider other)
    {
        return other.CompareTag(handTag) ||
               other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>() != null;
    }
}