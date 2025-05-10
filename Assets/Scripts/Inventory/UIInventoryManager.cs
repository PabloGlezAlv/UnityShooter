using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIInventoryManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject itemTooltip;
    [SerializeField] private GameObject contextMenu;
    [SerializeField] private GameObject dragItemImage;

    [Header("Equipment Slots")]
    [SerializeField] private Transform equipmentSlotsContainer;
    [SerializeField] private List<EquipmentSlotUI> equipmentSlotUIs = new List<EquipmentSlotUI>();

    [Header("Configuration")]
    [SerializeField] private float cellSize = 64f;
    [SerializeField] private float spacing = 2f;

    // Referencia al sistema de inventario y grid
    private InventorySystem inventorySystem;
    private GridSystem gridSystem;

    // Estado de UI
    private InventorySlot selectedSlot;
    private InventoryItem draggedItem;
    private InventorySlot sourceSlot;
    private RectTransform dragRectTransform;
    private Canvas parentCanvas;

    // Grid UI
    private InventorySlot[,] gridSlots;

    private void Awake()
    {
        // Obtener referencias
        inventorySystem = InventorySystem.Instance;
        dragRectTransform = dragItemImage.GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        // Inicializar UI de drag & drop
        dragItemImage.SetActive(false);
    }

    private void Start()
    {
        // Inicializar UI
        InitializeGridUI();
        InitializeEquipmentUI();

        // Ocultar elementos de UI
        HideTooltip();
        HideContextMenu();
    }

    // Inicializar la UI de la cuadrícula del inventario
    private void InitializeGridUI()
    {
        gridSystem = inventorySystem.GetComponent<GridSystem>();
        if (gridSystem == null)
        {
            Debug.LogError("No se encontró el GridSystem");
            return;
        }

        int width = gridSystem.Width;
        int height = gridSystem.Height;

        // Ajustar tamaño del contenedor
        gridContainer.sizeDelta = new Vector2(
            width * (cellSize + spacing) + spacing,
            height * (cellSize + spacing) + spacing
        );

        // Crear array de slots
        gridSlots = new InventorySlot[width, height];

        // Crear slots
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotObject = Instantiate(slotPrefab, gridContainer);
                InventorySlot slot = slotObject.GetComponent<InventorySlot>();

                // Posicionar slot
                RectTransform slotRect = slotObject.GetComponent<RectTransform>();
                slotRect.anchoredPosition = new Vector2(
                    x * (cellSize + spacing) + spacing + cellSize / 2,
                    -y * (cellSize + spacing) - spacing - cellSize / 2
                );

                // Inicializar slot
                Vector2Int gridPos = new Vector2Int(x, y);
                slot.Initialize(gridPos, this);

                // Guardar referencia
                gridSlots[x, y] = slot;
            }
        }

        // Actualizar visualización
        UpdateGridUI();
    }

    // Inicializar la UI de los slots de equipamiento
    private void InitializeEquipmentUI()
    {
        // TODO: Inicializar UI de slots de equipamiento
    }

    // Actualizar la visualización de la cuadrícula
    public void UpdateGridUI()
    {
        // TODO: Actualizar visualización de los items en la cuadrícula
    }

    // Actualizar la visualización de un slot de equipamiento
    public void UpdateEquipmentSlotUI(string slotName, InventoryItem item)
    {
        // Buscar el slot UI correspondiente
        foreach (var slotUI in equipmentSlotUIs)
        {
            if (slotUI.SlotName == slotName)
            {
                slotUI.UpdateUI(item);
                return;
            }
        }
    }

    // Mostrar tooltip para un item
    public void ShowTooltip(InventoryItem item, Vector3 position)
    {
        if (item == null)
        {
            HideTooltip();
            return;
        }

        // TODO: Llenar información del tooltip
        itemTooltip.SetActive(true);
        itemTooltip.transform.position = position;
    }

    // Ocultar tooltip
    public void HideTooltip()
    {
        itemTooltip.SetActive(false);
    }

    // Mostrar menú contextual para un item
    public void ShowContextMenu(InventoryItem item, Vector3 position)
    {
        if (item == null)
        {
            HideContextMenu();
            return;
        }

        // TODO: Llenar opciones del menú contextual
        contextMenu.SetActive(true);
        contextMenu.transform.position = position;
    }

    // Ocultar menú contextual
    public void HideContextMenu()
    {
        contextMenu.SetActive(false);
    }

    // Seleccionar un slot
    public void SelectSlot(InventorySlot slot)
    {
        if (selectedSlot != null)
        {
            // Deseleccionar slot anterior
            // TODO: Actualizar visual de deselección
        }

        selectedSlot = slot;
        // TODO: Actualizar visual de selección

        // Mostrar tooltip
        ShowTooltip(slot.GetItem(), slot.transform.position + Vector3.up * 100f);
    }

    #region Drag & Drop

    // Iniciar arrastre de un item
    public void BeginDragItem(InventorySlot sourceSlot, PointerEventData eventData)
    {
        InventoryItem item = sourceSlot.GetItem();
        if (item == null) return;

        this.sourceSlot = sourceSlot;
        draggedItem = item;

        // Configurar imagen de arrastre
        dragItemImage.SetActive(true);
        dragItemImage.GetComponent<Image>().sprite = item.Icon;

        // Actualizar posición
        dragRectTransform.position = eventData.position;

        // Ocultar tooltip y menú contextual
        HideTooltip();
        HideContextMenu();
    }

    // Arrastrar item
    public void DragItem(PointerEventData eventData)
    {
        if (draggedItem == null) return;

        // Actualizar posición
        dragRectTransform.position = eventData.position;

        // TODO: Resaltar slots válidos para soltar el item
    }

    // Finalizar arrastre de un item
    public void EndDragItem(PointerEventData eventData)
    {
        if (draggedItem == null) return;

        // Ocultar imagen de arrastre
        dragItemImage.SetActive(false);

        // TODO: Si no se soltó en un slot válido, devolver al slot original

        draggedItem = null;
        sourceSlot = null;
    }

    // Soltar item en un slot
    public void DropItemOnSlot(InventorySlot targetSlot, PointerEventData eventData)
    {
        if (draggedItem == null) return;

        // Intentar mover el item
        Vector2Int newPosition = targetSlot.GetGridPosition();
        bool success = gridSystem.TryMoveItem(draggedItem, newPosition);

        if (success)
        {
            // Actualizar UI
            UpdateGridUI();
        }
        else
        {
            // TODO: Mostrar mensaje de error o efecto visual
        }
    }

    #endregion
}

// Clase para manejar los slots de equipamiento en la UI
[System.Serializable]
public class EquipmentSlotUI
{
    public string SlotName;
    public Image SlotImage;
    public Image ItemImage;
    public TextMeshProUGUI ItemNameText;

    public void UpdateUI(InventoryItem item)
    {
        if (item != null)
        {
            ItemImage.gameObject.SetActive(true);
            ItemImage.sprite = item.Icon;

            if (ItemNameText != null)
            {
                ItemNameText.gameObject.SetActive(true);
                ItemNameText.text = item.ItemName;
            }
        }
        else
        {
            ItemImage.gameObject.SetActive(false);

            if (ItemNameText != null)
            {
                ItemNameText.gameObject.SetActive(false);
            }
        }
    }
}