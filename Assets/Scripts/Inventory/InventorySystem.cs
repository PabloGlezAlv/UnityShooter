using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventorySystem : MonoBehaviour
{
    // Singleton para acceso global
    public static InventorySystem Instance { get; private set; }

    // Referencias a otros componentes del sistema
    [SerializeField] private UIInventoryManager uiManager;
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private ItemDatabase itemDatabase;

    // Configuración del inventario
    [Header("Inventory Configuration")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private GameObject inventoryPanel;

    // Input System
    private PlayerControls playerControls;
    private InputAction toggleInventoryAction;

    // Lista de items en el inventario
    private List<InventoryItem> inventoryItems = new List<InventoryItem>();

    // Estado del inventario
    private bool isInventoryOpen = false;

    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializar controles
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        // Configurar acciones de input
        toggleInventoryAction = playerControls.Player.Inventory;
        toggleInventoryAction.Enable();
        toggleInventoryAction.performed += ToggleInventory;
    }

    private void OnDisable()
    {
        toggleInventoryAction.performed -= ToggleInventory;
        toggleInventoryAction.Disable();
    }

    private void Start()
    {
        // Ocultar inventario al inicio
        inventoryPanel.SetActive(false);
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        // TODO: Inicializar el inventario, cargar items guardados, etc.
    }

    private void ToggleInventory(InputAction.CallbackContext context)
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);

        // Activar/desactivar cursor y controles según corresponda
        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // TODO: Desactivar controles de juego
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // TODO: Reactivar controles de juego
        }
    }

    // Métodos públicos para gestionar el inventario
    public bool AddItem(InventoryItem item)
    {
        // TODO: Implementar lógica para añadir un item al inventario
        return false;
    }

    public bool RemoveItem(InventoryItem item)
    {
        // TODO: Implementar lógica para quitar un item del inventario
        return false;
    }

    public bool MoveItem(InventoryItem item, Vector2Int newPosition)
    {
        // TODO: Implementar lógica para mover un item dentro del inventario
        return false;
    }

    public bool CanPlaceItem(InventoryItem item, Vector2Int position)
    {
        // TODO: Implementar comprobación de si un item puede ser colocado en una posición específica
        return false;
    }

    // Métodos para interactuar con el equipamiento
    public void EquipItem(InventoryItem item, string slotName)
    {
        // TODO: Llamar al EquipmentManager para equipar el item
    }

    public void UnequipItem(string slotName)
    {
        // TODO: Llamar al EquipmentManager para desequipar el item
    }
}