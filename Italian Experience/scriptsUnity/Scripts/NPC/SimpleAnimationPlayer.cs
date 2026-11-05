using UnityEngine;

public class SimpleAnimationPlayer : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private string animationName = "Happy Idle";

    void Start()
    {
        // Recupera il componente Animator dall'oggetto
        animator = GetComponent<Animator>();

        if (animator != null)
        {
            // Fa partire l'animazione usando il nome dello stato
            animator.Play(animationName);
        }
        else
        {
            Debug.LogError("Manca l'Animator sul Man in Coat! Trascinalo nell'Inspector.");
        }
    }
}