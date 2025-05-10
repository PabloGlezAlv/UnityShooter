using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    // Informaci�n b�sica del �tem
    [Header("Basic Info")]
    public string ItemID;
    public string ItemName;
    public string Description;
    public Sprite Icon;
    public GameObject Prefab;

    // Propiedades f�sicas
    [Header("Physical Properties")]
    public Vector2Int Size = new Vector2Int(1, 1); // Tama�o en la cuadr�cula (ancho, alto)
    public float Weight = 1.0f;
    public bool IsRotatable = false;

    // Clasificaci�n del �tem
    [Header("Classification")]
    public ItemType Type;
    public ItemRarity Rarity;

    // Propiedades econ�micas
    [Header("Economy")]
    public int BaseValue;
    public int SellValue;

    // Estado del �tem
    [Header("State")]
    public float Durability = 100f;
    public float MaxDurability = 100f;

    // Propiedades para visualizaci�n en inventario
    [HideInInspector] public Vector2Int GridPosition;
    [HideInInspector] public bool IsRotated = false;

    // M�todos virtuales para comportamientos espec�ficos
    public virtual void UseItem()
    {
        // Comportamiento base para usar el �tem
        Debug.Log($"Using item: {ItemName}");
    }

    public virtual void OnPickup()
    {
        // Comportamiento al recoger el �tem
    }

    public virtual void OnDrop()
    {
        // Comportamiento al soltar el �tem
    }

    // M�todo para obtener las celdas ocupadas por este �tem en la cuadr�cula
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

    // M�todo para rotar el �tem
    public void Rotate()
    {
        if (IsRotatable)
        {
            IsRotated = !IsRotated;
        }
    }

    // Obtener el valor actual del �tem basado en su durabilidad
    public int GetCurrentValue()
    {
        float durabilityFactor = Durability / MaxDurability;
        return Mathf.RoundToInt(BaseValue * durabilityFactor);
    }
}

// Enumeraciones para clasificar los �tems
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