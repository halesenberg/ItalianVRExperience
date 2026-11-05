using System.Collections;
using UnityEngine;
using TMPro;
using Bellavalle;

public class LyricsSyncController : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private TextMeshProUGUI currentLineText;
    [SerializeField] private TextMeshProUGUI pronounNoteText;

    // Colori
    private static readonly string normalColor = "#FFFFFF";
    private static readonly string pronounColor = "#00AAFF";

    // Struttura dati per ogni riga
    [System.Serializable]
    public class LyricLine
    {
        public float timestamp;       // secondi dall'inizio della canzone
        [TextArea] public string lineIT;   // testo italiano (usa <color> per i pronomi)
        public string noteEN;         // nota in inglese sul pronome (vuota se nessuno)
    }

    [SerializeField] private LyricLine[] lines;

    private int currentIndex = -1;
    private bool isRunning = false;

    private void OnEnable()
    {
        currentIndex = -1;
        isRunning = true;
        currentLineText.text = "";
        pronounNoteText.text = "";
        StartCoroutine(SyncLyrics());
    }

    private void OnDisable()
    {
        isRunning = false;
        StopAllCoroutines();
    }

    private IEnumerator SyncLyrics()
    {
        while (isRunning)
        {
            if (musicSource == null || !musicSource.isPlaying)
            {
                yield return null;
                continue;
            }

            float time = musicSource.time;

            // Trova la riga corretta per il tempo attuale
            int newIndex = -1;
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (time >= lines[i].timestamp)
                {
                    newIndex = i;
                    break;
                }
            }

            if (newIndex != currentIndex)
            {
                currentIndex = newIndex;
                if (currentIndex >= 0)
                {
                    currentLineText.text = lines[currentIndex].lineIT;
                    pronounNoteText.text = lines[currentIndex].noteEN;
                    TrackPronoun(lines[currentIndex].noteEN);
                }
                else
                {
                    currentLineText.text = "";
                    pronounNoteText.text = "";
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Estrae il pronome dalla nota (formato "Lei - she") e lo registra come
    /// parola appresa, così l'AI Helper sa che il player l'ha già sentito nella canzone.
    /// </summary>
    private void TrackPronoun(string noteEN)
    {
        if (string.IsNullOrWhiteSpace(noteEN)) return;
        if (GameManager.Instance == null) return;

        int dashIndex = noteEN.IndexOf(" - ");
        if (dashIndex <= 0) return;

        string pronoun = noteEN.Substring(0, dashIndex).Trim();
        GameManager.Instance.LearnWord(pronoun);
    }
}