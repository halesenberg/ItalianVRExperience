using Bellavalle.Characters;
using UnityEngine;

public class LinaTrigger : MonoBehaviour
{
    public LinaSequence linaSequence;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            linaSequence.BeginSequence();
            Destroy(gameObject); // si autodistrugge dopo il trigger
        }
    }
}