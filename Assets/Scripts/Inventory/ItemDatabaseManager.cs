using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabaseManager : ScriptableObject
{
    [System.Serializable]
    public class ItemData
    {
        public string itemId;
        public string itemName;
        public Sprite itemIcon;
        public string description;
        public InventorySystem.ItemType itemType;
        [Header("Weapon Properties")]
        public int damage;
        public float attackSpeed;
        public float range;
        [Header("Consumable Properties")]
        public int healthRestoration;
        public int staminaRestoration;
        public float duration;
        [Header("Visual Properties")]
        public GameObject worldPrefab; // Prefab para cuando el objeto está en el mundo
    }

    public List<ItemData> items = new List<ItemData>();

    // Método para encontrar un ítem por su ID
    public ItemData GetItemById(string id)
    {
        return items.Find(item => item.itemId == id);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ItemDatabaseManager))]
    public class ItemDatabaseManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ItemDatabaseManager database = (ItemDatabaseManager)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Create Example Items"))
            {
                // Crear algunos ítems de ejemplo
                CreateExampleItems(database);
            }
        }

        private void CreateExampleItems(ItemDatabaseManager database)
        {
            // Limpiar la base de datos existente si está vacía
            if (database.items.Count == 0)
            {
                // Ejemplo de arma
                var pistol = new ItemDatabaseManager.ItemData
                {
                    itemId = "weapon_pistol",
                    itemName = "Pistola",
                    description = "Una pistola estándar. Daño moderado, velocidad de disparo rápida.",
                    itemType = InventorySystem.ItemType.Weapon,
                    damage = 15,
                    attackSpeed = 1.2f,
                    range = 50f
                };
                database.items.Add(pistol);

                // Ejemplo de arma potente
                var rifle = new ItemDatabaseManager.ItemData
                {
                    itemId = "weapon_rifle",
                    itemName = "Rifle de Asalto",
                    description = "Un rifle automático. Alto daño y buena cadencia de tiro.",
                    itemType = InventorySystem.ItemType.Weapon,
                    damage = 25,
                    attackSpeed = 0.9f,
                    range = 80f
                };
                database.items.Add(rifle);

                // Ejemplo de consumible
                var medkit = new ItemDatabaseManager.ItemData
                {
                    itemId = "consumable_medkit",
                    itemName = "Botiquín",
                    description = "Restaura 50 puntos de salud.",
                    itemType = InventorySystem.ItemType.Consumable,
                    healthRestoration = 50
                };
                database.items.Add(medkit);

                // Consumible de estamina
                var energyDrink = new ItemDatabaseManager.ItemData
                {
                    itemId = "consumable_energy",
                    itemName = "Bebida energética",
                    description = "Restaura 30 puntos de estamina y aumenta temporalmente la velocidad.",
                    itemType = InventorySystem.ItemType.Consumable,
                    staminaRestoration = 30,
                    duration = 10f
                };
                database.items.Add(energyDrink);

                // Ejemplo de ítem de misión
                var keycard = new ItemDatabaseManager.ItemData
                {
                    itemId = "quest_keycard",
                    itemName = "Tarjeta de acceso",
                    description = "Permite abrir puertas de seguridad.",
                    itemType = InventorySystem.ItemType.Quest
                };
                database.items.Add(keycard);

                // Equipamiento - Armadura
                var armor = new ItemDatabaseManager.ItemData
                {
                    itemId = "equipment_armor",
                    itemName = "Chaleco Antibalas",
                    description = "Reduce el daño recibido en un 30%.",
                    itemType = InventorySystem.ItemType.Equipment
                };
                database.items.Add(armor);

                // Guarda los cambios
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                Debug.Log("Ejemplos de ítems creados en la base de datos.");
            }
            else
            {
                Debug.Log("La base de datos ya contiene ítems. Bórrala primero si quieres crear ejemplos.");
            }
        }
    }
#endif
}