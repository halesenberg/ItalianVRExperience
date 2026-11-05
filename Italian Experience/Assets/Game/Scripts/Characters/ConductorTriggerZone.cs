using UnityEngine;

namespace Bellavalle.Characters
{
    [RequireComponent(typeof(Collider))]
    public class ConductorTriggerZone : MonoBehaviour
    {
        [SerializeField] ConductorSequence conductor;
        bool _fired;

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Zone] Entrato: {other.name}, tag={other.tag}");
            if (_fired || !other.CompareTag("Player")) return;
            _fired = true;
            Debug.Log("[Zone] BeginSequence chiamato!");
            conductor?.BeginSequence();
        }
    }
}