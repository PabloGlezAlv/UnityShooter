using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    // Información básica del ítem
    [Header("Basic Info")]
    public string ItemID;
    public string ItemName;
    public string Description;
    public Sprite Icon;
    public GameObject Prefab;

    // Propiedades físicas
    [Header("Physical Properties")]
    public Vector2Int Size = new Vector2Int(1, 1); // Tamaño en la cuadrícula (ancho, alto)
    public float Weight = 1.0f;
    public bool IsRotatable = false;

    // Clasificación del ítem
    [Header("Classification")]
    public ItemType Type;
    public ItemRarity Rarity;

    // Propiedades económicas
    [Header("Economy")]
    public int BaseValue;
    public int SellValue;

    // Estado del ítem
    [Header("State")]
    public float Durability = 100f;
    public float MaxDurability = 100f;

    // Propiedades para visualización en inventario
    [HideInInspector] public Vector2Int GridPosition;
    [HideInInspector] public bool IsRotated = false;

    // Métodos virtuales para comportamientos específicos
    public virtual void UseItem()
    {
        // Comportamiento base para usar el ítem
        Debug.Log($"Using item: {ItemName}");
    }

    public virtual void OnPickup()
    {
        // Comportamiento al recoger el ítem
    }

    public virtual void OnDrop()
    {
        // Comportamiento al soltar el ítem
    }

    // Método para obtener las celdas ocupadas por este ítem en la cuadrícula
    public Vector2Int[] GetOccupiedCells()
    {
        Vector2Int actualSize = IsRotated ? new Vector2Int(Size.y, Size.x) : Size;
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int x = 0; x < actualSize.x; x++)
        {
            for (int y = 0; y < actualSize.y; y++)
            {
                cells.Add(GridPosition + new Vector2Int(x, y));
            }
        }

        return cells.ToArray();
    }

    // Método para rotar el ítem
    public void Rotate()
    {
        if (IsRotatable)
        {
            IsRotated = !IsRotated;
        }
    }

    // Obtener el valor actual del ítem basado en su durabilidad
    public int GetCurrentValue()
    {
        float durabilityFactor = Durability / MaxDurability;
        return Mathf.RoundToInt(BaseValue * durabilityFactor);
    }
}

// Enumeraciones para clasificar los ítems
public enum ItemType
{
    Weapon,
    Armor,
    Ammo,
    Medical,
    Food,
    Tool,
    Key,
    Currency,
    Quest,
    Misc
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}