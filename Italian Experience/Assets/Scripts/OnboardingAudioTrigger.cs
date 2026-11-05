using UnityEngine;

/// <summary>
/// Riproduce l'audio del pannello quando il player entra nella zona di fronte ad esso.
///
/// Riutilizzabile su ogni pannello onboarding senza modifiche: basta un BoxCollider
/// (con "Is Trigger" attivo) posizionato davanti al pannello, un AudioSource sullo
/// stesso GameObject con il clip del pannello assegnato (Play On Awake = false) e
/// questo script. Per un nuovo pannello si cambia solo il clip sull'AudioSource,
/// nient'altro.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class OnboardingAudioTrigger : MonoBehaviour
{
    AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_audioSource.isPlaying) return; // non riparte se il player oscilla dentro/fuori dal trigger

        _audioSource.Play();
    }
}
