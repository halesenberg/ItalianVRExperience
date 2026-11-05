using UnityEngine;

public class TurntableController : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip acquaAzzurra;
    [SerializeField] private GameObject lyricsPanel;
    [SerializeField] private GameObject discObject;

    private bool isPlaying = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != discObject) return;
        if (isPlaying) return;

        isPlaying = true;
        musicSource.clip = acquaAzzurra;
        musicSource.Play();
        lyricsPanel.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject != discObject) return;

        isPlaying = false;
        musicSource.Stop();
        lyricsPanel.SetActive(false);
    }
}