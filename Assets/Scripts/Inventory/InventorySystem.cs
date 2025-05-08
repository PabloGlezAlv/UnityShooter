using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class InventorySystem : MonoBehaviour
{
    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public string itemName;
        public Sprite itemIcon;
        public int quantity;
        public string description;
        public bool isUsable;
        public bool isEquippable;
        public bool isConsumable;
        public ItemType itemType;

        public InventoryItem(string id, string name, Sprite icon, string desc, ItemType type, int qty = 1)
        {
            itemId = id;
            itemName = name;
            itemIcon = icon;
            quantity = qty;
            description = desc;
            itemType = type;

            // Configurar propiedades basadas en el tipo
            switch (itemType)
            {
                case ItemType.Weapon:
                    isEquippable = true;
                    isUsable = false;
                    isConsumable = false;
                    break;
                case ItemType.Consumable:
                    isEquippable = false;
                    isUsable = true;
                    isConsumable = true;
                    break;
                case ItemType.Equipment:
                    isEquippable = true;
                    isUsable = false;
                    isConsumable = false;
                    break;
                case ItemType.Quest:
                    isEquippable = false;
                    isUsable = false;
                    isConsumable = false;
                    break;
                default:
                    isEquippable = false;
                    isUsable = true;
                    isConsumable = false;
                    break;
            }
        }
    }

    public enum ItemType
    {
        Weapon,
        Consumable,
        Equipment,
        Quest,
        Misc
    }

    [Header("Referencias")]
    [SerializeField] private PlayerControls playerControls;
    [SerializeField] private FPSController playerController;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Canvas mainCanvas; // Canvas principal existente

    [Header("Configuración UI")]
    [SerializeField] private GameObject inventoryUIPrefab; // Prefab de la UI del inventario
    [SerializeField] private GameObject itemSlotPrefab; // Prefab para los slots de items

    [Header("Objetos Iniciales")]
    [SerializeField] private ItemDatabaseManager itemDatabase; // Referencia a la base de datos de items
    [SerializeField] private List<StartingItemInfo> startingItems = new List<StartingItemInfo>(); // Items con los que inicia el jugador

    [System.Serializable]
    public class StartingItemInfo
    {
        public string itemId;
        public int quantity = 1;
    }

    // Referencias de la UI de inventario 
    private GameObject inventoryUI;
    private Transform itemContainer;
    private GameObject itemDetailPanel;
    private TextMeshProUGUI itemNameText;
    private TextMeshProUGUI itemDescriptionText;
    private TextMeshProUGUI itemStatsText;
    private Image itemIconImage;
    private Button useButton;
    private Button dropButton;

    // Variables de estado
    private List<InventoryItem> inventoryItems = new List<InventoryItem>();
    private bool isInventoryActive = false;
    private InventoryItem selectedItem = null;

    // Capacidad máxima del inventario
    [SerializeField] private int maxInventorySlots = 20;

    private void Awake()
    {
        if (playerControls == null)
            playerControls = new PlayerControls();

        if (playerController == null)
            playerController = GetComponentInParent<FPSController>();

        if (weaponManager == null)
            weaponManager = GetComponentInParent<WeaponManager>();

        // Verificar que los prefabs necesarios estén asignados
        if (inventoryUIPrefab == null)
        {
            Debug.LogError("Error: No se ha asignado el prefab de UI del inventario");
            return;
        }

        if (itemSlotPrefab == null)
        {
            Debug.LogError("Error: No se ha asignado el prefab de los slots de items");
            return;
        }

        if (mainCanvas == null)
        {
            Debug.LogError("Error: No se ha asignado el Canvas principal");
            return;
        }

        // Instanciar la UI del inventario
        InitializeInventoryUI();
    }

    private void Start()
    {
        // Añadir los objetos iniciales al inventario
        AddStartingItems();
    }

    private void AddStartingItems()
    {
        if (itemDatabase == null)
        {
            Debug.LogWarning("No se ha asignado una base de datos de items. No se añadirán objetos iniciales.");
            return;
        }

        foreach (var startingItem in startingItems)
        {
            var itemData = itemDatabase.GetItemById(startingItem.itemId);
            if (itemData != null)
            {
                AddItem(itemData.itemId, itemData.itemName, itemData.itemIcon,
                        itemData.description, itemData.itemType, startingItem.quantity);
                Debug.Log($"Añadido objeto inicial: {itemData.itemName} x{startingItem.quantity}");
            }
            else
            {
                Debug.LogWarning($"No se encontró el item con ID '{startingItem.itemId}' en la base de datos.");
            }
        }
    }

    private void OnEnable()
    {
        // Registrar la acción de abrir/cerrar inventario
        playerControls.Player.Inventory.performed += OnInventoryToggle;
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.Inventory.performed -= OnInventoryToggle;
        playerControls.Player.Disable();
    }

    private void OnInventoryToggle(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    private void ToggleInventory()
    {
        isInventoryActive = !isInventoryActive;
        Debug.Log("Activar inventario");
        // Activar/desactivar la UI del inventario
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isInventoryActive);
        }

        // Desactivar/activar el movimiento del jugador
        if (playerController != null)
        {
            // Desactivar el control del jugador cuando el inventario está abierto
            if (isInventoryActive)
            {
                playerControls.Player.Disable();
                // Mantener solo las funciones de inventario activas
                playerControls.Player.Inventory.Enable();

                // Desbloquear el cursor para usar la UI
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                playerControls.Player.Enable();

                // Bloquear el cursor de nuevo al cerrar el inventario
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // Si acabamos de abrir el inventario, actualizar la UI
        if (isInventoryActive)
        {
            RefreshInventoryUI();
        }
    }

    private void InitializeInventoryUI()
    {
        // Instanciar la UI del inventario como hijo del canvas principal
        inventoryUI = Instantiate(inventoryUIPrefab, mainCanvas.transform);
        inventoryUI.name = "InventoryUI";

        // Configurar el RectTransform para que se ajuste correctamente
        RectTransform rectTransform = inventoryUI.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.2f, 0.2f);
            rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        // Buscar todas las referencias necesarias en el prefab
        itemContainer = inventoryUI.transform.Find("ItemContainer");
        if (itemContainer == null)
        {
            Debug.LogError("Error: No se pudo encontrar 'ItemContainer' en el prefab del inventario");
            return;
        }

        // Panel de detalles del item
        itemDetailPanel = inventoryUI.transform.Find("ItemDetailPanel").gameObject;
        itemNameText = itemDetailPanel.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
        itemDescriptionText = itemDetailPanel.transform.Find("ItemDescription").GetComponent<TextMeshProUGUI>();
        itemStatsText = itemDetailPanel.transform.Find("ItemStats").GetComponent<TextMeshProUGUI>();
        itemIconImage = itemDetailPanel.transform.Find("ItemIcon").GetComponent<Image>();

        // Botones
        useButton = itemDetailPanel.transform.Find("UseButton").GetComponent<Button>();
        dropButton = itemDetailPanel.transform.Find("DropButton").GetComponent<Button>();

        // Agregar listeners a los botones
        useButton.onClick.AddListener(UseSelectedItem);
        dropButton.onClick.AddListener(DropSelectedItem);

        // Agregar listener al botón de cerrar
        Button closeButton = inventoryUI.transform.Find("CloseButton").GetComponent<Button>();
        closeButton.onClick.AddListener(ToggleInventory);

        // Desactivar la UI del inventario al inicio
        inventoryUI.SetActive(false);
    }

    public void AddItem(string itemId, string itemName, Sprite icon, string description, ItemType type, int quantity = 1)
    {
        // Verificar si ya tenemos este item
        InventoryItem existingItem = inventoryItems.Find(item => item.itemId == itemId);

        if (existingItem != null)
        {
            // Si ya tenemos el item, incrementar la cantidad
            existingItem.quantity += quantity;

            // Si el inventario está activo, actualizar la UI
            if (isInventoryActive)
            {
                RefreshInventoryUI();
            }

            // Mostrar notificación al obtener un item
            StartCoroutine(ShowItemNotification(itemName + " +" + quantity));
        }
        else
        {
            // Si es un nuevo item, agregarlo a la lista
            if (inventoryItems.Count < maxInventorySlots)
            {
                InventoryItem newItem = new InventoryItem(itemId, itemName, icon, description, type, quantity);
                inventoryItems.Add(newItem);

                // Si el inventario está activo, actualizar la UI
                if (isInventoryActive)
                {
                    RefreshInventoryUI();
                }

                // Mostrar notificación al obtener un nuevo item
                StartCoroutine(ShowItemNotification(itemName));
            }
            else
            {
                Debug.Log("Inventario lleno, no se puede agregar: " + itemName);
                // Aquí podrías implementar una notificación de inventario lleno
            }
        }
    }

    public void RemoveItem(string itemId, int quantity = 1)
    {
        InventoryItem item = inventoryItems.Find(i => i.itemId == itemId);

        if (item != null)
        {
            item.quantity -= quantity;

            // Si la cantidad es 0 o menos, eliminar el item
            if (item.quantity <= 0)
            {
                inventoryItems.Remove(item);
            }

            // Actualizar la UI si está activa
            if (isInventoryActive)
            {
                RefreshInventoryUI();
            }
        }
    }

    private void RefreshInventoryUI()
    {
        // Limpiar los slots existentes
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        // Crear nuevos slots para cada item
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemContainer);
            InventoryItem item = inventoryItems[i];

            // Configurar el slot
            Image slotIcon = slot.transform.Find("ItemIcon").GetComponent<Image>();
            TextMeshProUGUI slotQuantity = slot.transform.Find("ItemQuantity").GetComponent<TextMeshProUGUI>();

            slotIcon.sprite = item.itemIcon;
            slotQuantity.text = item.quantity > 1 ? item.quantity.ToString() : "";

            // Agregar listener para seleccionar este item
            int index = i; // Necesario para capturar la variable en la clausura
            Button slotButton = slot.GetComponent<Button>();
            slotButton.onClick.AddListener(() => SelectItem(index));
        }

        // Limpiar panel de detalles si no hay selección
        if (selectedItem == null)
        {
            ClearItemDetails();
        }
    }

    private void SelectItem(int index)
    {
        if (index >= 0 && index < inventoryItems.Count)
        {
            selectedItem = inventoryItems[index];

            // Actualizar la UI de detalles
            itemNameText.text = selectedItem.itemName;
            itemDescriptionText.text = selectedItem.description;
            itemIconImage.sprite = selectedItem.itemIcon;

            // Configurar estadísticas según el tipo
            switch (selectedItem.itemType)
            {
                case ItemType.Weapon:
                    itemStatsText.text = "Daño: 10\nVelocidad: 1.5\nPrecisión: 80%";
                    break;
                case ItemType.Consumable:
                    itemStatsText.text = "Restaura 25 de salud";
                    break;
                default:
                    itemStatsText.text = "";
                    break;
            }

            // Activar/desactivar botones según el tipo de item
            useButton.gameObject.SetActive(selectedItem.isUsable);
            dropButton.gameObject.SetActive(true);

            // Mostrar el panel de detalles
            itemDetailPanel.SetActive(true);
        }
    }

    private void ClearItemDetails()
    {
        itemNameText.text = "";
        itemDescriptionText.text = "";
        itemStatsText.text = "";
        itemIconImage.sprite = null;
        itemDetailPanel.SetActive(false);
    }

    private void UseSelectedItem()
    {
        if (selectedItem != null)
        {
            // Lógica para usar el item según su tipo
            switch (selectedItem.itemType)
            {
                case ItemType.Weapon:
                    // Equipar el arma (implementar lógica específica)
                    Debug.Log("Equipando arma: " + selectedItem.itemName);
                    break;

                case ItemType.Consumable:
                    // Usar consumible
                    Debug.Log("Usando consumible: " + selectedItem.itemName);

                    // Reducir cantidad
                    if (selectedItem.isConsumable)
                    {
                        RemoveItem(selectedItem.itemId, 1);

                        // Si se consumió el último, limpiar la selección
                        if (!inventoryItems.Contains(selectedItem))
                        {
                            selectedItem = null;
                            ClearItemDetails();
                        }
                    }
                    break;

                default:
                    Debug.Log("Usando item: " + selectedItem.itemName);
                    break;
            }
        }
    }

    private void DropSelectedItem()
    {
        if (selectedItem != null)
        {
            Debug.Log("Tirando item: " + selectedItem.itemName);

            // Implementar lógica para tirar físicamente el item en el mundo si es necesario

            // Remover del inventario
            RemoveItem(selectedItem.itemId, 1);

            // Si se eliminó por completo, limpiar la selección
            if (!inventoryItems.Contains(selectedItem))
            {
                selectedItem = null;
                ClearItemDetails();
            }
        }
    }

    private System.Collections.IEnumerator ShowItemNotification(string itemName)
    {
        // Buscar o crear un objeto para la notificación
        GameObject notifObj = GameObject.Find("ItemNotification");
        if (notifObj == null)
        {
            notifObj = new GameObject("ItemNotification");
            notifObj.transform.SetParent(mainCanvas.transform, false);

            // Crear fondo
            Image notifBg = notifObj.AddComponent<Image>();
            notifBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            RectTransform notifRect2 = notifObj.GetComponent<RectTransform>();
            notifRect2.anchorMin = new Vector2(0.5f, 0);
            notifRect2.anchorMax = new Vector2(0.5f, 0);
            notifRect2.sizeDelta = new Vector2(300, 60);

            // Crear texto
            GameObject textObj = new GameObject("NotificationText");
            textObj.transform.SetParent(notifObj.transform, false);

            TextMeshProUGUI notifText2 = textObj.AddComponent<TextMeshProUGUI>();
            notifText2.alignment = TextAlignmentOptions.Center;
            notifText2.fontSize = 18;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
        }

        // Configurar texto
        TextMeshProUGUI notifText = notifObj.GetComponentInChildren<TextMeshProUGUI>();
        notifText.text = "Recogido: " + itemName;
        notifText.color = Color.white;

        // Animar entrada
        RectTransform notifRect = notifObj.GetComponent<RectTransform>();
        notifRect.anchoredPosition = new Vector2(0, -60);

        float elapsedTime = 0f;
        float duration = 0.5f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            notifRect.anchoredPosition = Vector2.Lerp(new Vector2(0, -60), new Vector2(0, 80), t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Mantener visible
        yield return new WaitForSeconds(2f);

        // Animar salida
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            notifRect.anchoredPosition = Vector2.Lerp(new Vector2(0, 80), new Vector2(0, -60), t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(notifObj);
    }

    // Métodos públicos para uso externo

    public bool HasItem(string itemId, int quantity = 1)
    {
        InventoryItem item = inventoryItems.Find(i => i.itemId == itemId);
        return item != null && item.quantity >= quantity;
    }

    public int GetItemCount(string itemId)
    {
        InventoryItem item = inventoryItems.Find(i => i.itemId == itemId);
        return item != null ? item.quantity : 0;
    }

    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(inventoryItems);
    }

    public bool IsInventoryFull()
    {
        return inventoryItems.Count >= maxInventorySlots;
    }

    public int GetRemainingSlots()
    {
        return maxInventorySlots - inventoryItems.Count;
    }
}