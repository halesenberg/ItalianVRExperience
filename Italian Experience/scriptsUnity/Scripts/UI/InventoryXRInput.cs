using UnityEngine;
using UnityEngine.InputSystem;
using Bellavalle.UI;

namespace Bellavalle.UI
{
    public class InventoryXRInput : MonoBehaviour
    {
        [SerializeField] InventoryUI inventoryUI;
        [SerializeField] InputActionReference toggleButton;

        void OnEnable()
        {
            if (toggleButton != null) toggleButton.action.performed += OnToggle;
        }

        void OnDisable()
        {
            if (toggleButton != null) toggleButton.action.performed -= OnToggle;
        }

        void OnToggle(InputAction.CallbackContext ctx)
        {
            Debug.Log("[InventoryXRInput] A premuto");
            inventoryUI?.ToggleInventory();
        }

        void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
            {
                Debug.Log("[TEST] I premuto");
                inventoryUI?.ToggleInventory();
            }
        }
    }
}