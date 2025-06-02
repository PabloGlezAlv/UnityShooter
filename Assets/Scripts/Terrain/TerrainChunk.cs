using UnityEngine;

public class TerrainChunk
{
    const float colliderGenerationDistanceThreshold = 5;
    public event System.Action<TerrainChunk, bool> onVisibilityChanged;
    public Vector2 coord;

    GameObject meshObject;
    Vector2 sampleCentre;
    Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    TerrainChunkWaterGenerator waterGenerator;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    int colliderLODIndex;

    HeightMap heightMap;
    bool heightMapReceived;
    int previousLODIndex = -1;
    bool hasSetCollider;
    float maxViewDst;

    HeightMapSettings heightMapSettings;
    public MeshSettings MeshSettings;
    Transform viewer;

    // Variables para el agua
    bool enableWater;
    float waterThreshold;
    Material waterMaterial;

    // Variables para el bioma
    private BiomeSystem.BiomeData previousBiomeData;
    public BiomeSystem.BiomeData biomeData;
    private bool biomeDataReceived;

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels,
                         int colliderLODIndex, Transform parent, Transform viewer, Material material,
                         bool enableWater = false, float waterThreshold = 0, Material waterMaterial = null)
    {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapSettings = heightMapSettings;
        this.MeshSettings = meshSettings;
        this.viewer = viewer;

        // Configuración del agua
        this.enableWater = enableWater;
        this.waterThreshold = waterThreshold;
        this.waterMaterial = waterMaterial;

        sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        // Configurar generador de agua si está habilitado
        if (enableWater && waterMaterial != null)
        {
            waterGenerator = meshObject.AddComponent<TerrainChunkWaterGenerator>();
            waterGenerator.Initialize(waterThreshold, waterMaterial);
        }

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;

        // Obtener información del bioma para el centro del chunk
        Vector3 chunkWorldPos = new Vector3(position.x, 0, position.y);
        LoadBiomeData(chunkWorldPos);

        // Añadir componente para mostrar información del bioma en el editor
        meshObject.AddComponent<TerrainChunkBiomeDebug>().chunk = this;
    }
    private void RegenerateWithNewBiome()
    {
        heightMapReceived = false;
        hasSetCollider = false;
        previousLODIndex = -1;

        // Limpiar meshes LOD existentes
        for (int i = 0; i < lodMeshes.Length; i++)
        {
            lodMeshes[i].hasRequestedMesh = false;
            lodMeshes[i].hasMesh = false;
            lodMeshes[i].mesh = null;
        }

        // Solicitar nuevo heightmap con datos de bioma
        ThreadedDataRequester.RequestData(() =>
            HeightMapGenerator.GenerateHeightMap(
                MeshSettings.numVertsPerLine,
                MeshSettings.numVertsPerLine,
                heightMapSettings,
                sampleCentre,
                biomeData),
            OnHeightMapReceived);
    }
    private void LoadBiomeData(Vector3 position)
    {
        BiomeSystem.BiomeData newBiomeData = BiomeSystem.GetBiomeData(position);

        // Si el bioma cambió, regenerar el heightmap
        if (!biomeDataReceived || newBiomeData.biomeName != biomeData.biomeName)
        {
            biomeData = newBiomeData;
            biomeDataReceived = true;

            if (heightMapReceived)
            {
                RegenerateWithNewBiome();
            }
        }
    }

    public void Load()
    {
        // CAMBIO: Pasar datos de bioma al generador
        ThreadedDataRequester.RequestData(() =>
            HeightMapGenerator.GenerateHeightMap(
                MeshSettings.numVertsPerLine,
                MeshSettings.numVertsPerLine,
                heightMapSettings,
                sampleCentre,
                biomeData 
            ),
            OnHeightMapReceived
        );
    }

    void OnHeightMapReceived(object heightMapObject)
    {
        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        ApplyBiomeMaterial();

        if (enableWater && waterGenerator != null)
        {
            waterGenerator.GenerateWaterForMesh();
        }

        UpdateTerrainChunk();
    }

    private void ApplyBiomeMaterial()
    {
        if (biomeDataReceived && !string.IsNullOrEmpty(biomeData.biomeName))
        {
            var biomes = BiomeSystem.GetAllBiomes();
            var currentBiome = biomes.Find(b => b.name == biomeData.biomeName);

            if (currentBiome != null && currentBiome.terrainMaterial != null)
            {
                meshRenderer.material = currentBiome.terrainMaterial;
            }
            else
            {
                // Crear material temporal con color del bioma
                Material tempMaterial = new Material(meshRenderer.material);
                tempMaterial.color = biomeData.biomeColor;
                meshRenderer.material = tempMaterial;
            }
        }
    }

    Vector2 viewerPosition
    {
        get
        {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    public void UpdateTerrainChunk()
    {
        Vector3 currentWorldPos = new Vector3(sampleCentre.x, 0, sampleCentre.y);
        LoadBiomeData(currentWorldPos);

        if (heightMapReceived)
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

            bool wasVisible = IsVisible();
            bool visible = viewerDstFromNearestEdge <= maxViewDst;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;

                        if (enableWater && waterGenerator != null)
                        {
                            waterGenerator.GenerateWaterForMesh();
                        }
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(heightMap, MeshSettings);
                    }
                }
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                if (onVisibilityChanged != null)
                {
                    onVisibilityChanged(this, visible);
                }
            }
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!hasSetCollider)
        {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold)
            {
                if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, MeshSettings);
                }
            }

            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);

        // También actualizar visibilidad del agua
        if (enableWater && waterGenerator != null)
        {
            waterGenerator.SetVisible(visible);
        }
    }

    public bool IsVisible()
    {
        return meshObject.activeSelf;
    }

    // Método para obtener la posición central del chunk en el mundo
    public Vector3 GetWorldPosition()
    {
        return meshObject.transform.position;
    }
}

