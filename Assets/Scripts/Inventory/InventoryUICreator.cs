using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

public class InventoryUICreator : MonoBehaviour
{
    [MenuItem("GameObject/UI/Inventory UI")]
    static void CreateInventoryUI()
    {
        // Verificar si existe un canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Necesitas crear un Canvas primero.", "OK");
            return;
        }

        // Crear el panel principal
        GameObject inventoryUI = new GameObject("InventoryUI");
        inventoryUI.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = inventoryUI.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.2f, 0.2f);
        rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image bgImage = inventoryUI.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Título del inventario
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(inventoryUI.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 50);
        titleRect.anchoredPosition = new Vector2(0, 0);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTARIO";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        // Contenedor de ítems
        GameObject itemContainer = new GameObject("ItemContainer");
        itemContainer.transform.SetParent(inventoryUI.transform, false);

        RectTransform containerRect = itemContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0.2f);
        containerRect.anchorMax = new Vector2(0.7f, 0.9f);
        containerRect.offsetMin = new Vector2(20, 10);
        containerRect.offsetMax = new Vector2(-10, -10);

        Image containerBg = itemContainer.AddComponent<Image>();
        containerBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        GridLayoutGroup grid = itemContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(10, 10);
        grid.padding = new RectOffset(10, 10, 10, 10);

        // Panel de detalles
        GameObject detailPanel = new GameObject("ItemDetailPanel");
        detailPanel.transform.SetParent(inventoryUI.transform, false);

        RectTransform detailRect = detailPanel.AddComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(0.7f, 0.2f);
        detailRect.anchorMax = new Vector2(1, 0.9f);
        detailRect.offsetMin = new Vector2(10, 10);
        detailRect.offsetMax = new Vector2(-20, -10);

        Image detailBg = detailPanel.AddComponent<Image>();
        detailBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // Icono del ítem
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(detailPanel.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.8f);
        iconRect.anchorMax = new Vector2(0.5f, 0.8f);
        iconRect.sizeDelta = new Vector2(80, 80);
        iconRect.anchoredPosition = Vector2.zero;

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;

        // Nombre del ítem
        GameObject nameObj = new GameObject("ItemName");
        nameObj.transform.SetParent(detailPanel.transform, false);

        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.7f);
        nameRect.anchorMax = new Vector2(1, 0.75f);
        nameRect.offsetMin = new Vector2(10, 0);
        nameRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 18;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;

        // Descripción del ítem
        GameObject descObj = new GameObject("ItemDescription");
        descObj.transform.SetParent(detailPanel.transform, false);

        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.5f);
        descRect.anchorMax = new Vector2(1, 0.65f);
        descRect.offsetMin = new Vector2(10, 0);
        descRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 14;
        descText.color = Color.white;

        // Estadísticas del ítem
        GameObject statsObj = new GameObject("ItemStats");
        statsObj.transform.SetParent(detailPanel.transform, false);

        RectTransform statsRect = statsObj.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0.3f);
        statsRect.anchorMax = new Vector2(1, 0.45f);
        statsRect.offsetMin = new Vector2(10, 0);
        statsRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 14;
        statsText.color = Color.yellow;

        // Botón Usar
        GameObject useButtonObj = CreateButton("UseButton", detailPanel.transform, "USAR",
            new Vector2(0.1f, 0.15f), new Vector2(0.45f, 0.25f),
            new Color(0.2f, 0.6f, 0.2f, 1));

        // Botón Tirar
        GameObject dropButtonObj = CreateButton("DropButton", detailPanel.transform, "TIRAR",
            new Vector2(0.55f, 0.15f), new Vector2(0.9f, 0.25f),
            new Color(0.6f, 0.2f, 0.2f, 1));

        // Botón Cerrar
        GameObject closeButtonObj = CreateButton("CloseButton", inventoryUI.transform, "X",
            new Vector2(1, 1), new Vector2(1, 1),
            new Color(0.7f, 0.2f, 0.2f, 1));

        RectTransform closeRect = closeButtonObj.GetComponent<RectTransform>();
        closeRect.anchoredPosition = new Vector2(-20, -20);
        closeRect.sizeDelta = new Vector2(40, 40);

        // Crear el prefab del ItemSlot
        CreateItemSlotPrefab();

        // Preguntamos al usuario si quiere guardar el prefab de la UI
        bool saveUIPrefab = EditorUtility.DisplayDialog(
            "Guardar Prefab de UI",
            "¿Deseas guardar esta UI como un prefab para usarla en el InventorySystem?",
            "Sí, guardar prefab",
            "No, solo crear temporalmente");

        if (saveUIPrefab)
        {
            // Verificar si existe la carpeta Resources/UI
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "UI");
            }

            string prefabPath = "Assets/Resources/UI/InventoryUIPrefab.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(inventoryUI, prefabPath);

            if (prefab != null)
            {
                Debug.Log("Prefab de UI de inventario guardado en: " + prefabPath);

                // Preguntar si se quiere asignar este prefab a un sistema de inventario existente
                bool assignToSystem = EditorUtility.DisplayDialog(
                    "Asignar Prefab",
                    "¿Deseas buscar y asignar este prefab a un InventorySystem existente en la escena?",
                    "Sí, asignar",
                    "No, lo haré manualmente");

                if (assignToSystem)
                {
                    InventorySystem[] inventorySystems = FindObjectsOfType<InventorySystem>();
                    if (inventorySystems.Length > 0)
                    {
                        // Si hay varios, mostrar un selector o asignar al primero
                        InventorySystem targetSystem = inventorySystems[0]; // Por simplicidad asignamos al primero

                        SerializedObject serializedSystem = new SerializedObject(targetSystem);
                        SerializedProperty prefabProperty = serializedSystem.FindProperty("inventoryUIPrefab");
                        prefabProperty.objectReferenceValue = prefab;

                        serializedSystem.ApplyModifiedProperties();
                        Debug.Log("Prefab asignado al sistema de inventario: " + targetSystem.name);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No encontrado", "No se encontró ningún InventorySystem en la escena.", "OK");
                    }
                }
            }
        }
        else
        {
            Debug.Log("UI de inventario creada temporalmente. No se ha guardado como prefab.");
        }
    }

    static GameObject CreateButton(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = new Vector2(10, 10);
        buttonRect.offsetMax = new Vector2(-10, -10);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(color.r + 0.1f, color.g + 0.1f, color.b + 0.1f, color.a);
        colors.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f, color.a);
        button.colors = colors;

        // Crear el texto del botón
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 16;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        return buttonObj;
    }

    static void CreateItemSlotPrefab()
    {
        // Crear un prefab para el slot de item
        GameObject slotPrefab = new GameObject("ItemSlotPrefab");

        RectTransform slotRect = slotPrefab.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(80, 80);

        Image slotImage = slotPrefab.AddComponent<Image>();
        slotImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        Button slotButton = slotPrefab.AddComponent<Button>();
        ColorBlock colors = slotButton.colors;
        colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        slotButton.colors = colors;

        // Crear el icono del item
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(slotPrefab.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;

        // Crear el texto de cantidad
        GameObject qtyObj = new GameObject("ItemQuantity");
        qtyObj.transform.SetParent(slotPrefab.transform, false);

        RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
        qtyRect.anchorMin = new Vector2(0.7f, 0);
        qtyRect.anchorMax = new Vector2(1, 0.3f);
        qtyRect.offsetMin = Vector2.zero;
        qtyRect.offsetMax = Vector2.zero;

        TextMeshProUGUI qtyText = qtyObj.AddComponent<TextMeshProUGUI>();
        qtyText.fontSize = 14;
        qtyText.alignment = TextAlignmentOptions.Center;
        qtyText.color = Color.yellow;

        // Guardar el prefab
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
        }

        string prefabPath = "Assets/Resources/UI/ItemSlotPrefab.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slotPrefab, prefabPath);

        // Destruir el objeto temporal de la escena
        UnityEngine.Object.DestroyImmediate(slotPrefab);

        if (prefab != null)
        {
            Debug.Log("Prefab de slot de item guardado en: " + prefabPath);
        }
        else
        {
            Debug.LogError("No se pudo guardar el prefab de slot de item");
        }
    }
}
#endif