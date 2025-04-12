using System.Collections.Generic;
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

    // Lista de submeshes para cada tipo de bioma
    private Dictionary<ProceduralTerrainGenerator.TerrainType, List<int>> biomeTriangles = new Dictionary<ProceduralTerrainGenerator.TerrainType, List<int>>();

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

        // Inicializar diccionario de triángulos para cada tipo de bioma
        foreach (ProceduralTerrainGenerator.TerrainTypeSettings setting in terrainSettings)
        {
            if (!biomeTriangles.ContainsKey(setting.terrainType))
            {
                biomeTriangles[setting.terrainType] = new List<int>();
            }
        }

        GenerateMesh();
        PositionChunk();
    }

    private void GenerateMesh()
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> vertexColors = new List<Color>(); // Para transiciones suaves entre biomas

        // Crear vértices
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Usar el valor exacto de altura para cada vértice
                float height = heightMap[x, z] * heightMultiplier;
                vertices.Add(new Vector3(x, height, z));

                // UVs para tiling de texturas - ajustadas para evitar deformaciones
                float uvScale = 0.1f;
                uvs.Add(new Vector2(x * uvScale, z * uvScale));

                // Color vértice basado en bioma - para hacer blending
                ProceduralTerrainGenerator.TerrainType biomeType = biomeMap[x, z];
                Color biomeColor = GetBiomeColor(biomeType);
                vertexColors.Add(biomeColor);
            }
        }

        // Agrupar triángulos por tipo de bioma
        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int verticesPerRow = chunkSize + 1;

                int topLeft = z * verticesPerRow + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * verticesPerRow + x;
                int bottomRight = bottomLeft + 1;

                // Determinar el bioma para este cuadrado basado en la esquina superior izquierda
                ProceduralTerrainGenerator.TerrainType biomeType = biomeMap[x, z];

                // Primer triángulo (superior izquierdo)
                biomeTriangles[biomeType].Add(topLeft);
                biomeTriangles[biomeType].Add(bottomLeft);
                biomeTriangles[biomeType].Add(topRight);

                // Segundo triángulo (inferior derecho)
                biomeTriangles[biomeType].Add(topRight);
                biomeTriangles[biomeType].Add(bottomLeft);
                biomeTriangles[biomeType].Add(bottomRight);
            }
        }

        // Asignar vértices, UVs y colores
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = vertexColors.ToArray();

        // Contar submeshes necesarios (solo los que tienen triángulos)
        int submeshCount = 0;
        foreach (var triangleList in biomeTriangles.Values)
        {
            if (triangleList.Count > 0)
            {
                submeshCount++;
            }
        }

        // Configurar submeshes
        mesh.subMeshCount = submeshCount;

        // Lista para almacenar materiales en el mismo orden que los submeshes
        List<Material> materialsList = new List<Material>();

        // Índice para control de submesh
        int submeshIndex = 0;

        // Asignar triángulos a cada submesh
        foreach (ProceduralTerrainGenerator.TerrainType biomeType in biomeTriangles.Keys)
        {
            List<int> triangles = biomeTriangles[biomeType];
            if (triangles.Count > 0)
            {
                mesh.SetTriangles(triangles.ToArray(), submeshIndex);

                // Buscar material para este bioma
                Material biomeMaterial = null;
                foreach (ProceduralTerrainGenerator.TerrainTypeSettings setting in terrainSettings)
                {
                    if (setting.terrainType == biomeType)
                    {
                        biomeMaterial = setting.material;
                        break;
                    }
                }

                // Si no hay material, usar uno por defecto
                if (biomeMaterial == null)
                {
                    biomeMaterial = new Material(Shader.Find("Standard"));
                    biomeMaterial.color = Color.magenta; // Color de error para identificarlo
                }

                // Configurar material para usar vertex colors
                SetupMaterialForBlending(biomeMaterial);

                materialsList.Add(biomeMaterial);
                submeshIndex++;
            }
        }

        // Recalcular normales y tangentes
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        // Asignar mesh y materiales
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshRenderer.sharedMaterials = materialsList.ToArray();
    }

    private void SetupMaterialForBlending(Material material)
    {
        // Habilitar vertex color en el material si el shader lo soporta
        if (material.HasProperty("_Color"))
        {
            // La mayoría de shaders estándar ya manejan vertex colors adecuadamente
            material.EnableKeyword("_VERTEXCOLOR_ON");

            // Si usas el shader Standard, puedes mezclarlo así
            if (material.shader.name.Contains("Standard"))
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
        }
    }

    private Color GetBiomeColor(ProceduralTerrainGenerator.TerrainType biomeType)
    {
        // Colores de referencia para cada tipo de bioma
        // Esto ayuda a suavizar las transiciones en los bordes
        switch (biomeType)
        {
            case ProceduralTerrainGenerator.TerrainType.Water:
                return new Color(0.0f, 0.2f, 0.8f, 1f);
            case ProceduralTerrainGenerator.TerrainType.Beach:
                return new Color(0.95f, 0.95f, 0.7f, 1f);
            case ProceduralTerrainGenerator.TerrainType.Grass:
                return new Color(0.2f, 0.8f, 0.2f, 1f);
            case ProceduralTerrainGenerator.TerrainType.Forest:
                return new Color(0.05f, 0.5f, 0.05f, 1f);
            case ProceduralTerrainGenerator.TerrainType.Mountain:
                return new Color(0.5f, 0.5f, 0.5f, 1f);
            case ProceduralTerrainGenerator.TerrainType.Snow:
                return new Color(0.95f, 0.95f, 0.95f, 1f);
            default:
                return Color.white;
        }
    }

    private void PositionChunk()
    {
        transform.position = new Vector3(
            coord.x * chunkSize,
            0,
            coord.y * chunkSize
        );
    }
}