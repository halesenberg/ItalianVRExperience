using UnityEngine;

public sealed class HandFollower : MonoBehaviour
{
    public Transform handToFollow; // Trascina qui il polso della mano grigia

    void LateUpdate()
    {
        if (handToFollow != null)
        {
            transform.position = handToFollow.position;
            transform.rotation = handToFollow.rotation;
        }
    }
}