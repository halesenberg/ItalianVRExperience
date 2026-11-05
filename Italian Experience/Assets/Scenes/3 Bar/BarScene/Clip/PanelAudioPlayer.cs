using UnityEngine;

public class PanelAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip audioClip;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 0 = audio 2D, sempre chiaro indipendentemente dalla posizione del player in VR

        if (audioClip == null)
        {
            Debug.LogWarning($"[AUDIO DEBUG] {gameObject.name}: nessuna AudioClip assegnata su PanelAudioPlayer!");
        }
    }

    public void PlayClip()
    {
        if (audioClip == null)
        {
            Debug.LogWarning($"[AUDIO DEBUG] {gameObject.name}: PlayClip chiamato ma audioClip è null.");
            return;
        }

        audioSource.Stop();
        audioSource.clip = audioClip;
        audioSource.Play();
        Debug.Log($"[AUDIO DEBUG] Riproduzione clip su {gameObject.name}: {audioClip.name}");
    }
}
