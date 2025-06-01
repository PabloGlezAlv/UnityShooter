using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapPreview : MonoBehaviour
{
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public enum DrawMode { NoiseMap, Mesh, FalloffMap, BiomeMesh, BiomeMap };
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public Material terrainMaterial;

    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;

    [Header("Biome Settings")]
    [Tooltip("Selected biome for preview mode")]
    public string selectedBiomeName;
    [Tooltip("Override mesh color with biome color in preview")]
    public bool useBiomeColor = true;
    [Tooltip("Show biome temperature and humidity noise")]
    public bool showClimateData = false;

    private List<string> availableBiomes = new List<string>();
    private BiomeDefinition selectedBiome;

    public bool autoUpdate;

    private void OnEnable()
    {
        InitializeBiomeSystem();
        RefreshBiomeList();
    }

    private void InitializeBiomeSystem()
    {
        // Forzar inicialización del BiomeSystem
        BiomeSystem.Initialize();

        // Si aún no hay biomas, es probable que sea un problema de inicialización en editor
        if (BiomeSystem.GetAllBiomes().Count == 0)
        {
            Debug.LogWarning("BiomeSystem no se inicializó correctamente. Intentando reinicializar...");

            // Limpiar cache y reinicializar
            BiomeSystem.ClearCache();
            BiomeSystem.Initialize();
        }
    }

    private void RefreshBiomeList()
    {
        availableBiomes.Clear();

        var allBiomes = BiomeSystem.GetAllBiomes();

        if (allBiomes.Count == 0)
        {
            Debug.LogError("No se encontraron biomas. Verificar que BiomeSystem.Initialize() funcione correctamente.");
            return;
        }

        foreach (var biome in allBiomes)
        {
            if (biome != null && !string.IsNullOrEmpty(biome.name))
            {
                availableBiomes.Add(biome.name);
            }
        }

        // Seleccionar el primer bioma por defecto si no hay ninguno seleccionado
        if (string.IsNullOrEmpty(selectedBiomeName) && availableBiomes.Count > 0)
        {
            selectedBiomeName = availableBiomes[0];
        }

        // Buscar el bioma seleccionado
        UpdateSelectedBiome();
    }

    private void UpdateSelectedBiome()
    {
        if (string.IsNullOrEmpty(selectedBiomeName)) return;

        selectedBiome = null;
        foreach (var biome in BiomeSystem.GetAllBiomes())
        {
            if (biome != null && biome.name == selectedBiomeName)
            {
                selectedBiome = biome;
                return;
            }
        }

        // Si no se encontró el bioma, seleccionar el primero disponible
        if (selectedBiome == null && availableBiomes.Count > 0)
        {
            selectedBiomeName = availableBiomes[0];
            UpdateSelectedBiome();
        }
    }

    public void DrawMapInEditor()
    {
        InitializeBiomeSystem();
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        // OBTENER DATOS DE BIOMA PARA LA GENERACIÓN
        BiomeSystem.BiomeData biomeData = default;
        if (!string.IsNullOrEmpty(selectedBiomeName))
        {
            UpdateSelectedBiome();
            if (selectedBiome != null)
            {
                biomeData = new BiomeSystem.BiomeData
                {
                    biomeName = selectedBiome.name,
                    biomeColor = selectedBiome.biomeColor,
                    temperature = (selectedBiome.minTemperature + selectedBiome.maxTemperature) / 2f,
                    humidity = (selectedBiome.minHumidity + selectedBiome.maxHumidity) / 2f
                };
            }
        }

        // GENERAR HEIGHTMAP CON BIOMA
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
            meshSettings.numVertsPerLine,
            meshSettings.numVertsPerLine,
            heightMapSettings,
            Vector2.zero,
            biomeData);

        if (drawMode == DrawMode.NoiseMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
        }
        else if (drawMode == DrawMode.BiomeMesh)
        {
            DrawBiomeMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
        }
        else if (drawMode == DrawMode.BiomeMap)
        {
            DrawBiomeTexture();
        }
    }

    public void DrawTexture(Texture2D texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
        textureRender.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial = terrainMaterial;
        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    public void DrawBiomeMesh(MeshData meshData)
    {
        // Actualizar el bioma seleccionado por si cambió
        UpdateSelectedBiome();

        meshFilter.sharedMesh = meshData.CreateMesh();
        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);

        // Aplicar color del bioma si está habilitado
        if (useBiomeColor && selectedBiome != null)
        {
            // Crear una copia del material para no modificar el original
            Material biomeMaterial = new Material(terrainMaterial);
            biomeMaterial.color = selectedBiome.biomeColor;
            meshRenderer.sharedMaterial = biomeMaterial;
        }
        else
        {
            meshRenderer.sharedMaterial = terrainMaterial;
        }
    }

    public void DrawBiomeTexture()
    {
        // Generar un mapa de biomas como textura
        int resolution = meshSettings.numVertsPerLine;
        Texture2D biomeTexture = GenerateBiomeTexture(resolution);
        DrawTexture(biomeTexture);
    }

    private Texture2D GenerateBiomeTexture(int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];

        float scale = meshSettings.meshWorldSize / resolution;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector3 worldPos = new Vector3(
                    (x - resolution / 2) * scale,
                    0,
                    (y - resolution / 2) * scale
                );

                // Obtener datos de bioma para esta posición
                var biomeData = BiomeSystem.GetBiomeData(worldPos);
                pixels[y * resolution + x] = biomeData.biomeColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    void OnValidate()
    {
        // Asegurar inicialización en el editor
        if (!Application.isPlaying)
        {
            InitializeBiomeSystem();
            RefreshBiomeList();
        }

        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }

        // Actualizar el bioma seleccionado si cambió
        if (selectedBiome == null || selectedBiome.name != selectedBiomeName)
        {
            UpdateSelectedBiome();
        }
    }

