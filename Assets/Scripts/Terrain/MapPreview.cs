using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq; // Added for .OrderByDescending()

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

    public bool autoUpdate;

    private void OnEnable()
    {
        // InitializeBiomeSystem(); // Removed as per new_code
        // RefreshBiomeList(); // Removed as per new_code
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
        // availableBiomes.Clear(); // Removed as per new_code

        // var allBiomes = BiomeSystem.GetAllBiomes(); // Removed as per new_code

        // if (allBiomes.Count == 0) // Removed as per new_code
        // { // Removed as per new_code
        //     Debug.LogError("No se encontraron biomas. Verificar que BiomeSystem.Initialize() funcione correctamente."); // Removed as per new_code
        //     return; // Removed as per new_code
        // } // Removed as per new_code

        // foreach (var biome in allBiomes) // Removed as per new_code
        // { // Removed as per new_code
        //     if (biome != null && !string.IsNullOrEmpty(biome.name)) // Removed as per new_code
        //     { // Removed as per new_code
        //         availableBiomes.Add(biome.name); // Removed as per new_code
        //     } // Removed as per new_code
        // } // Removed as per new_code

        // // Seleccionar el primer bioma por defecto si no hay ninguno seleccionado // Removed as per new_code
        // if (string.IsNullOrEmpty(selectedBiomeName) && availableBiomes.Count > 0) // Removed as per new_code
        // { // Removed as per new_code
        //     selectedBiomeName = availableBiomes[0]; // Removed as per new_code
        // } // Removed as per new_code

        // // Buscar el bioma seleccionado // Removed as per new_code
        // UpdateSelectedBiome(); // Removed as per new_code
    }

    private void UpdateSelectedBiome()
    {
        // if (string.IsNullOrEmpty(selectedBiomeName)) return; // Removed as per new_code

        // selectedBiome = null; // Removed as per new_code
        // foreach (var biome in BiomeSystem.GetAllBiomes()) // Removed as per new_code
        // { // Removed as per new_code
        //     if (biome != null && biome.name == selectedBiomeName) // Removed as per new_code
        //     { // Removed as per new_code
        //         selectedBiome = biome; // Removed as per new_code
        //         return; // Removed as per new_code
        //     } // Removed as per new_code
        // } // Removed as per new_code

        // // Si no se encontró el bioma, seleccionar el primero disponible // Removed as per new_code
        // if (selectedBiome == null && availableBiomes.Count > 0) // Removed as per new_code
        // { // Removed as per new_code
        //     selectedBiomeName = availableBiomes[0]; // Removed as per new_code
        //     UpdateSelectedBiome(); // Removed as per new_code
        // } // Removed as per new_code
    }

    public void DrawMapInEditor()
    {
        InitializeBiomeSystem();
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero, Vector2.zero, meshSettings.meshWorldSize);

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
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
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
        meshFilter.sharedMesh = meshData.CreateMesh();
        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
        meshRenderer.sharedMaterial = terrainMaterial;
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

                // Obtener datos de bioma para esta posicin
                var biomeData = BiomeSystem.GetBiomeData(worldPos);
                if (biomeData.influences != null && biomeData.influences.Count > 0)
                {
                    var mainBiome = biomeData.influences.OrderByDescending(i => i.influence).First();
                    var biomeDef = BiomeSystem.GetAllBiomes().Find(b => b.name == mainBiome.biomeName);
                    if (biomeDef != null)
                    {
                        pixels[y * resolution + x] = biomeDef.biomeColor;
                    }
                }
                else
                {
                    pixels[y * resolution + x] = Color.black;
                }
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
    }

