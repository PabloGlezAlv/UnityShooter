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

    private enum Direction
    {
        North,
        East,
        South,
        West,
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
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

    public bool useSmoothedBiomes = true;
    public float biomeFadeDistance = 0.05f;

    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int currentPlayerChunk;
    private float[,] falloffMap;
    private bool initialized = false;

    // Cache for vertex heights to ensure consistent values
    private Dictionary<Vector3Int, float> vertexHeightCache = new Dictionary<Vector3Int, float>();

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

        // Initialize at the player's position
        currentPlayerChunk = new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );

        StartCoroutine(UpdateVisibleChunks());
        StartCoroutine(DelayedPlayerSnap());
    }

    private IEnumerator DelayedPlayerSnap()
    {
        // Wait for initial chunks to generate
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

                // Smoother falloff curve
                falloffMap[x, y] = Mathf.Pow(value, 3f) / (Mathf.Pow(value, 3f) + Mathf.Pow(2.2f - 2.2f * value, 3f));
            }
        }
    }
    #endregion

    #region Chunk Management
    private IEnumerator UpdateVisibleChunks()
    {
        // Generate initial chunks
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

                    // Keep player on terrain if initialized
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

        // Determine which chunks to create
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

        // First remove chunks out of range
        foreach (Vector2Int chunkCoord in chunksToRemove)
        {
            Destroy(chunks[chunkCoord].gameObject);
            chunks.Remove(chunkCoord);
        }

        // Then create new chunks
        foreach (Vector2Int chunkCoord in chunksToCreate)
        {
            CreateChunkWithSharedEdges(chunkCoord);
        }
    }

    private void CreateChunkWithSharedEdges(Vector2Int coord)
    {
        GameObject chunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
        Chunk chunk = chunkObject.AddComponent<Chunk>();
        chunk.transform.parent = transform;
        chunks.Add(coord, chunk);

        // Create heightmap with shared edge data
        float[,] heightMap = GenerateHeightMapWithSharedEdges(coord);

        // Generate biome map
        TerrainType[,] biomeMap = GenerateBiomeMap(heightMap, coord);

        // Initialize the chunk
        chunk.Initialize(
            heightMap,
            biomeMap,
            terrainSettings,
            chunkSize,
            heightMultiplier,
            coord
        );
    }

    private float[,] GenerateHeightMapWithSharedEdges(Vector2Int coord)
    {
        float[,] heightMap = new float[chunkSize + 1, chunkSize + 1];

        // Obtener chunks vecinos
        Dictionary<Direction, Chunk> neighbors = GetNeighboringChunks(coord);

        // Llenar heightmap con datos de bordes compartidos o generar desde cero
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Crear ID de vértice en coordenadas mundiales para valores de altura consistentes
                Vector3Int worldVertexID = new Vector3Int(
                    coord.x * chunkSize + x,
                    0,
                    coord.y * chunkSize + z
                );

                // Comprobar si este vértice está en algún borde
                bool isWestEdge = x == 0;
                bool isEastEdge = x == chunkSize;
                bool isSouthEdge = z == 0;
                bool isNorthEdge = z == chunkSize;

                // Comprobar si es un vértice de esquina
                bool isCorner = (isWestEdge || isEastEdge) && (isNorthEdge || isSouthEdge);

                // Primero, comprobar si podemos reutilizar este vértice de un vecino
                float height = 0;
                bool heightFound = false;

                // Comprobar vecino oeste para borde compartido
                if (isWestEdge && neighbors.TryGetValue(Direction.West, out Chunk westNeighbor))
                {
                    height = westNeighbor.heightMap[chunkSize, z];
                    heightFound = true;
                }
                // Comprobar vecino este para borde compartido
                else if (isEastEdge && neighbors.TryGetValue(Direction.East, out Chunk eastNeighbor))
                {
                    height = eastNeighbor.heightMap[0, z];
                    heightFound = true;
                }
                // Comprobar vecino sur para borde compartido
                else if (isSouthEdge && neighbors.TryGetValue(Direction.South, out Chunk southNeighbor))
                {
                    height = southNeighbor.heightMap[x, chunkSize];
                    heightFound = true;
                }
                // Comprobar vecino norte para borde compartido
                else if (isNorthEdge && neighbors.TryGetValue(Direction.North, out Chunk northNeighbor))
                {
                    height = northNeighbor.heightMap[x, 0];
                    heightFound = true;
                }

                // Manejar esquinas comprobando vecinos diagonales si es necesario
                if (isCorner && !heightFound)
                {
                    if (isWestEdge && isNorthEdge && neighbors.TryGetValue(Direction.NorthWest, out Chunk nwNeighbor))
                    {
                        height = nwNeighbor.heightMap[chunkSize, 0];
                        heightFound = true;
                    }
                    else if (isEastEdge && isNorthEdge && neighbors.TryGetValue(Direction.NorthEast, out Chunk neNeighbor))
                    {
                        height = neNeighbor.heightMap[0, 0];
                        heightFound = true;
                    }
                    else if (isWestEdge && isSouthEdge && neighbors.TryGetValue(Direction.SouthWest, out Chunk swNeighbor))
                    {
                        height = swNeighbor.heightMap[chunkSize, chunkSize];
                        heightFound = true;
                    }
                    else if (isEastEdge && isSouthEdge && neighbors.TryGetValue(Direction.SouthEast, out Chunk seNeighbor))
                    {
                        height = seNeighbor.heightMap[0, chunkSize];
                        heightFound = true;
                    }
                }

                // Si no encontramos la altura de los vecinos, usar GetCachedHeight
                if (!heightFound)
                {
                    // Usar nuestro método mejorado de obtención de alturas
                    height = GetCachedHeight(worldVertexID);
                }

                // Establecer la altura en nuestro heightmap
                heightMap[x, z] = height;

                // Aplicar un suavizado adicional a los bordes para mejorar transiciones
                if ((isWestEdge || isEastEdge || isNorthEdge || isSouthEdge) && heightFound)
                {
                    // Calcular la altura teórica que tendría este punto según nuestro ruido
                    float theoreticalHeight = GetCachedHeight(worldVertexID);

                    // En los bordes externos del mundo (sin chunks vecinos), no mezclar
                    bool isWorldEdge = (isWestEdge && !neighbors.ContainsKey(Direction.West)) ||
                                     (isEastEdge && !neighbors.ContainsKey(Direction.East)) ||
                                     (isSouthEdge && !neighbors.ContainsKey(Direction.South)) ||
                                     (isNorthEdge && !neighbors.ContainsKey(Direction.North));

                    if (!isWorldEdge)
                    {
                        // Calcular distancia desde el borde (para suavizado gradual)
                        float edgeBlendFactor = 0.5f;

                        // Para bordes internos (con chunks vecinos), mezclar suavemente
                        if (isWestEdge && x == 0)
                        {
                            // Suavizar usando valores teóricos y reales
                            height = Mathf.Lerp(height, theoreticalHeight, edgeBlendFactor);
                        }
                        else if (isEastEdge && x == chunkSize)
                        {
                            height = Mathf.Lerp(height, theoreticalHeight, edgeBlendFactor);
                        }
                        else if (isSouthEdge && z == 0)
                        {
                            height = Mathf.Lerp(height, theoreticalHeight, edgeBlendFactor);
                        }
                        else if (isNorthEdge && z == chunkSize)
                        {
                            height = Mathf.Lerp(height, theoreticalHeight, edgeBlendFactor);
                        }
                    }

                    // Actualizar la altura final en el mapa
                    heightMap[x, z] = height;
                }
            }
        }

        // Aplicar suavizado adicional en los bordes internos del chunk
        SmoothInternalEdges(heightMap);

        return heightMap;
    }

    // Método adicional para suavizar los bordes internos del chunk
    private void SmoothInternalEdges(float[,] heightMap)
    {
        // Crear una copia del mapa original para no afectar los cálculos
        float[,] originalMap = new float[chunkSize + 1, chunkSize + 1];
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                originalMap[x, z] = heightMap[x, z];
            }
        }

        // Cantidad de celdas cerca del borde a suavizar
        int borderSize = 3;

        // Suavizar solo las áreas cercanas a los bordes
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Calcular distancia al borde más cercano
                int distanceToEdge = Mathf.Min(
                    Mathf.Min(x, chunkSize - x),
                    Mathf.Min(z, chunkSize - z)
                );

                // Solo suavizar si estamos cerca del borde pero no exactamente en él
                if (distanceToEdge < borderSize && distanceToEdge > 0)
                {
                    // Factor de suavizado basado en la distancia al borde
                    float smoothFactor = 1.0f - (float)distanceToEdge / borderSize;

                    // Crear promedio de vecinos
                    float sum = 0;
                    int count = 0;

                    // Considerar celdas vecinas
                    for (int nz = -1; nz <= 1; nz++)
                    {
                        for (int nx = -1; nx <= 1; nx++)
                        {
                            int checkX = x + nx;
                            int checkZ = z + nz;

                            if (checkX >= 0 && checkX <= chunkSize &&
                                checkZ >= 0 && checkZ <= chunkSize)
                            {
                                sum += originalMap[checkX, checkZ];
                                count++;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        float average = sum / count;
                        // Mezclar original con promedio basado en el factor de suavizado
                        heightMap[x, z] = Mathf.Lerp(originalMap[x, z], average, smoothFactor * 0.3f);
                    }
                }
            }
        }
    }

    // El método GetCachedHeight mencionado anteriormente
    private float GetCachedHeight(Vector3Int worldVertex)
    {
        if (vertexHeightCache.TryGetValue(worldVertex, out float cachedHeight))
        {
            return cachedHeight;
        }

        // Generar nueva altura
        float worldX = worldVertex.x;
        float worldZ = worldVertex.z;

        // Aplicar ruido con posible suavizado adicional en bordes
        float noiseValue = SampleSmoothNoise(worldX, worldZ);

        // Aplicar falloff si está habilitado
        if (useFalloff)
        {
            Vector2Int chunkCoord = new Vector2Int(
                Mathf.FloorToInt(worldX / chunkSize),
                Mathf.FloorToInt(worldZ / chunkSize)
            );

            float distanceFromCenter = Mathf.Sqrt(
                chunkCoord.x * chunkCoord.x + chunkCoord.y * chunkCoord.y
            ) / worldRadius;

            if (distanceFromCenter > 0.5f)
            {
                float edgeFactor = Mathf.Clamp01((distanceFromCenter - 0.5f) * 2f);
                noiseValue = Mathf.Lerp(noiseValue, 0f, edgeFactor);
            }
        }

        float height = heightCurve.Evaluate(noiseValue);

        // Cachear para uso futuro
        vertexHeightCache[worldVertex] = height;
        return height;
    }

    // Método para muestreo de ruido con suavizado adicional
    private float SampleSmoothNoise(float x, float z)
    {
        float noiseValue = SampleNoise(x, z);

        // Aplicar suavizado adicional en coordenadas divisibles por chunkSize
        // (estas corresponden a los bordes de chunks)
        bool isOnChunkBoundaryX = Mathf.RoundToInt(x) % chunkSize == 0;
        bool isOnChunkBoundaryZ = Mathf.RoundToInt(z) % chunkSize == 0;

        if (isOnChunkBoundaryX || isOnChunkBoundaryZ)
        {
            // Muestrear puntos vecinos adicionales para mejor interpolación
            float noise2 = SampleNoise(x + 0.5f, z);
            float noise3 = SampleNoise(x, z + 0.5f);
            float noise4 = SampleNoise(x - 0.5f, z);
            float noise5 = SampleNoise(x, z - 0.5f);

            // Promediar con más peso en el valor central
            noiseValue = (noiseValue * 0.6f) + (noise2 + noise3 + noise4 + noise5) * 0.1f;
        }

        return noiseValue;
    }

    private Dictionary<Direction, Chunk> GetNeighboringChunks(Vector2Int coord)
    {
        Dictionary<Direction, Chunk> neighbors = new Dictionary<Direction, Chunk>();

        // Check all eight directions (four cardinal + four diagonal)
        if (chunks.TryGetValue(new Vector2Int(coord.x - 1, coord.y), out Chunk west))
            neighbors.Add(Direction.West, west);

        if (chunks.TryGetValue(new Vector2Int(coord.x + 1, coord.y), out Chunk east))
            neighbors.Add(Direction.East, east);

        if (chunks.TryGetValue(new Vector2Int(coord.x, coord.y - 1), out Chunk south))
            neighbors.Add(Direction.South, south);

        if (chunks.TryGetValue(new Vector2Int(coord.x, coord.y + 1), out Chunk north))
            neighbors.Add(Direction.North, north);

        // Diagonal neighbors for corner vertices
        if (chunks.TryGetValue(new Vector2Int(coord.x - 1, coord.y + 1), out Chunk northWest))
            neighbors.Add(Direction.NorthWest, northWest);

        if (chunks.TryGetValue(new Vector2Int(coord.x + 1, coord.y + 1), out Chunk northEast))
            neighbors.Add(Direction.NorthEast, northEast);

        if (chunks.TryGetValue(new Vector2Int(coord.x - 1, coord.y - 1), out Chunk southWest))
            neighbors.Add(Direction.SouthWest, southWest);

        if (chunks.TryGetValue(new Vector2Int(coord.x + 1, coord.y - 1), out Chunk southEast))
            neighbors.Add(Direction.SouthEast, southEast);

        return neighbors;
    }
    #endregion

    #region Terrain Generation
    private float SampleNoise(float x, float z)
    {
        // Implementación actual usando Perlin Noise con octavas
        // Vamos a usar Simplex Noise para mayor suavidad en las transiciones

        float noiseValue = 0;
        float amplitude = 1;
        float frequency = 1;
        float maxValue = 0;

        for (int i = 0; i < 4; i++)
        {
            float xCoord = x / scale * frequency + seed;
            float zCoord = z / scale * frequency + seed;

            // Usar función SimplexNoise en lugar de PerlinNoise para mayor suavidad
            // (Si no tienes una implementación de SimplexNoise, puedes usar esta versión mejorada de Perlin)
            float octaveValue = (Mathf.PerlinNoise(xCoord, zCoord) * 2 - 1);
            noiseValue += octaveValue * amplitude;

            maxValue += amplitude;
            amplitude *= 0.5f;
            frequency *= 2.2f; // Usar un valor no entero para evitar patrones repetitivos
        }

        return Mathf.Clamp01(noiseValue / maxValue * 0.5f + 0.5f); // Normalizar a rango [0,1]
    }

    private TerrainType[,] GenerateBiomeMap(float[,] heightMap, Vector2Int coord)
    {
        TerrainType[,] biomeMap = new TerrainType[chunkSize + 1, chunkSize + 1];

        // Caché para valores de ruido de bioma por coordenada mundial
        Dictionary<Vector2Int, Vector2> biomeCacheDict = new Dictionary<Vector2Int, Vector2>();

        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Coordenadas mundiales
                int worldX = coord.x * chunkSize + x;
                int worldZ = coord.y * chunkSize + z;
                Vector2Int worldCoord = new Vector2Int(worldX, worldZ);

                // Valores de humedad y temperatura
                Vector2 climateData;

                // Verificar caché primero para consistencia entre chunks
                if (biomeCacheDict.TryGetValue(worldCoord, out climateData))
                {
                    // Usar valores cacheados
                }
                else
                {
                    // Usar diferentes semillas para humedad y temperatura
                    float moisture = Mathf.PerlinNoise(
                        worldX * 0.01f + seed * 0.7f + 100,
                        worldZ * 0.01f + seed * 0.7f + 100
                    );

                    float temperature = Mathf.PerlinNoise(
                        worldX * 0.005f + seed * 0.3f + 200,
                        worldZ * 0.005f + seed * 0.3f + 200
                    );

                    climateData = new Vector2(moisture, temperature);
                    biomeCacheDict[worldCoord] = climateData;
                }

                float height = heightMap[x, z];
                TerrainType selectedType = TerrainType.Water; // Valor por defecto

                // Sistema más robusto de selección de biomas
                float bestMatchScore = float.MinValue;

                foreach (TerrainTypeSettings setting in terrainSettings)
                {
                    // Calcular puntuación basada en qué tan bien encaja con todos los parámetros
                    float heightFit = 1.0f - Mathf.Clamp01(
                        Mathf.Abs(height - (setting.minHeight + setting.maxHeight) / 2) * 2 /
                        (setting.maxHeight - setting.minHeight)
                    );

                    float moistureFit = 1.0f - Mathf.Clamp01(
                        Mathf.Abs(climateData.x - setting.moistureThreshold) * 5
                    );

                    float temperatureFit = 1.0f - Mathf.Clamp01(
                        Mathf.Abs(climateData.y - setting.temperatureThreshold) * 5
                    );

                    // Ponderación de factores (puedes ajustar estos valores)
                    float totalScore = heightFit * 0.6f + moistureFit * 0.2f + temperatureFit * 0.2f;

                    // Si encaja mejor que anteriores, seleccionar este bioma
                    if (totalScore > bestMatchScore)
                    {
                        bestMatchScore = totalScore;
                        selectedType = setting.terrainType;
                    }
                }

                biomeMap[x, z] = selectedType;
            }
        }

        // Si se activan biomas suavizados, aplicar filtro de suavizado
        if (useSmoothedBiomes)
        {
            SmoothBiomeMap(biomeMap);
        }

        return biomeMap;
    }

    // Método nuevo para suavizar biomas en bordes
    private void SmoothBiomeMap(TerrainType[,] biomeMap)
    {
        TerrainType[,] originalMap = new TerrainType[chunkSize + 1, chunkSize + 1];

        // Copiar mapa original
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                originalMap[x, z] = biomeMap[x, z];
            }
        }

        // Aplicar suavizado usando un promedio ponderado de vecinos
        for (int z = 0; z <= chunkSize; z++)
        {
            for (int x = 0; x <= chunkSize; x++)
            {
                // Saltarse los bordes (para compartir con otros chunks)
                if (x == 0 || x == chunkSize || z == 0 || z == chunkSize)
                    continue;

                // Contar biomas vecinos
                Dictionary<TerrainType, int> biomeCounts = new Dictionary<TerrainType, int>();

                for (int nz = -1; nz <= 1; nz++)
                {
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        int checkX = x + nx;
                        int checkZ = z + nz;

                        if (checkX >= 0 && checkX <= chunkSize &&
                            checkZ >= 0 && checkZ <= chunkSize)
                        {
                            TerrainType neighborType = originalMap[checkX, checkZ];

                            if (!biomeCounts.ContainsKey(neighborType))
                                biomeCounts[neighborType] = 0;

                            biomeCounts[neighborType]++;
                        }
                    }
                }

                // Suavizar solo si hay al menos 6 vecinos diferentes
                // (esto evita cambiar biomas grandes y homogéneos)
                TerrainType currentType = originalMap[x, z];
                TerrainType mostCommonType = currentType;
                int maxCount = 0;

                foreach (var pair in biomeCounts)
                {
                    if (pair.Value > maxCount)
                    {
                        maxCount = pair.Value;
                        mostCommonType = pair.Key;
                    }
                }

                // Si el bioma más común es muy diferente, cambiar
                if (mostCommonType != currentType && maxCount >= 5)
                {
                    biomeMap[x, z] = mostCommonType;
                }
            }
        }
    }
    #endregion

    #region Player Position
    private void Update()
    {
        // Keep player above terrain if initialized
        if (initialized && player != null)
        {
            Vector3 playerPos = player.position;
            float height = GetTerrainHeightAtPosition(playerPos) + minPlayerHeight;

            // Only adjust if player is below terrain
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
        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.z / chunkSize)
        );

        if (chunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            // Get local position within chunk
            float localX = worldPosition.x - chunkCoord.x * chunkSize;
            float localZ = worldPosition.z - chunkCoord.y * chunkSize;

            // Get indices of the four surrounding vertices
            int x0 = Mathf.FloorToInt(localX);
            int x1 = Mathf.Min(x0 + 1, chunkSize);
            int z0 = Mathf.FloorToInt(localZ);
            int z1 = Mathf.Min(z0 + 1, chunkSize);

            // Ensure indices are within bounds
            x0 = Mathf.Clamp(x0, 0, chunkSize);
            x1 = Mathf.Clamp(x1, 0, chunkSize);
            z0 = Mathf.Clamp(z0, 0, chunkSize);
            z1 = Mathf.Clamp(z1, 0, chunkSize);

            // Get heights of the four vertices
            float h00 = chunk.heightMap[x0, z0] * heightMultiplier;
            float h01 = chunk.heightMap[x0, z1] * heightMultiplier;
            float h10 = chunk.heightMap[x1, z0] * heightMultiplier;
            float h11 = chunk.heightMap[x1, z1] * heightMultiplier;

            // Calculate interpolation factors
            float tx = localX - x0;
            float tz = localZ - z0;

            // Bilinear interpolation for exact height
            float h0 = Mathf.Lerp(h00, h10, tx);
            float h1 = Mathf.Lerp(h01, h11, tx);
            return Mathf.Lerp(h0, h1, tz);
        }

        // If chunk not found, calculate height based on global coordinates
        float worldX = worldPosition.x;
        float worldZ = worldPosition.z;

        // Use the same noise method as in chunks
        float noiseValue = SampleNoise(worldX, worldZ);
        float height = heightCurve.Evaluate(noiseValue) * heightMultiplier;

        return height;
    }
    #endregion
}