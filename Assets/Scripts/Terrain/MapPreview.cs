using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class MapPreview : MonoBehaviour
{
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public enum DrawMode { NoiseMap, Mesh, FalloffMap, BiomeMesh };
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

    private List<string> availableBiomes = new List<string>();
    private int selectedBiomeIndex = 0;
    private BiomeDefinition selectedBiome;

    public bool autoUpdate;

    private void OnEnable()
    {
        // Inicializar BiomeSystem si no se ha hecho
        if (BiomeSystem.GetAllBiomes().Count == 0)
        {
            BiomeSystem.Initialize();
        }

        // Cargar lista de biomas disponibles
        RefreshBiomeList();
    }

    private void RefreshBiomeList()
    {
        availableBiomes.Clear();

        foreach (var biome in BiomeSystem.GetAllBiomes())
        {
            availableBiomes.Add(biome.name);
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

        foreach (var biome in BiomeSystem.GetAllBiomes())
        {
            if (biome.name == selectedBiomeName)
            {
                selectedBiome = biome;
                return;
            }
        }
    }

    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

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
        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    public void DrawBiomeMesh(MeshData meshData)
    {
        // Actualizar el biome seleccionado por si cambió
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
        if (drawMode != DrawMode.BiomeMesh || selectedBiome == null || !meshFilter.gameObject.activeSelf)
            return;

        Vector3 meshPosition = meshFilter.transform.position;
        Vector3 labelPosition = meshPosition + Vector3.up * 2f;

        UnityEditor.Handles.color = selectedBiome.biomeColor;
        UnityEditor.Handles.DrawWireCube(meshPosition, new Vector3(1, 0.1f, 1));

        string biomeInfo = $"Bioma: {selectedBiome.name}\n" +
                           $"Color: {ColorUtility.ToHtmlStringRGB(selectedBiome.biomeColor)}";

        UnityEditor.Handles.Label(labelPosition, biomeInfo);
    }

    // Editor personalizado para mostrar un dropdown de biomas
    [UnityEditor.CustomEditor(typeof(MapPreview))]
    public class MapPreviewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            MapPreview mapPreview = (MapPreview)target;

            // Dibujar los controles por defecto
            base.OnInspectorGUI();

            // Separador
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Biome Selector", EditorStyles.boldLabel);

            // Mostrar dropdown de biomas disponibles solo si tenemos biomas
            if (mapPreview.availableBiomes.Count > 0)
            {
                int currentIndex = mapPreview.availableBiomes.IndexOf(mapPreview.selectedBiomeName);
                if (currentIndex < 0) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Select Biome", currentIndex, mapPreview.availableBiomes.ToArray());

                if (newIndex != currentIndex)
                {
                    Undo.RecordObject(mapPreview, "Change Selected Biome");
                    mapPreview.selectedBiomeName = mapPreview.availableBiomes[newIndex];
                    mapPreview.UpdateSelectedBiome();

                    if (mapPreview.autoUpdate)
                    {
                        mapPreview.DrawMapInEditor();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No biomes available. Make sure BiomeSystem is initialized.", MessageType.Warning);
            }

            // Botón para actualizar la vista
            if (GUILayout.Button("Update Preview"))
            {
                mapPreview.DrawMapInEditor();
            }
        }
    }
#endif
}