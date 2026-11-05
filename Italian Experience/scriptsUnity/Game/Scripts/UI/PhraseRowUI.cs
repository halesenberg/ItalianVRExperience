using UnityEngine;
using TMPro;
using Bellavalle.Data;

namespace Bellavalle.UI
{
    public class PhraseRowUI : MonoBehaviour
    {
        [SerializeField] TMP_Text textIT;
        [SerializeField] TMP_Text textEN;
        [SerializeField] GameObject playAudioButton;
        [SerializeField] AudioSource audioSource;

        PhraseEntry _entry;

        public void Setup(PhraseEntry entry)
        {
            _entry = entry;
            if (entry == null) return;

            if (textIT != null) textIT.text = entry.textIT;
            if (textEN != null) textEN.text = string.IsNullOrEmpty(entry.textEN)
                ? "" : $"({entry.textEN})";

            bool hasAudio = entry.voiceClip != null;
            if (playAudioButton != null)
                playAudioButton.SetActive(hasAudio);
        }

        public void PlayAudio()
        {
            if (_entry?.voiceClip == null || audioSource == null) return;
            audioSource.PlayOneShot(_entry.voiceClip);
        }
    }
}