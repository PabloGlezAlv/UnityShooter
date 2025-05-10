using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image slotImage;
    [SerializeField] private Image itemImage;

    private InventoryItem currentItem;
    private Vector2Int gridPosition;
    private RectTransform rectTransform;
    private Canvas canvas;

    // Referencia al sistema de inventario
    private UIInventoryManager uiManager;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(Vector2Int position, UIInventoryManager manager)
    {
        gridPosition = position;
        uiManager = manager;
    }

    public void SetItem(InventoryItem item)
    {
        currentItem = item;

        if (item != null)
        {
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = item.Icon;
            // Ajustar tamaño según el item
            // TODO: Implementar lógica para tamaños de items
        }
        else
        {
            itemImage.gameObject.SetActive(false);
        }
    }

    public InventoryItem GetItem()
    {
        return currentItem;
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public void ClearSlot()
    {
        currentItem = null;
        itemImage.gameObject.SetActive(false);
    }

    #region Interface Implementations

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Click derecho - abrir menú contextual
            if (currentItem != null)
            {
                uiManager.ShowContextMenu(currentItem, transform.position);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Click izquierdo - seleccionar item
            uiManager.SelectSlot(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            uiManager.BeginDragItem(this, eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        uiManager.DragItem(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        uiManager.EndDragItem(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        uiManager.DropItemOnSlot(this, eventData);
    }

    #endregion
}