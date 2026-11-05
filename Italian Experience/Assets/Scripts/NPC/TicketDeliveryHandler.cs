using UnityEngine;
using Bellavalle.Core;
using Bellavalle.Scene;

namespace Bellavalle.Missions
{
    /// <summary>
    /// Ascolta la consegna del biglietto al controllore e fa avanzare
    /// manualmente il dialogo al nodo di ringraziamento.
    ///
    /// Setup:
    ///  1. Metti questo script su un GameObject in scena (es. sul controllore
    ///     o su un GameObject "TicketDelivery")
    ///  2. Assegna giveToNPCZone (la zona di consegna sul controllore)
    ///  3. Assegna dialogueManager (quello in scena)
    ///  4. thankYouNodeId deve essere l'id del nodo di ringraziamento (es. "node_02")
    /// </summary>
    public class TicketDeliveryHandler : MonoBehaviour
    {
        [Header("Consegna")]
        [SerializeField] GiveToNPCZone giveToNPCZone;
        [SerializeField] string expectedItemId = "biglietto_treno";

        [Header("Dialogo")]
        [SerializeField] DialogueManager dialogueManager;
        [SerializeField] string thankYouNodeId = "node_02";

        bool _delivered;

        void OnEnable()
        {
            if (giveToNPCZone != null)
                giveToNPCZone.OnItemDelivered += OnItemDelivered;
        }

        void OnDisable()
        {
            if (giveToNPCZone != null)
                giveToNPCZone.OnItemDelivered -= OnItemDelivered;
        }

        void OnItemDelivered(string itemId)
        {
            if (_delivered) return;
            if (itemId != expectedItemId) return;

            _delivered = true;

            // Registra una parola/frase imparata (opzionale, per lo zaino)
            GameManager.Instance.LearnPhrase(
                "Ecco il biglietto.",
                "Here's the ticket.",
                PhraseCategory.Risposta,
                sourceNpcId: "controllore");

            // Passa al nodo di ringraziamento
            dialogueManager.GoToNodePublic(thankYouNodeId);
        }
    }
}