using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ProceduralTerrainGenerator : MonoBehaviour
{
    [System.Serializable]
    public enum TerrainType
    {
        Water,
        Beach,
        Grass,
        Forest,
        Mountain,
        Snow
    }

    [System.Serializable]
    public struct TerrainTypeSettings
    {
        public TerrainType terrainType;
        public Material material;
        [Range(0, 1)]
        public float minHeight;
        [Range(0, 1)]
        public float maxHeight;
        public float moistureThreshold;
        public float temperatureThreshold;
    }

    public Transform player;
    public int viewDistance = 5;
    public int chunkSize = 16;
    public float scale = 20f;
    public int seed = 0;
    public float heightMultiplier = 10f;
    public AnimationCurve heightCurve;
    public TerrainTypeSettings[] terrainSettings;
    public bool useFalloff = false;
    public float worldRadius = 20f;
    public float minPlayerHeight = 1f;

    // Añadido: para suavizado entre chunks
    public float borderBlendDistance = 1f;

    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int currentPlayerChunk;
    private float[,] falloffMap;
    private bool initialized = false;

    #region Initialization
    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player transform not assigned to ProceduralTerrainGenerator!");
            return;
        }

        UnityEngine.Random.InitState(seed);
        GenerateFalloffMap();

        // Inicializar en la posición del jugador
        currentPlayerChunk = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        StartCoroutine(UpdateVisibleChunks());
        StartCoroutine(DelayedPlayerSnap());
    }

    private IEnumerator DelayedPlayerSnap()
    {
        // Esperar a que se generen los chunks iniciales
        yield return new WaitForSeconds(0.5f);
        SnapPlayerToTerrain();
        initialized = true;
    }

    private void GenerateFalloffMap()
    {
        falloffMap = new float[chunkSize + 1, chunkSize + 1];

        for (int y = 0; y <= chunkSize; y++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                float xv = x / (float)chunkSize * 2 - 1;
                float yv = y / (float)chunkSize * 2 - 1;
                float value = Mathf.Max(Mathf.Abs(xv), Mathf.Abs(yv));

                // Curva de falloff más suave
                falloffMap[x, y] = Mathf.Pow(value, 3f) / (Mathf.Pow(value, 3f) + Mathf.Pow(2.2f - 2.2f * value, 3f));
            }
        }
    }
    #endregion

    #region Chunk Management
    private IEnumerator UpdateVisibleChunks()
    {
        // Generar chunks iniciales
        UpdateChunks();

        while (true)
        {
            if (player != null)
            {
                Vector2Int playerChunk = new Vector2Int(
                    Mathf.FloorToInt(player.position.x / chunkSize),
                    Mathf.FloorToInt(player.position.z / chunkSize)
                );

                if (playerChunk != currentPlayerChunk)
                {
                    currentPlayerChunk = playerChunk;
                    UpdateChunks();

                    // Si el jugador está inicializado, mantenerlo en el terreno
                    if (initialized)
                    {
                        SnapPlayerToTerrain();
                    }
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void UpdateChunks()
    {
        List<Vector2Int> chunksToRemove = new List<Vector2Int>(chunks.Keys);
        List<Vector2Int> chunksToCreate = new List<Vector2Int>();

        // Determinar qué chunks crear
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(
                    currentPlayerChunk.x + x,
                    currentPlayerChunk.y + z
                );

                if (!chunks.ContainsKey(chunkCoord))
                {
                    chunksToCreate.Add(chunkCoord);
                }
                else
                {
                    chunksToRemove.Remove(chunkCoord);
                }
            }
        }

        // Primero eliminar chunks fuera de rango
        foreach (Vector2Int chunkCoord in chunksToRemove)
        {
            Destroy(chunks[chunkCoord].gameObject);
            chunks.Remove(chunkCoord);
        }

        // Luego crear nuevos chunks
        foreach (Vector2Int chunkCoord in chunksToCreate)
        {
            CreateChunk(chunkCoord);
        }
    }

    private void CreateChunk(Vector2Int coord)
    {
        GameObject chunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
        Chunk chunk = chunkObject.AddComponent<Chunk>();
        chunk.transform.parent = transform;
        chunks.Add(coord, chunk);

        float[,] heightMap = GenerateHeightMap(coord);
        TerrainType[,] biomeMap = GenerateBiomeMap(heightMap, coord);

        chunk.Initialize(
            heightMap,
            biomeMap,
            terrainSettings,
            chunkSize,
            heightMultiplier,
            coord
        );
    }
    #endregion

    #region Terrain Generation
    // Nuevo método: Obtiene la altura exacta para cualquier coordenada sin truncar
    private float GetExactNoiseHeight(float worldX, float worldZ)
    {
        float noiseValue = SampleNoise(worldX, worldZ);
        return heightCurve.Evaluate(noiseValue);
    }

    private float[,] GenerateHeightMap(Vector2Int coord)
    {
        float[,] heightMap = new float[chunkSize + 1, chunkSize + 1];

        // Calcular distancia al centro del mundo para el falloff global
        float distanceFromCenter = 0;
        if (useFalloff)
        {
            distanceFromCenter = Mathf.Sqrt(coord.x * coord.x + coord.y * coord.y) / worldRadius;
        }

        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Coordenadas absolutas del mundo (con precisión exacta para evitar discontinuidades)
                float worldX = coord.x * chunkSize + x;
                float worldZ = coord.y * chunkSize + z;

                // Usar el mismo algoritmo exacto para todos los puntos
                float noiseValue = GetExactNoiseHeight(worldX, worldZ);

                // Aplicar falloff solo en los bordes del mundo si está habilitado
                if (useFalloff && distanceFromCenter > 0.5f)
                {
                    // Factor de suavizado para el borde del mundo
                    float edgeFactor = Mathf.Clamp01((distanceFromCenter - 0.5f) * 2f);
                    noiseValue = Mathf.Lerp(noiseValue, 0f, edgeFactor);
                }

                heightMap[x, z] = noiseValue;
            }
        }

        return heightMap;
    }

    // Método consistente para generar ruido, usado en todo el mundo
    private float SampleNoise(float x, float z)
    {
        float noiseValue = 0;
        float amplitude = 1;
        float frequency = 1;
        float maxValue = 0;

        // Octavas para añadir detalle
        for (int i = 0; i < 4; i++)
        {
            // Usar coordenadas exactas para evitar errores de precisión en los bordes
            float xCoord = x / scale * frequency + seed;
            float zCoord = z / scale * frequency + seed;

            noiseValue += Mathf.PerlinNoise(xCoord, zCoord) * amplitude;

            maxValue += amplitude;
            amplitude *= 0.5f;
            frequency *= 2;
        }

        return noiseValue / maxValue;
    }

    private TerrainType[,] GenerateBiomeMap(float[,] heightMap, Vector2Int coord)
    {
        TerrainType[,] biomeMap = new TerrainType[chunkSize + 1, chunkSize + 1];

        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Coordenadas absolutas del mundo
                float worldX = coord.x * chunkSize + x;
                float worldZ = coord.y * chunkSize + z;

                // Usar semilla diferente para humedad y temperatura para variedad
                float moisture = Mathf.PerlinNoise(
                    worldX * 0.01f + seed * 0.7f + 100,
                    worldZ * 0.01f + seed * 0.7f + 100
                );

                float temperature = Mathf.PerlinNoise(
                    worldX * 0.005f + seed * 0.3f + 200,
                    worldZ * 0.005f + seed * 0.3f + 200
                );

                float height = heightMap[x, z];
                TerrainType selectedType = TerrainType.Water; // Valor predeterminado

                // Asignar bioma basado en altura, humedad y temperatura
                foreach (TerrainTypeSettings setting in terrainSettings)
                {
                    if (height >= setting.minHeight && height <= setting.maxHeight &&
                        moisture >= setting.moistureThreshold &&
                        temperature >= setting.temperatureThreshold)
                    {
                        selectedType = setting.terrainType;
                        break;
                    }
                }

                biomeMap[x, z] = selectedType;
            }
        }
        return biomeMap;
    }
    #endregion

    #region Player Position
    private void Update()
    {
        // Si el jugador está inicializado, mantenerlo sobre el terreno
        if (initialized && player != null)
        {
            Vector3 playerPos = player.position;
            float height = GetTerrainHeightAtPosition(playerPos) + minPlayerHeight;

            // Solo ajustar si el jugador está por debajo del terreno
            if (playerPos.y < height)
            {
                player.position = new Vector3(playerPos.x, height, playerPos.z);
            }
        }
    }

    private void SnapPlayerToTerrain()
    {
        if (player == null) return;

        Vector3 playerPos = player.position;
        float height = GetTerrainHeightAtPosition(playerPos);
        player.position = new Vector3(playerPos.x, height + minPlayerHeight, playerPos.z);
    }

    public float GetTerrainHeightAtPosition(Vector3 worldPosition)
    {
        // Intentamos primero usar la interpolación en el chunk
        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.z / chunkSize)
        );

        if (chunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            // Obtener la posición local dentro del chunk
            float localX = worldPosition.x - chunkCoord.x * chunkSize;
            float localZ = worldPosition.z - chunkCoord.y * chunkSize;

            // Obtener los índices de los cuatro vértices circundantes
            int x0 = Mathf.FloorToInt(localX);
            int x1 = Mathf.Min(x0 + 1, chunkSize);
            int z0 = Mathf.FloorToInt(localZ);
            int z1 = Mathf.Min(z0 + 1, chunkSize);

            // Asegurarse de que los índices estén dentro de los límites
            x0 = Mathf.Clamp(x0, 0, chunkSize);
            x1 = Mathf.Clamp(x1, 0, chunkSize);
            z0 = Mathf.Clamp(z0, 0, chunkSize);
            z1 = Mathf.Clamp(z1, 0, chunkSize);

            // Obtener las alturas de los cuatro vértices
            float h00 = chunk.heightMap[x0, z0] * heightMultiplier;
            float h01 = chunk.heightMap[x0, z1] * heightMultiplier;
            float h10 = chunk.heightMap[x1, z0] * heightMultiplier;
            float h11 = chunk.heightMap[x1, z1] * heightMultiplier;

            // Calcular los factores de interpolación
            float tx = localX - x0;
            float tz = localZ - z0;

            // Interpolar para obtener la altura exacta
            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(h0, h1, tz);
        }

        // Si no encontramos el chunk, calculamos la altura basada en las coordenadas globales
        // esto garantiza consistencia incluso si el chunk no está cargado
        float worldX = worldPosition.x;
        float worldZ = worldPosition.z;

        float height = GetExactNoiseHeight(worldX, worldZ) * heightMultiplier;

        return height;
    }
    #endregion
}