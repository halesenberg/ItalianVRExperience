using UnityEngine;
using TMPro;
using Bellavalle.Core;

namespace Bellavalle.UI
{
    public class PhraseRowUI : MonoBehaviour
    {
        [SerializeField] TMP_Text textIT;
        [SerializeField] TMP_Text textEN;
        [SerializeField] GameObject playAudioButton;
        [SerializeField] AudioSource audioSource;

        LearnedPhrase _phrase;

        public void Setup(LearnedPhrase phrase)
        {
            _phrase = phrase;
            if (phrase == null) return;

            if (textIT != null) textIT.text = phrase.textIT;
            if (textEN != null) textEN.text = string.IsNullOrEmpty(phrase.textEN)
                ? "" : $"({phrase.textEN})";

            bool hasAudio = phrase.audioClip != null;
            if (playAudioButton != null)
                playAudioButton.SetActive(hasAudio);
        }

        public void PlayAudio()
        {
            if (_phrase == null || _phrase.audioClip == null) return;
            if (audioSource == null) return;

            audioSource.PlayOneShot(_phrase.audioClip);
        }
    }
}