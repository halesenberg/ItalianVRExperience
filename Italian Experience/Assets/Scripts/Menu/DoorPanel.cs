using UnityEngine;
using TMPro;

public class DoorTutorialPanel : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [SerializeField] private GameObject tutorialCanvas; // Il Canvas con "Apri la porta ➔"
    [SerializeField] private TMP_Text instructionText;

    [Header("Impostazioni")]
    [SerializeField] private string handTag = "Hand";
    [SerializeField] private string textToShow = "Apri la porta ➔";

    private bool hasBeenOpened = false;

    private void Start()
    {
        if (instructionText != null)
        {
            instructionText.text = textToShow;
        }

        // Il tutorial parte nascosto
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Se la porta non è mai stata aperta e la mano si avvicina, mostra il tutorial
        if (!hasBeenOpened && IsHand(other))
        {
            if (tutorialCanvas != null)
            {
                tutorialCanvas.SetActive(true);
            }
        }
    }

    // Chiamare questo metodo quando la porta viene effettivamente aperta/manovrata
    public void OnDoorOpened()
    {
        hasBeenOpened = true;

        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false); // Disattiva il pannello per sempre
        }
    }

    private bool IsHand(Collider other)
    {
        return other.CompareTag(handTag) ||
               other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>() != null;
    }
}