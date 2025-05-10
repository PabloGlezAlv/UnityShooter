using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EquipmentManager : MonoBehaviour
{
    // Categorías de equipamiento
    [Serializable]
    public class EquipmentSlot
    {
        public string SlotName;
        public Transform AttachPoint; // Punto de fijación para modelos 3D
        public ItemType[] AllowedTypes; // Tipos de items permitidos en este slot
        public InventoryItem EquippedItem; // Item actualmente equipado
    }

    // Lista de slots de equipamiento disponibles
    [SerializeField] private List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>();

    // Referencias a otros sistemas
    [SerializeField] private UIInventoryManager uiManager;

    // Diccionario para acceso rápido a los slots por nombre
    private Dictionary<string, EquipmentSlot> slotDictionary = new Dictionary<string, EquipmentSlot>();

    // Eventos para notificar cambios en el equipamiento
    public event Action<string, InventoryItem> OnEquipmentChanged;

    private void Awake()
    {
        // Inicializar diccionario de slots
        foreach (var slot in equipmentSlots)
        {
            slotDictionary[slot.SlotName] = slot;
        }
    }

    private void Start()
    {
        // Inicializar UI para los slots de equipamiento
        UpdateAllEquipmentUI();
    }

    // Equipar un item en un slot específico
    public bool EquipItem(InventoryItem item, string slotName)
    {
        if (!slotDictionary.TryGetValue(slotName, out EquipmentSlot slot))
        {
            Debug.LogWarning($"Slot no encontrado: {slotName}");
            return false;
        }

        // Verificar si el tipo de item es permitido en este slot
        bool isAllowed = false;
        foreach (var allowedType in slot.AllowedTypes)
        {
            if (item.Type == allowedType)
            {
                isAllowed = true;
                break;
            }
        }

        if (!isAllowed)
        {
            Debug.LogWarning($"Item tipo {item.Type} no permitido en slot {slotName}");
            return false;
        }

        // Desequipar item actual si existe
        if (slot.EquippedItem != null)
        {
            UnequipItem(slotName);
        }

        // Equipar nuevo item
        slot.EquippedItem = item;

        // Instanciar modelo 3D si es necesario
        if (item.Prefab != null && slot.AttachPoint != null)
        {
            GameObject itemObject = Instantiate(item.Prefab, slot.AttachPoint);
            itemObject.transform.localPosition = Vector3.zero;
            itemObject.transform.localRotation = Quaternion.identity;
        }

        // Aplicar efectos del item (stats, etc.)
        ApplyItemEffects(item);

        // Notificar cambio
        OnEquipmentChanged?.Invoke(slotName, item);

        // Actualizar UI
        UpdateEquipmentUI(slotName);

        return true;
    }

    // Desequipar un item de un slot específico
    public InventoryItem UnequipItem(string slotName)
    {
        if (!slotDictionary.TryGetValue(slotName, out EquipmentSlot slot))
        {
            Debug.LogWarning($"Slot no encontrado: {slotName}");
            return null;
        }

        if (slot.EquippedItem == null)
        {
            return null;
        }

        InventoryItem unequippedItem = slot.EquippedItem;

        // Eliminar efectos del item
        RemoveItemEffects(unequippedItem);

        // Destruir modelo 3D si existe
        if (slot.AttachPoint != null && slot.AttachPoint.childCount > 0)
        {
            foreach (Transform child in slot.AttachPoint)
            {
                Destroy(child.gameObject);
            }
        }

        // Limpiar referencia
        slot.EquippedItem = null;

        // Notificar cambio
        OnEquipmentChanged?.Invoke(slotName, null);

        // Actualizar UI
        UpdateEquipmentUI(slotName);

        return unequippedItem;
    }

    // Obtener item equipado en un slot específico
    public InventoryItem GetEquippedItem(string slotName)
    {
        if (slotDictionary.TryGetValue(slotName, out EquipmentSlot slot))
        {
            return slot.EquippedItem;
        }

        Debug.LogWarning($"Slot no encontrado: {slotName}");
        return null;
    }

    // Comprobar si un slot específico está ocupado
    public bool IsSlotOccupied(string slotName)
    {
        if (slotDictionary.TryGetValue(slotName, out EquipmentSlot slot))
        {
            return slot.EquippedItem != null;
        }

        Debug.LogWarning($"Slot no encontrado: {slotName}");
        return false;
    }

    // Actualizar UI para un slot específico
    private void UpdateEquipmentUI(string slotName)
    {
        // TODO: Llamar al UIManager para actualizar la representación visual del slot
        if (uiManager != null)
        {
            //uiManager.UpdateEquipmentSlotUI(slotName, slotDictionary[slotName].EquippedItem);
        }
    }

    // Actualizar UI para todos los slots
    private void UpdateAllEquipmentUI()
    {
        foreach (var slotName in slotDictionary.Keys)
        {
            UpdateEquipmentUI(slotName);
        }
    }

    // Aplicar efectos de un item (stats, buffs, etc.)
    private void ApplyItemEffects(InventoryItem item)
    {
        // TODO: Implementar efectos específicos según tipo de item
    }

    // Eliminar efectos de un item
    private void RemoveItemEffects(InventoryItem item)
    {
        // TODO: Implementar eliminación de efectos específicos según tipo de item
    }
}