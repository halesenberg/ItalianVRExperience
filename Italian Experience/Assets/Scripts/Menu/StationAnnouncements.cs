using System.Collections;
using UnityEngine;

public class StationAnnouncements : MonoBehaviour
{
    public AudioClip[] announcementClips; // trascina qui le 3 clip nel Inspector
    public float intervalSeconds = 60f;   // intervallo tra un annuncio e l'altro

    private AudioSource audioSource;
    private int currentIndex = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(PlayAnnouncements());
    }

    IEnumerator PlayAnnouncements()
    {
        while (true)
        {
            // Aspetta l'intervallo prima di partire
            yield return new WaitForSeconds(intervalSeconds);

            // Riproduci la clip corrente
            audioSource.clip = announcementClips[currentIndex];
            audioSource.Play();

            // Passa alla prossima (ciclicamente: 0 → 1 → 2 → 0 → ...)
            currentIndex = (currentIndex + 1) % announcementClips.Length;
        }
    }
}