#if UNITY_EDITOR
    // Mostrar información del bioma en el editor
    private void OnDrawGizmos()
    {
        // if ((drawMode != DrawMode.BiomeMesh && drawMode != DrawMode.BiomeMap) || // Removed as per new_code
        //     selectedBiome == null || // Removed as per new_code
        //     !meshFilter.gameObject.activeSelf) // Removed as per new_code
        //     return; // Removed as per new_code

        // Vector3 meshPosition = meshFilter.transform.position; // Removed as per new_code
        // Vector3 labelPosition = meshPosition + Vector3.up * 2f; // Removed as per new_code

        // Handles.color = selectedBiome.biomeColor; // Removed as per new_code
        // Handles.DrawWireCube(meshPosition, new Vector3(1, 0.1f, 1)); // Removed as per new_code

        // string biomeInfo = $"Bioma: {selectedBiome.name}\n" + // Removed as per new_code
        //                    $"Color: {ColorUtility.ToHtmlStringRGB(selectedBiome.biomeColor)}\n" + // Removed as per new_code
        //                    $"Temp: {selectedBiome.minTemperature:F2}-{selectedBiome.maxTemperature:F2}\n" + // Removed as per new_code
        //                    $"Humid: {selectedBiome.minHumidity:F2}-{selectedBiome.maxHumidity:F2}"; // Removed as per new_code

        // Handles.Label(labelPosition, biomeInfo); // Removed as per new_code
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
            // EditorGUILayout.LabelField($"Available Biomes: {mapPreview.availableBiomes.Count}"); // Removed as per new_code

            // Mostrar dropdown de biomas disponibles solo si tenemos biomas // Removed as per new_code
            // if (mapPreview.availableBiomes.Count > 0) // Removed as per new_code
            // { // Removed as per new_code
            //     int currentIndex = mapPreview.availableBiomes.IndexOf(mapPreview.selectedBiomeName); // Removed as per new_code
            //     if (currentIndex < 0) currentIndex = 0; // Removed as per new_code

            //     int newIndex = EditorGUILayout.Popup("Select Biome", currentIndex, mapPreview.availableBiomes.ToArray()); // Removed as per new_code

            //     if (newIndex != currentIndex && newIndex >= 0 && newIndex < mapPreview.availableBiomes.Count) // Removed as per new_code
            //     { // Removed as per new_code
            //         Undo.RecordObject(mapPreview, "Change Selected Biome"); // Removed as per new_code
            //         mapPreview.selectedBiomeName = mapPreview.availableBiomes[newIndex]; // Removed as per new_code
            //         mapPreview.UpdateSelectedBiome(); // Removed as per new_code

            //         if (mapPreview.autoUpdate) // Removed as per new_code
            //         { // Removed as per new_code
            //             mapPreview.DrawMapInEditor(); // Removed as per new_code
            //         } // Removed as per new_code
            //     } // Removed as per new_code

            //     // Mostrar información del bioma seleccionado // Removed as per new_code
            //     if (mapPreview.selectedBiome != null) // Removed as per new_code
            //     { // Removed as per new_code
            //         EditorGUILayout.Space(); // Removed as per new_code
            //         EditorGUILayout.LabelField("Selected Biome Info:", EditorStyles.boldLabel); // Removed as per new_code
            //         EditorGUILayout.LabelField($"Name: {mapPreview.selectedBiome.name}"); // Removed as per new_code
            //         EditorGUILayout.ColorField("Color", mapPreview.selectedBiome.biomeColor); // Removed as per new_code
            //         EditorGUILayout.LabelField($"Temperature: {mapPreview.selectedBiome.minTemperature:F2} - {mapPreview.selectedBiome.maxTemperature:F2}"); // Removed as per new_code
            //         EditorGUILayout.LabelField($"Humidity: {mapPreview.selectedBiome.minHumidity:F2} - {mapPreview.selectedBiome.maxHumidity:F2}"); // Removed as per new_code
            //     } // Removed as per new_code
            // } // Removed as per new_code
            // else // Removed as per new_code
            // { // Removed as per new_code
            //     EditorGUILayout.HelpBox("No biomes available. Click 'Reinitialize Biome System' to try again.", MessageType.Warning); // Removed as per new_code
            // } // Removed as per new_code

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