// Componente para mostrar información del bioma en el editor
public class TerrainChunkBiomeDebug : MonoBehaviour
{
    public TerrainChunk chunk;

    private void OnDrawGizmos()
    {
        if (chunk == null || !chunk.IsVisible())
            return;
#if UNITY_EDITOR
        // Obtener tamaño y posición del chunk
        float meshWorldSize = chunk.MeshSettings.meshWorldSize;
        Vector3 chunkPosition = chunk.GetWorldPosition();

        // Calcular el centro del chunk PRIMERO
        Vector3 center = chunkPosition;

        // Calcular esquinas del chunk basadas en el centro
        Vector3 halfSize = new Vector3(meshWorldSize / 2, 0, meshWorldSize / 2);
        Vector3[] corners = new Vector3[4];
        corners[0] = center - halfSize; // Esquina inferior izquierda
        corners[1] = center + new Vector3(halfSize.x, 0, -halfSize.z); // Esquina inferior derecha
        corners[2] = center + halfSize; // Esquina superior derecha
        corners[3] = center + new Vector3(-halfSize.x, 0, halfSize.z); // Esquina superior izquierda

        // Dibujar contorno del chunk
        UnityEditor.Handles.color = chunk.biomeData.biomeColor;
        for (int i = 0; i < 4; i++)
        {
            UnityEditor.Handles.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        // Dibujar una cruz en el centro para marcar la posición exacta
        float crossSize = meshWorldSize * 0.05f;
        UnityEditor.Handles.DrawLine(
            center + Vector3.left * crossSize,
            center + Vector3.right * crossSize
        );
        UnityEditor.Handles.DrawLine(
            center + Vector3.back * crossSize,
            center + Vector3.forward * crossSize
        );

        // Etiqueta con información del bioma EN EL CENTRO
        string biomeInfo = $"Chunk {chunk.coord}\n" +
                          $"Temp: {chunk.biomeData.temperature:F2}\n" +
                          $"Humidity: {chunk.biomeData.humidity:F2}\n" +
                          $"Bioma: {chunk.biomeData.biomeName}";

        // Posicionar el label exactamente en el centro del chunk
        Vector3 labelPosition = center + Vector3.up * (meshWorldSize * 0.1f);

        UnityEditor.Handles.Label(
            labelPosition,
            biomeInfo,
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.white },
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter // Centrar el texto
            }
        );
#endif
    }
}

class LODMesh
{

    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    int lod;
    public event System.Action updateCallback;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataReceived(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
    }
}