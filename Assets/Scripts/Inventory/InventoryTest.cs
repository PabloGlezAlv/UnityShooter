using UnityEngine;
using UnityEditor;

public class InventoryVisualTest : MonoBehaviour
{
    [SerializeField] private ItemDatabaseManager itemDatabase;
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private bool showTestButtons = true;

    [Header("Test de ítems")]
    [SerializeField] private string testItemId = "weapon_pistol";
    [SerializeField] private int testItemQuantity = 1;

    private void OnGUI()
    {
        if (!showTestButtons || itemDatabase == null || inventorySystem == null)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 300));
        GUILayout.Box("Prueba de Inventario");

        if (GUILayout.Button("Abrir/Cerrar Inventario"))
        {
            // Simulamos presionar la tecla de inventario
            inventorySystem.SendMessage("ToggleInventory", null, SendMessageOptions.DontRequireReceiver);
        }

        GUILayout.Space(10);
        GUILayout.Label("Añadir objetos:");

        // Mostrar botones para cada ítem disponible en la base de datos
        foreach (var item in itemDatabase.items)
        {
            if (GUILayout.Button($"Añadir {item.itemName}"))
            {
                inventorySystem.AddItem(
                    item.itemId,
                    item.itemName,
                    item.itemIcon,
                    item.description,
                    item.itemType,
                    1
                );
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("Ítem Personalizado:");
        testItemId = GUILayout.TextField(testItemId);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Cantidad:");
        if (int.TryParse(GUILayout.TextField(testItemQuantity.ToString()), out int result))
        {
            testItemQuantity = result;
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Añadir ítem personalizado"))
        {
            var itemData = itemDatabase.GetItemById(testItemId);
            if (itemData != null)
            {
                inventorySystem.AddItem(
                    itemData.itemId,
                    itemData.itemName,
                    itemData.itemIcon,
                    itemData.description,
                    itemData.itemType,
                    testItemQuantity
                );
            }
            else
            {
                Debug.LogWarning($"No se encontró el ítem con ID: {testItemId}");
            }
        }

        GUILayout.EndArea();
    }
}