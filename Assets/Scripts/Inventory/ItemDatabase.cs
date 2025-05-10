using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemDatabase : MonoBehaviour
{
    // Singleton para acceso global
    public static ItemDatabase Instance { get; private set; }

    // Lista de todos los items disponibles en el juego
    [SerializeField] private List<InventoryItem> allItems = new List<InventoryItem>();

    // Diccionario para acceso rápido a los items por ID
    private Dictionary<string, InventoryItem> itemDictionary = new Dictionary<string, InventoryItem>();

    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Crear diccionario de items
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        // Cargar todos los items de Resources si la lista está vacía
        if (allItems.Count == 0)
        {
            allItems = Resources.LoadAll<InventoryItem>("Items").ToList();
        }

        // Construir el diccionario
        itemDictionary.Clear();
        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemID))
            {
                // Evitar duplicados
                if (!itemDictionary.ContainsKey(item.ItemID))
                {
                    itemDictionary.Add(item.ItemID, item);
                }
                else
                {
                    Debug.LogWarning($"Item duplicado con ID: {item.ItemID}");
                }
            }
            else
            {
                Debug.LogError($"Item inválido en la base de datos: {item?.name ?? "null"}");
            }
        }

        Debug.Log($"ItemDatabase inicializada con {itemDictionary.Count} items");
    }

    // Obtener un item por su ID
    public InventoryItem GetItemByID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("Se solicitó un item con ID vacío");
            return null;
        }

        if (itemDictionary.TryGetValue(itemID, out InventoryItem item))
        {
            // Crear una instancia nueva para evitar modificar el original
            return Instantiate(item);
        }

        Debug.LogWarning($"Item no encontrado con ID: {itemID}");
        return null;
    }

    // Obtener todos los items de un tipo específico
    public List<InventoryItem> GetItemsByType(ItemType type)
    {
        return allItems.Where(item => item.Type == type).Select(item => Instantiate(item)).ToList();
    }

    // Obtener todos los items de una rareza específica
    public List<InventoryItem> GetItemsByRarity(ItemRarity rarity)
    {
        return allItems.Where(item => item.Rarity == rarity).Select(item => Instantiate(item)).ToList();
    }

    // Buscar items por nombre (coincidencia parcial)
    public List<InventoryItem> SearchItemsByName(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return new List<InventoryItem>();
        }

        searchTerm = searchTerm.ToLower();
        return allItems
            .Where(item => item.ItemName.ToLower().Contains(searchTerm))
            .Select(item => Instantiate(item))
            .ToList();
    }

    // Obtener un item aleatorio
    public InventoryItem GetRandomItem()
    {
        if (allItems.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, allItems.Count);
        return Instantiate(allItems[randomIndex]);
    }

    // Obtener un item aleatorio de un tipo específico
    public InventoryItem GetRandomItemByType(ItemType type)
    {
        var itemsOfType = allItems.Where(item => item.Type == type).ToList();
        if (itemsOfType.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, itemsOfType.Count);
        return Instantiate(itemsOfType[randomIndex]);
    }

    // Obtener un item aleatorio de una rareza específica
    public InventoryItem GetRandomItemByRarity(ItemRarity rarity)
    {
        var itemsOfRarity = allItems.Where(item => item.Rarity == rarity).ToList();
        if (itemsOfRarity.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, itemsOfRarity.Count);
        return Instantiate(itemsOfRarity[randomIndex]);
    }

    // Contar la cantidad total de items en la base de datos
    public int GetTotalItemCount()
    {
        return allItems.Count;
    }

    // Agregar un nuevo item a la base de datos (útil para mods o contenido dinámico)
    public bool AddItemToDatabase(InventoryItem newItem)
    {
        if (newItem == null || string.IsNullOrEmpty(newItem.ItemID))
        {
            Debug.LogError("Intento de agregar un item inválido a la base de datos");
            return false;
        }

        if (itemDictionary.ContainsKey(newItem.ItemID))
        {
            Debug.LogWarning($"Ya existe un item con ID: {newItem.ItemID}");
            return false;
        }

        // Agregar a las colecciones
        allItems.Add(newItem);
        itemDictionary.Add(newItem.ItemID, newItem);

        return true;
    }

    // Para debug: listar todos los items en la consola
    public void DebugListAllItems()
    {
        Debug.Log($"===== Base de datos de items ({allItems.Count} items) =====");
        foreach (var item in allItems)
        {
            Debug.Log($"ID: {item.ItemID}, Nombre: {item.ItemName}, Tipo: {item.Type}, Rareza: {item.Rarity}");
        }
    }
}