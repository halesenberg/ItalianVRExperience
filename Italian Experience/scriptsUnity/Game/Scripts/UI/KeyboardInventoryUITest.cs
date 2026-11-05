using UnityEngine;
using UnityEngine.InputSystem;
using Bellavalle.UI;

namespace Bellavalle.UI
{
    /// <summary>
    /// SOLO PER TEST — apre/chiude l'inventario con il tasto I.
    /// Da rimuovere (o disattivare) una volta collegato il vero tasto menu XR.
    ///
    /// Setup:
    ///  1. Metti questo script sullo stesso GameObject di InventoryUI
    ///     (es. InventoryCanvas)
    ///  2. Assegna inventoryUI nell'Inspector
    ///  3. Premi Play, premi I per apri/chiudi
    /// </summary>
    public class KeyboardInventoryTest : MonoBehaviour
    {
        [SerializeField] InventoryUI inventoryUI;

        void Update()
        {
            if (inventoryUI == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.iKey.wasPressedThisFrame)
                inventoryUI.ToggleInventory();
        }
    }
}