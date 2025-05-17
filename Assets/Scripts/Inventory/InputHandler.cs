using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Manages input interactions for inventory item manipulation.
/// Handles rotating, moving, and other inventory-specific inputs.
/// </summary>
public class InputHandler : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private UIInventoryManager uiManager;

    [Header("Input Configuration")]
    [SerializeField] private float doubleClickTime = 0.3f;
    [SerializeField] private KeyCode rotateItemKey = KeyCode.R;
    [SerializeField] private KeyCode splitStackKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode quickMoveKey = KeyCode.LeftControl;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Input state tracking
    private float lastClickTime = 0f;
    private InventorySlot lastClickedSlot = null;
    private InventoryItem selectedItem = null;
    private bool isRotatingItem = false;

    // Cached PlayerControls reference (from new Input System)
    private PlayerControls playerControls;
    private InputAction rotateAction;
    private InputAction quickMoveAction;
    private InputAction splitStackAction;

    #region Unity Lifecycle

    private void Awake()
    {
        // Create new input actions if using the new Input System
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        // Register input events - New Input System approach
        if (playerControls != null)
        {
            // Setup input actions
            rotateAction = playerControls.Inventory.Rotate;
            rotateAction.Enable();
            rotateAction.performed += OnRotateItem;

            quickMoveAction = playerControls.Inventory.QuickMove;
            quickMoveAction.Enable();

            splitStackAction = playerControls.Inventory.SplitStack;
            splitStackAction.Enable();

            // Additional inventory input mappings can be added here
        }
    }

    private void OnDisable()
    {
        // Unregister input events
        if (playerControls != null)
        {
            rotateAction.performed -= OnRotateItem;
            rotateAction.Disable();
            quickMoveAction.Disable();
            splitStackAction.Disable();
        }
    }

    private void Update()
    {
        // Only process inventory inputs when inventory is open
        if (!inventorySystem.IsInventoryOpen()) return;

        // Process hovering for tooltips
        ProcessHovering();
    }

    #endregion

    #region Input Handlers

    // Handler for item rotation input
    private void OnRotateItem(InputAction.CallbackContext context)
    {
        if (selectedItem != null && selectedItem.IsRotatable)
        {
            RotateSelectedItem();
        }
    }
    // Process hovering for tooltips
    private void ProcessHovering()
    {
        // Only process hovering if we're not dragging an item
        if (uiManager != null && !IsInDragOperation())
        {
            // Check if we're hovering over an inventory slot
            InventorySlot hoveredSlot = GetHoveredSlot();
            if (hoveredSlot != null && hoveredSlot.GetItem() != null)
            {
                // Position tooltip relative to the cursor
                Vector3 tooltipPosition = Input.mousePosition + new Vector3(10f, 10f, 0f);
                uiManager.ShowTooltip(hoveredSlot.GetItem(), tooltipPosition);
            }
            else
            {
                uiManager.HideTooltip();
            }
        }
    }

    // Get the slot that the cursor is currently hovering over
    private InventorySlot GetHoveredSlot()
    {
        if (EventSystem.current == null) return null;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            InventorySlot slot = result.gameObject.GetComponent<InventorySlot>();
            if (slot != null)
            {
                return slot;
            }
        }

        return null;
    }

    #endregion

    #region Item Operations

    // Rotate the currently selected item
    private void RotateSelectedItem()
    {
        if (selectedItem == null) return;

        // Update the item rotation state
        selectedItem.Rotate();

        // Check if the item can be placed in its new rotation
        if (inventorySystem.CanPlaceItem(selectedItem, selectedItem.GridPosition))
        {
            // Update the visual representation and grid state
            uiManager.UpdateGridUI();

            if (debugMode)
            {
                Debug.Log($"Rotated item: {selectedItem.ItemName}");
            }
        }
        else
        {
            // Revert rotation if can't place
            selectedItem.Rotate(); // Rotate back

            if (debugMode)
            {
                Debug.LogWarning($"Can't rotate item: {selectedItem.ItemName} at position {selectedItem.GridPosition}");
            }
        }
    }

    // Handle double click on an item
    public void HandleDoubleClick(InventorySlot slot)
    {
        if (slot == null || slot.GetItem() == null) return;

        float timeSinceLastClick = Time.time - lastClickTime;

        if (slot == lastClickedSlot && timeSinceLastClick < doubleClickTime)
        {
            // It's a double click!
            InventoryItem item = slot.GetItem();

            // Determine appropriate action based on item type
            switch (item.Type)
            {
                case ItemType.Weapon:
                case ItemType.Armor:
                    // Try to equip the item
                    TryEquipItem(item);
                    break;

                case ItemType.Food:
                case ItemType.Medical:
                    // Try to use the item
                    TryUseItem(item);
                    break;

                default:
                    // For other items, maybe just select/deselect
                    ToggleItemSelection(slot);
                    break;
            }
        }

        // Update last click tracking
        lastClickTime = Time.time;
        lastClickedSlot = slot;
    }

    // Try to equip an item
    private void TryEquipItem(InventoryItem item)
    {
        string appropriateSlot = DetermineAppropriateEquipmentSlot(item);

        if (!string.IsNullOrEmpty(appropriateSlot))
        {
            inventorySystem.EquipItem(item, appropriateSlot);

            if (debugMode)
            {
                Debug.Log($"Equipped {item.ItemName} in {appropriateSlot}");
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.LogWarning($"No appropriate equipment slot found for {item.ItemName}");
            }
        }
    }

    // Determine appropriate equipment slot based on item type
    private string DetermineAppropriateEquipmentSlot(InventoryItem item)
    {
        // This is a simplified example - in a real system,
        // you might want more sophisticated logic or data-driven approach
        switch (item.Type)
        {
            case ItemType.Weapon:
                return "WeaponSlot";

            case ItemType.Armor:
                // You could further differentiate by subtype or properties
                return "ArmorSlot";

            default:
                return string.Empty;
        }
    }

    // Try to use an item
    private void TryUseItem(InventoryItem item)
    {
        // Delegate to the item's own use behavior
        item.UseItem();

        // Additional inventory system logic
        // e.g., remove item if consumed

        if (debugMode)
        {
            Debug.Log($"Used item: {item.ItemName}");
        }
    }

    // Toggle item selection
    private void ToggleItemSelection(InventorySlot slot)
    {
        if (selectedItem == slot.GetItem())
        {
            // Deselect if already selected
            selectedItem = null;
            uiManager.SelectSlot(null);

            if (debugMode)
            {
                Debug.Log("Item deselected");
            }
        }
        else
        {
            // Select new item
            selectedItem = slot.GetItem();
            uiManager.SelectSlot(slot);

            if (debugMode)
            {
                Debug.Log($"Selected item: {selectedItem.ItemName}");
            }
        }
    }

    // Cancel current operation
    private void CancelCurrentOperation()
    {
        // Cancel any active drag operation
        if (IsInDragOperation())
        {
            // Let the UI manager know to cancel drag
            // This would typically be handled by EndDragItem in UIInventoryManager
            // but we can trigger it manually
            uiManager.HideContextMenu();
            uiManager.HideTooltip();
        }

        // Clear selected item
        selectedItem = null;
    }

    // Check if we're in the middle of a drag operation
    private bool IsInDragOperation()
    {
        // This would need proper integration with UIInventoryManager
        // This is a simplified check - real implementation would depend on your UIInventoryManager state
        return false;
    }

    #endregion

    #region Editor Utilities

    // Helper method for editor integration - can be called from editor scripts
    public void SetupDefaultReferences()
    {
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIInventoryManager>();
        }
    }

    #endregion
}