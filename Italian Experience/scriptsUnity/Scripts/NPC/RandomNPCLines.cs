using UnityEngine;
using TMPro;

public class RandomLineNPC : MonoBehaviour
{
    [SerializeField] string[] lines;
    [SerializeField] AudioClip[] audioClips; // stesso ordine delle lines
    [SerializeField] float displayDuration = 3f;
    [SerializeField] TextMeshPro worldText;
    [SerializeField] AudioSource audioSource;
    [SerializeField] Animator animator;       // ← NUOVO: l'Animator dell'NPC seduta

    static readonly int H_Talk = Animator.StringToHash("IsTalking");

    int _lastIndex = -1;
    bool _hasAnimator;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        _hasAnimator = animator != null && animator.runtimeAnimatorController != null;
    }

    public void SayRandomLine()
    {
        int index;
        do { index = Random.Range(0, lines.Length); }
        while (lines.Length > 1 && index == _lastIndex);

        _lastIndex = index;
        worldText.text = lines[index];
        worldText.gameObject.SetActive(true);

        // durata: se c'è l'audio uso la sua lunghezza, altrimenti displayDuration
        float dur = displayDuration;
        if (audioSource != null && audioClips != null && index < audioClips.Length && audioClips[index] != null)
        {
            audioSource.PlayOneShot(audioClips[index]);
            dur = audioClips[index].length;
        }

        // passa a Talk
        if (_hasAnimator) animator.SetBool(H_Talk, true);

        CancelInvoke();
        Invoke(nameof(Hide), dur);
    }

    void Hide()
    {
        worldText.gameObject.SetActive(false);
        if (_hasAnimator) animator.SetBool(H_Talk, false);  // torna a Sitting Idle
    }
}