#if UNITY_EDITOR
    // Mostrar información del bioma en el editor
    private void OnDrawGizmos()
    {
        if ((drawMode != DrawMode.BiomeMesh && drawMode != DrawMode.BiomeMap) ||
            selectedBiome == null ||
            !meshFilter.gameObject.activeSelf)
            return;

        Vector3 meshPosition = meshFilter.transform.position;
        Vector3 labelPosition = meshPosition + Vector3.up * 2f;

        Handles.color = selectedBiome.biomeColor;
        Handles.DrawWireCube(meshPosition, new Vector3(1, 0.1f, 1));

        string biomeInfo = $"Bioma: {selectedBiome.name}\n" +
                           $"Color: {ColorUtility.ToHtmlStringRGB(selectedBiome.biomeColor)}\n" +
                           $"Temp: {selectedBiome.minTemperature:F2}-{selectedBiome.maxTemperature:F2}\n" +
                           $"Humid: {selectedBiome.minHumidity:F2}-{selectedBiome.maxHumidity:F2}";

        Handles.Label(labelPosition, biomeInfo);
    }

    // Editor personalizado para mostrar un dropdown de biomas
    [CustomEditor(typeof(MapPreview))]
    public class MapPreviewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            MapPreview mapPreview = (MapPreview)target;

            // Dibujar los controles por defecto
            DrawDefaultInspector();

            // Separador
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Biome Controls", EditorStyles.boldLabel);

            // Botón para reinicializar el sistema de biomas
            if (GUILayout.Button("Reinitialize Biome System"))
            {
                BiomeSystem.ClearCache();
                mapPreview.InitializeBiomeSystem();
                mapPreview.RefreshBiomeList();
            }

            // Mostrar información de debug
            EditorGUILayout.LabelField($"Available Biomes: {mapPreview.availableBiomes.Count}");

            // Mostrar dropdown de biomas disponibles solo si tenemos biomas
            if (mapPreview.availableBiomes.Count > 0)
            {
                int currentIndex = mapPreview.availableBiomes.IndexOf(mapPreview.selectedBiomeName);
                if (currentIndex < 0) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Select Biome", currentIndex, mapPreview.availableBiomes.ToArray());

                if (newIndex != currentIndex && newIndex >= 0 && newIndex < mapPreview.availableBiomes.Count)
                {
                    Undo.RecordObject(mapPreview, "Change Selected Biome");
                    mapPreview.selectedBiomeName = mapPreview.availableBiomes[newIndex];
                    mapPreview.UpdateSelectedBiome();

                    if (mapPreview.autoUpdate)
                    {
                        mapPreview.DrawMapInEditor();
                    }
                }

                // Mostrar información del bioma seleccionado
                if (mapPreview.selectedBiome != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Selected Biome Info:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Name: {mapPreview.selectedBiome.name}");
                    EditorGUILayout.ColorField("Color", mapPreview.selectedBiome.biomeColor);
                    EditorGUILayout.LabelField($"Temperature: {mapPreview.selectedBiome.minTemperature:F2} - {mapPreview.selectedBiome.maxTemperature:F2}");
                    EditorGUILayout.LabelField($"Humidity: {mapPreview.selectedBiome.minHumidity:F2} - {mapPreview.selectedBiome.maxHumidity:F2}");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No biomes available. Click 'Reinitialize Biome System' to try again.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Botón para actualizar la vista
            if (GUILayout.Button("Update Preview"))
            {
                mapPreview.DrawMapInEditor();
            }

            // Aplicar cambios si es necesario
            if (GUI.changed)
            {
                EditorUtility.SetDirty(mapPreview);
                if (mapPreview.autoUpdate)
                {
                    mapPreview.DrawMapInEditor();
                }
            }
        }
    }
#endif
}