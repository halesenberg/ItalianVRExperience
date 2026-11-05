using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PronunciationEntry : MonoBehaviour
{
    public TextMeshProUGUI italianText;
    public Button playButton;
    public AudioClip audioClip;
    public AudioSource audioSource;

    void Start()
    {
        playButton.onClick.AddListener(() =>
        {
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.Play();
        });
    }

    public void Setup(string text, AudioClip clip, AudioSource source)
    {
        italianText.text = text;
        audioClip = clip;
        audioSource = source;
    }
}