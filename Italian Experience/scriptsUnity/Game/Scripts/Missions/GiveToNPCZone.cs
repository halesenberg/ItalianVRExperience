using UnityEngine;

namespace Bellavalle.Missions
{
    /// Zona di consegna su un NPC. Quando il player rilascia un
    /// DeliverableItem dentro questo trigger, notifica l'evento OnItemDelivered.
    ///
    /// Riutilizzabile per qualsiasi missione "porta X a NPC":
    ///  - Banconota al chitarrista
    ///  - Alimenti a Carla (lista della spesa)
    ///  - Qualsiasi futura missione "fetch and deliver"
    ///
    /// Setup:
    ///  1. Metti su un GameObject vicino/sull'NPC
    ///  2. Aggiungi un SphereCollider (o BoxCollider), Is Trigger = true
    ///  3. Assegna acceptedItemIds (lascia vuoto per accettare qualsiasi item)
    ///  4. Collega OnItemDelivered al codice della missione specifica
    ///     (es. MissionManager, o un listener dedicato)
    /// </summary>
    public class GiveToNPCZone : MonoBehaviour
    {
        [Header("Filtro item accettati")]
        [Tooltip("Se vuoto, accetta qualsiasi DeliverableItem. " +
                 "Se compilato, accetta solo gli itemId elencati.")]
        [SerializeField] string[] acceptedItemIds;

        [Header("Comportamento")]
        [Tooltip("Se true, l'oggetto consegnato viene distrutto " +
                 "(es. la banconota \"sparisce\" nelle mani dell'NPC). " +
                 "Se false, resta nella scena (es. lo gestisce un'altra missione).")]
        [SerializeField] bool destroyOnDelivery = true;

        /// <summary>
        /// Invocato quando un item valido viene consegnato.
        /// Passa l'itemId consegnato.
        /// </summary>
        public event System.Action<string> OnItemDelivered;

        void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponent<DeliverableItem>();
            if (item == null) return;
            if (item.IsGrabbed) return;          // ancora in mano, non e' stato rilasciato
            if (!IsAccepted(item.itemId)) return;

            HandleDelivery(item);
        }

        void OnTriggerStay(Collider other)
        {
            // Gestisce il caso in cui l'item viene rilasciato GIA' dentro il trigger
            var item = other.GetComponent<DeliverableItem>();
            if (item == null) return;
            if (item.IsGrabbed) return;
            if (!IsAccepted(item.itemId)) return;

            HandleDelivery(item);
        }

        bool IsAccepted(string itemId)
        {
            if (acceptedItemIds == null || acceptedItemIds.Length == 0) return true;
            foreach (var id in acceptedItemIds)
                if (id == itemId) return true;
            return false;
        }

        void HandleDelivery(DeliverableItem item)
        {
            string id = item.itemId;
            OnItemDelivered?.Invoke(id);

            if (destroyOnDelivery)
                Destroy(item.gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            var sphere = GetComponent<SphereCollider>();
            if (sphere != null)
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
        }
    }
}
