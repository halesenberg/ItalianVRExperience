using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RoomLabelSetup : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;
    [SerializeField] private GameObject visualContainer;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button replayButton;


    [TextArea] public string italianText = "C'è un divano.";
    [TextArea] public string englishText = "There is a sofa.";

    private void Awake()
    {
        labelText.text = $"<size=120%><b>{italianText}</b></size>\n" +
                         $"<size=85%><color=#888888><i>{englishText}</i></color></size>";

        if (clip != null) audioSource.clip = clip;

        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (replayButton != null) replayButton.onClick.AddListener(PlayAudio);

        Hide();
    }

    public void Show()
    {
        visualContainer.SetActive(true);
        PlayAudio();
    }

    public void Hide()
    {
        visualContainer.SetActive(false);
        audioSource.Stop();
    }

    private void PlayAudio()
    {
        if (audioSource.clip != null)
        {
            audioSource.Stop();
            audioSource.Play();
        }
    }
}