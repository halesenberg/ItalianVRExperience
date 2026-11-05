using UnityEngine;


namespace Bellavalle.Missions
{
    /// <summary>
    /// Marca un oggetto come "consegnabile" a un NPC.
    /// Metti questo script su qualsiasi oggetto che il player deve
    /// raccogliere e portare a un NPC (banconota, alimenti, ecc.)
    ///
    /// Lavora insieme a GiveToNPCZone, posizionato sull'NPC.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class DeliverableItem : MonoBehaviour
    {
        [Tooltip("Id univoco dell'oggetto, usato da GiveToNPCZone per " +
                 "riconoscere quale oggetto è stato consegnato. " +
                 "Es: \"banconota_10\", \"pomodori\", \"pane\"")]
        public string itemId;

        public bool IsGrabbed { get; private set; }

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

        void Awake()
        {
            _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            _grab.selectEntered.AddListener(_ => IsGrabbed = true);
            _grab.selectExited.AddListener(_ => IsGrabbed = false);
        }

        void OnDestroy()
        {
            if (_grab == null) return;
            _grab.selectEntered.RemoveAllListeners();
            _grab.selectExited.RemoveAllListeners();
        }
    }
}
