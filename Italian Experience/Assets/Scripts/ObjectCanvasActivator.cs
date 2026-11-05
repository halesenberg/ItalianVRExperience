using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ObjectCanvasActivator : MonoBehaviour
{
    [SerializeField] private GameObject canvas;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        Debug.Log($"[CANVAS DEBUG] Awake su {gameObject.name}");

        interactable = GetComponent<XRBaseInteractable>();

        if (interactable == null)
        {
            Debug.LogError($"[CANVAS DEBUG] {gameObject.name}: XR Interactable NON trovato!");
            return;
        }

        Debug.Log($"[CANVAS DEBUG] Interactable trovato: {interactable.GetType().Name}");

        if (canvas == null)
        {
            Debug.LogError($"[CANVAS DEBUG] {gameObject.name}: Canvas NON assegnato!");
            return;
        }

        Debug.Log($"[CANVAS DEBUG] Canvas assegnato: {canvas.name}");

        canvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEntered);
            Debug.Log($"[CANVAS DEBUG] Listener hover aggiunto a {gameObject.name}");
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"[CANVAS DEBUG] HOVER RILEVATO su {gameObject.name}");

        if (canvas != null)
        {
            canvas.SetActive(true);
            Debug.Log($"[CANVAS DEBUG] Canvas ATTIVATO: {canvas.name}");
        }
    }

    public void CloseCanvas()
    {
        Debug.Log($"[CANVAS DEBUG] CloseCanvas chiamato su {gameObject.name}");

        if (canvas != null)
            canvas.SetActive(false);
    }
}