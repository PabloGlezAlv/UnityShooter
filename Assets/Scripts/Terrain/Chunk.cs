using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [HideInInspector] public float[,] heightMap;
    private ProceduralTerrainGenerator.TerrainType[,] biomeMap;
    private ProceduralTerrainGenerator.TerrainTypeSettings[] terrainSettings;
    private int chunkSize;
    private float heightMultiplier;
    private Vector2Int coord;
    private MeshCollider meshCollider;

    // Dictionary of submeshes for each biome type
    private Dictionary<ProceduralTerrainGenerator.TerrainType, List<int>> biomeTriangles = new Dictionary<ProceduralTerrainGenerator.TerrainType, List<int>>();

    // Almacena los pesos de bioma para cada vértice (x,z) -> [agua, arena, hierba, etc...]
    private Dictionary<Vector2Int, BiomeWeights> vertexBiomeWeights = new Dictionary<Vector2Int, BiomeWeights>();

    // Estructura para almacenar los pesos de bioma
    private class BiomeWeights
    {
        public Dictionary<ProceduralTerrainGenerator.TerrainType, float> weights = new Dictionary<ProceduralTerrainGenerator.TerrainType, float>();

        // Devuelve los pesos normalizados de los principales biomas (como máximo 3 para RGB)
        public Dictionary<ProceduralTerrainGenerator.TerrainType, float> GetTopWeights(int maxBiomes = 3)
        {
            // Ordenar biomas por peso
            var sortedBiomes = weights.OrderByDescending(pair => pair.Value).Take(maxBiomes).ToList();

            // Calcular suma total para normalizar
            float totalWeight = 0f;
            foreach (var pair in sortedBiomes)
            {
                totalWeight += pair.Value;
            }

            // Crear diccionario normalizado de los principales biomas
            Dictionary<ProceduralTerrainGenerator.TerrainType, float> topWeights = new Dictionary<ProceduralTerrainGenerator.TerrainType, float>();

            if (totalWeight > 0)
            {
                foreach (var pair in sortedBiomes)
                {
                    topWeights.Add(pair.Key, pair.Value / totalWeight);
                }
            }
            else if (sortedBiomes.Count > 0)
            {
                // Si no hay pesos, asignar 1.0 al primero
                topWeights.Add(sortedBiomes[0].Key, 1.0f);
            }

            return topWeights;
        }
    }

    public void Initialize(float[,] heightMap, ProceduralTerrainGenerator.TerrainType[,] biomeMap,
        ProceduralTerrainGenerator.TerrainTypeSettings[] terrainSettings, int chunkSize,
        float heightMultiplier, Vector2Int coord)
    {
        this.heightMap = heightMap;
        this.biomeMap = biomeMap;
        this.terrainSettings = terrainSettings;
        this.chunkSize = chunkSize;
        this.heightMultiplier = heightMultiplier;
        this.coord = coord;

        // Initialize triangle dictionary for each biome type
        foreach (ProceduralTerrainGenerator.TerrainTypeSettings setting in terrainSettings)
        {
            if (!biomeTriangles.ContainsKey(setting.terrainType))
            {
                biomeTriangles[setting.terrainType] = new List<int>();
            }
        }

        // Precalcular los pesos de bioma para cada vértice
        CalculateVertexBiomeWeights();

        GenerateMesh();
        PositionChunk();
    }

    private void CalculateVertexBiomeWeights()
    {
        // Distancia de influencia para el cálculo de pesos (en unidades de terreno)
        float influenceRadius = 3.0f;

        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                BiomeWeights weights = new BiomeWeights();
                float normalizedHeight = heightMap[x, z];

                // 1. Asignar pesos basados en la altura y cercanía a umbrales de bioma
                foreach (var setting in terrainSettings)
                {
                    // Calcular distancia al rango de altura del bioma
                    float heightMiddle = (setting.minHeight + setting.maxHeight) * 0.5f;
                    float heightRange = (setting.maxHeight - setting.minHeight) * 0.5f;
                    float heightDistance = Mathf.Abs(normalizedHeight - heightMiddle) / heightRange;

                    // Convertir distancia a peso (más cerca = mayor peso)
                    float heightWeight = Mathf.Clamp01(1.0f - heightDistance);

                    // Aplicar una curva para hacer la transición más suave
                    heightWeight = Mathf.SmoothStep(0, 1, heightWeight);

                    // Almacenar el peso inicial
                    weights.weights[setting.terrainType] = heightWeight;
                }

                // 2. Considerar biomas vecinos para suavizar transiciones
                int searchRadius = Mathf.CeilToInt(influenceRadius);

                for (int nz = -searchRadius; nz <= searchRadius; nz++)
                {
                    for (int nx = -searchRadius; nx <= searchRadius; nx++)
                    {
                        int neighborX = x + nx;
                        int neighborZ = z + nz;

                        // Verificar si el vecino está dentro del chunk
                        if (neighborX >= 0 && neighborX <= chunkSize &&
                            neighborZ >= 0 && neighborZ <= chunkSize)
                        {
                            // Distancia al vecino
                            float distance = Mathf.Sqrt(nx * nx + nz * nz);

                            // Si está dentro del radio de influencia
                            if (distance <= influenceRadius)
                            {
                                // Calcular factor de influencia basado en la distancia
                                float influence = 1.0f - (distance / influenceRadius);
                                influence = Mathf.SmoothStep(0, 1, influence);

                                // Obtener bioma del vecino
                                ProceduralTerrainGenerator.TerrainType neighborBiome = biomeMap[neighborX, neighborZ];

                                // Añadir influencia del bioma vecino
                                if (!weights.weights.ContainsKey(neighborBiome))
                                {
                                    weights.weights[neighborBiome] = 0;
                                }
                                weights.weights[neighborBiome] += influence * 0.5f; // Factor 0.5 para no sobrepasar demasiado
                            }
                        }
                    }
                }

                // Almacenar los pesos calculados para este vértice
                vertexBiomeWeights[new Vector2Int(x, z)] = weights;
            }
        }
    }

    private void GenerateMesh()
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> vertexColors = new List<Color>(); // Para transiciones de bioma suaves

        // Create vertices
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Altura actual
                float height = heightMap[x, z] * heightMultiplier;
                vertices.Add(new Vector3(x, height, z));

                // UVs para texturas
                float uvScale = 0.1f;
                uvs.Add(new Vector2(x * uvScale, z * uvScale));

                // Color de vértice para transición de biomas
                Color vertexColor = CalculateVertexColor(x, z);
                vertexColors.Add(vertexColor);
            }
        }

        // Group triangles by biome type
        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int verticesPerRow = chunkSize + 1;

                int topLeft = z * verticesPerRow + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * verticesPerRow + x;
                int bottomRight = bottomLeft + 1;

                // Determine biome for this quad based on dominant biome among the four corners
                ProceduralTerrainGenerator.TerrainType dominantBiome = GetDominantBiomeForQuad(x, z);

                // First triangle (upper-left)
                biomeTriangles[dominantBiome].Add(topLeft);
                biomeTriangles[dominantBiome].Add(bottomLeft);
                biomeTriangles[dominantBiome].Add(topRight);

                // Second triangle (lower-right)
                biomeTriangles[dominantBiome].Add(topRight);
                biomeTriangles[dominantBiome].Add(bottomLeft);
                biomeTriangles[dominantBiome].Add(bottomRight);
            }
        }

        // Assign vertices, UVs and colors
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = vertexColors.ToArray();

        // Count required submeshes (only those with triangles)
        int submeshCount = 0;
        foreach (var triangleList in biomeTriangles.Values)
        {
            if (triangleList.Count > 0)
            {
                submeshCount++;
            }
        }

        // Configure submeshes
        mesh.subMeshCount = submeshCount;

        // List to store materials in same order as submeshes
        List<Material> materialsList = new List<Material>();

        // Index for submesh control
        int submeshIndex = 0;

        // Assign triangles to each submesh
        foreach (ProceduralTerrainGenerator.TerrainType biomeType in biomeTriangles.Keys)
        {
            List<int> triangles = biomeTriangles[biomeType];
            if (triangles.Count > 0)
            {
                mesh.SetTriangles(triangles.ToArray(), submeshIndex);

                // Find material for this biome
                Material biomeMaterial = null;
                foreach (ProceduralTerrainGenerator.TerrainTypeSettings setting in terrainSettings)
                {
                    if (setting.terrainType == biomeType)
                    {
                        biomeMaterial = setting.material;
                        break;
                    }
                }

                // If no material, use default
                if (biomeMaterial == null)
                {
                    biomeMaterial = new Material(Shader.Find("Standard"));
                    biomeMaterial.color = Color.magenta; // Error color for identification
                }

                // Configure material for vertex color blending
                SetupMaterialForBlending(biomeMaterial, biomeType);

                materialsList.Add(biomeMaterial);
                submeshIndex++;
            }
        }

        // Recalculate normals and other mesh data
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        // Assign mesh and materials
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshRenderer.sharedMaterials = materialsList.ToArray();
    }

    private ProceduralTerrainGenerator.TerrainType GetDominantBiomeForQuad(int x, int z)
    {
        // Conteo de biomas para los cuatro vértices del quad
        Dictionary<ProceduralTerrainGenerator.TerrainType, float> combinedWeights =
            new Dictionary<ProceduralTerrainGenerator.TerrainType, float>();

        // Sumar los pesos de los biomas en cada esquina
        AddCornerBiomeWeights(combinedWeights, x, z);           // Esquina superior izquierda
        AddCornerBiomeWeights(combinedWeights, x + 1, z);       // Esquina superior derecha
        AddCornerBiomeWeights(combinedWeights, x, z + 1);       // Esquina inferior izquierda
        AddCornerBiomeWeights(combinedWeights, x + 1, z + 1);   // Esquina inferior derecha

        // Encontrar el bioma con mayor peso total
        ProceduralTerrainGenerator.TerrainType dominantBiome = biomeMap[x, z]; // Default
        float maxWeight = 0;

        foreach (var pair in combinedWeights)
        {
            if (pair.Value > maxWeight)
            {
                maxWeight = pair.Value;
                dominantBiome = pair.Key;
            }
        }

        return dominantBiome;
    }

    private void AddCornerBiomeWeights(Dictionary<ProceduralTerrainGenerator.TerrainType, float> combinedWeights, int x, int z)
    {
        if (x < 0 || x > chunkSize || z < 0 || z > chunkSize)
            return;

        Vector2Int vertexKey = new Vector2Int(x, z);
        if (vertexBiomeWeights.TryGetValue(vertexKey, out BiomeWeights vertexWeights))
        {
            foreach (var pair in vertexWeights.weights)
            {
                if (!combinedWeights.ContainsKey(pair.Key))
                {
                    combinedWeights[pair.Key] = 0;
                }
                combinedWeights[pair.Key] += pair.Value;
            }
        }
    }

    private Color CalculateVertexColor(int x, int z)
    {
        Vector2Int vertexKey = new Vector2Int(x, z);

        if (vertexBiomeWeights.TryGetValue(vertexKey, out BiomeWeights weights))
        {
            // Obtener los 3 biomas principales (para canales RGB)
            var topBiomes = weights.GetTopWeights(3);

            // Asignar cada bioma a un canal de color específico
            // Esto podría mejorarse con un mapeo más elaborado si tienes más de 3 biomas
            Color vertexColor = new Color(0, 0, 0, 1);

            foreach (var pair in topBiomes)
            {
                switch (pair.Key)
                {
                    case ProceduralTerrainGenerator.TerrainType.Water:
                        vertexColor.b = pair.Value; // Agua -> azul
                        break;
                    case ProceduralTerrainGenerator.TerrainType.Beach:
                        vertexColor.r = pair.Value * 0.7f;
                        vertexColor.g = pair.Value * 0.7f;
                        break;
                    case ProceduralTerrainGenerator.TerrainType.Grass:
                        vertexColor.g = pair.Value; // Hierba -> verde
                        break;
                    case ProceduralTerrainGenerator.TerrainType.Forest:
                        vertexColor.g = pair.Value * 0.7f;
                        break;
                    case ProceduralTerrainGenerator.TerrainType.Mountain:
                        vertexColor.r = pair.Value * 0.5f;
                        vertexColor.g = pair.Value * 0.5f;
                        vertexColor.b = pair.Value * 0.5f;
                        break;
                    case ProceduralTerrainGenerator.TerrainType.Snow:
                        vertexColor.r = pair.Value;
                        vertexColor.g = pair.Value;
                        vertexColor.b = pair.Value;
                        break;
                }
            }

            return vertexColor;
        }

        // Fallback a colores de bioma básicos
        return GetBiomeColor(biomeMap[Mathf.Clamp(x, 0, chunkSize), Mathf.Clamp(z, 0, chunkSize)]);
    }

    private Color GetBiomeColor(ProceduralTerrainGenerator.TerrainType biomeType)
    {
        // Colores básicos para cada tipo de bioma
        switch (biomeType)
        {
            case ProceduralTerrainGenerator.TerrainType.Water:
                return new Color(0, 0, 1, 1); // Azul
            case ProceduralTerrainGenerator.TerrainType.Beach:
                return new Color(0.76f, 0.7f, 0.5f, 1); // Arena
            case ProceduralTerrainGenerator.TerrainType.Grass:
                return new Color(0, 0.8f, 0, 1); // Verde
            case ProceduralTerrainGenerator.TerrainType.Forest:
                return new Color(0, 0.5f, 0, 1); // Verde oscuro
            case ProceduralTerrainGenerator.TerrainType.Mountain:
                return new Color(0.5f, 0.5f, 0.5f, 1); // Gris
            case ProceduralTerrainGenerator.TerrainType.Snow:
                return new Color(1, 1, 1, 1); // Blanco
            default:
                return Color.magenta; // Color de error
        }
    }

    private void SetupMaterialForBlending(Material material, ProceduralTerrainGenerator.TerrainType biomeType)
    {
        // Asegurarse de que el shader tiene soporte para vertex colors
        if (material.shader.name != "Custom/TerrainBlendShader")
        {
            Material newMaterial = new Material(Shader.Find("Custom/TerrainBlendShader"));
            if (newMaterial != null)
            {
                // Copiar propiedades relevantes
                newMaterial.CopyPropertiesFromMaterial(material);
                material = newMaterial;
            }
        }

        // Activar el uso de colores de vértice en el shader
        material.EnableKeyword("_VERTEXCOLOR_ON");
        material.SetFloat("_UseVertexColor", 1.0f);

        // Configurar parámetros de blending específicos para cada bioma
        switch (biomeType)
        {
            case ProceduralTerrainGenerator.TerrainType.Water:
                material.SetFloat("_TextureScale", 20.0f);
                material.SetFloat("_BlendSharpness", 4.0f);
                material.SetFloat("_NormalInfluence", 0.4f);
                material.SetFloat("_Glossiness", 0.9f);
                break;
            case ProceduralTerrainGenerator.TerrainType.Beach:
                material.SetFloat("_TextureScale", 15.0f);
                material.SetFloat("_BlendSharpness", 6.0f);
                material.SetFloat("_NormalInfluence", 0.1f);
                break;
            case ProceduralTerrainGenerator.TerrainType.Grass:
                material.SetFloat("_TextureScale", 10.0f);
                material.SetFloat("_BlendSharpness", 8.0f);
                material.SetFloat("_NormalInfluence", 0.2f);
                break;
            case ProceduralTerrainGenerator.TerrainType.Forest:
                material.SetFloat("_TextureScale", 12.0f);
                material.SetFloat("_BlendSharpness", 5.0f);
                material.SetFloat("_NormalInfluence", 0.3f);
                break;
            case ProceduralTerrainGenerator.TerrainType.Mountain:
                material.SetFloat("_TextureScale", 8.0f);
                material.SetFloat("_BlendSharpness", 10.0f);
                material.SetFloat("_NormalInfluence", 0.5f);
                break;
            case ProceduralTerrainGenerator.TerrainType.Snow:
                material.SetFloat("_TextureScale", 5.0f);
                material.SetFloat("_BlendSharpness", 3.0f);
                material.SetFloat("_NormalInfluence", 0.15f);
                material.SetFloat("_Glossiness", 0.7f);
                break;
        }
    }

    private void PositionChunk()
    {
        // Posicionar el chunk en el mundo según sus coordenadas
        transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
    }

    // Método para actualizar el mesh cuando cambian los datos de terreno
    public void UpdateMesh()
    {
        // Limpiar los triángulos existentes
        foreach (var key in biomeTriangles.Keys)
        {
            biomeTriangles[key].Clear();
        }

        // Volver a calcular los pesos de bioma
        vertexBiomeWeights.Clear();
        CalculateVertexBiomeWeights();

        // Regenerar el mesh
        GenerateMesh();
    }

    // Método para obtener la altura del terreno en una posición
    public float GetHeightAtPosition(Vector3 worldPosition)
    {
        // Convertir la posición mundial a coordenadas locales del chunk
        Vector3 localPos = worldPosition - transform.position;
        int x = Mathf.FloorToInt(localPos.x);
        int z = Mathf.FloorToInt(localPos.z);

        // Verificar si está dentro de los límites
        if (x < 0 || x >= chunkSize || z < 0 || z >= chunkSize)
            return 0f;

        // Interpolar para obtener una altura suave
        float fracX = localPos.x - x;
        float fracZ = localPos.z - z;

        // Obtener las alturas de las cuatro esquinas
        float h1 = heightMap[x, z] * heightMultiplier;
        float h2 = heightMap[Mathf.Min(x + 1, chunkSize), z] * heightMultiplier;
        float h3 = heightMap[x, Mathf.Min(z + 1, chunkSize)] * heightMultiplier;
        float h4 = heightMap[Mathf.Min(x + 1, chunkSize), Mathf.Min(z + 1, chunkSize)] * heightMultiplier;

        // Interpolación bilineal
        float height = Mathf.Lerp(
            Mathf.Lerp(h1, h2, fracX),
            Mathf.Lerp(h3, h4, fracX),
            fracZ
        );

        return height;
    }

    // Método para obtener el tipo de bioma en una posición
    public ProceduralTerrainGenerator.TerrainType GetBiomeAtPosition(Vector3 worldPosition)
    {
        // Convertir la posición mundial a coordenadas locales del chunk
        Vector3 localPos = worldPosition - transform.position;
        int x = Mathf.FloorToInt(localPos.x);
        int z = Mathf.FloorToInt(localPos.z);

        // Verificar si está dentro de los límites
        if (x < 0 || x >= chunkSize || z < 0 || z >= chunkSize)
            return ProceduralTerrainGenerator.TerrainType.Grass; // Valor por defecto

        return biomeMap[x, z];
    }

    // Método para depuración visual de biomas
    public void DebugDrawBiomeBoundaries()
    {
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Solo dibuja puntos donde hay cambio de bioma
                if (x < chunkSize && z < chunkSize)
                {
                    ProceduralTerrainGenerator.TerrainType current = biomeMap[x, z];

                    // Comprobar vecinos
                    bool differentNeighbor = false;

                    if (x > 0 && biomeMap[x - 1, z] != current)
                        differentNeighbor = true;
                    else if (x < chunkSize - 1 && biomeMap[x + 1, z] != current)
                        differentNeighbor = true;
                    else if (z > 0 && biomeMap[x, z - 1] != current)
                        differentNeighbor = true;
                    else if (z < chunkSize - 1 && biomeMap[x, z + 1] != current)
                        differentNeighbor = true;

                    if (differentNeighbor)
                    {
                        Vector3 worldPos = transform.position + new Vector3(x, heightMap[x, z] * heightMultiplier + 0.5f, z);
                        Color color = GetBiomeColor(current);
                        Debug.DrawLine(worldPos, worldPos + Vector3.up * 5, color, 5.0f);
                    }
                }
            }
        }
    }
}
