using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static manager for biome data and climate calculations
/// </summary>
public static class BiomeSystem
{
    // Configuración del ruido para temperatura y humedad
    private static float temperatureNoiseScale = 0.01f;
    private static float humidityNoiseScale = 0.01f;

    // Lista estática de definiciones de biomas
    private static List<BiomeDefinition> biomes = new List<BiomeDefinition>();

    // Cache para almacenar datos de biomas por chunks
    private static Dictionary<Vector2Int, BiomeData> biomeDataCache = new Dictionary<Vector2Int, BiomeData>();

    // Tamaño del chunk para cálculos de bioma
    public static float chunkSize = 50f;

    private static readonly object syncObj = new object();

    private static Dictionary<string, Dictionary<Vector2Int, BiomeData>> neighborBiomeCache =
    new Dictionary<string, Dictionary<Vector2Int, BiomeData>>();

    // Estructura para almacenar datos de bioma por chunk
    public struct BiomeData
    {
        public float temperature;
        public float humidity;
        public string biomeName;
        public Color biomeColor;
    }

    /// <summary>
    /// Inicializa el sistema de biomas
    /// </summary>
    public static void Initialize()
    {
        if (biomes.Count == 0)
        {
            SetupDefaultBiomes();
        }
    }

    /// <summary>
    /// Configura la escala de ruido para el sistema de biomas
    /// </summary>
    public static void ConfigureNoiseSettings(float tempScale, float humidScale)
    {
        temperatureNoiseScale = tempScale;
        humidityNoiseScale = humidScale;
    }

    /// <summary>
    /// Configura los biomas por defecto
    /// </summary>
    private static void SetupDefaultBiomes()
    {
        //// Océano (frío y muy húmedo)
        //biomes.Add(new BiomeDefinition
        //{
        //    name = "Océano",
        //    minTemperature = 0.0f,
        //    maxTemperature = 0.4f,
        //    minHumidity = 0.7f,
        //    maxHumidity = 1.0f,
        //    biomeColor = new Color(0.0f, 0.3f, 0.8f),
        //    heightMultiplier = 0.3f, // Más bajo
        //    heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0.2f)
        //});

        //// Desierto
        //biomes.Add(new BiomeDefinition
        //{
        //    name = "Desierto",
        //    minTemperature = 0.7f,
        //    maxTemperature = 1.0f,
        //    minHumidity = 0.0f,
        //    maxHumidity = 0.2f,
        //    biomeColor = new Color(0.85f, 0.8f, 0.3f),
        //    heightMultiplier = 0.8f, // Altura media
        //    heightCurve = AnimationCurve.Linear(0, 0, 1, 1)
        //});

        //// Montaña
        //biomes.Add(new BiomeDefinition
        //{
        //    name = "Montaña",
        //    minTemperature = 0.0f,
        //    maxTemperature = 0.3f,
        //    minHumidity = 0.3f,
        //    maxHumidity = 0.7f,
        //    biomeColor = new Color(0.5f, 0.5f, 0.5f),
        //    heightMultiplier = 2.0f, // Mucho más alto
        //    heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1.5f)
        //});

        //// Llanura (temperatura media, humedad media-baja)
        //biomes.Add(new BiomeDefinition
        //{
        //    name = "Llanura",
        //    minTemperature = 0.4f,
        //    maxTemperature = 0.7f,
        //    minHumidity = 0.2f,
        //    maxHumidity = 0.5f,
        //    biomeColor = new Color(0.65f, 0.8f, 0.1f), // Verde amarillento
        //    heightMultiplier = 0.6f, // Altura baja-media
        //    heightCurve = AnimationCurve.Linear(0, 0, 1, 0.3f) // Terreno plano
        //});

        //// Jungla (caliente y muy húmedo)
        //biomes.Add(new BiomeDefinition
        //{
        //    name = "Jungla",
        //    minTemperature = 0.7f,
        //    maxTemperature = 1.0f,
        //    minHumidity = 0.7f,
        //    maxHumidity = 1.0f,
        //    biomeColor = new Color(0.0f, 0.6f, 0.0f), // Verde intenso
        //    heightMultiplier = 0.9f, // Altura media-alta
        //    heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0.8f) // Terreno ondulado
        //});

        // Bosque (temperatura media, humedad media-alta)
        biomes.Add(new BiomeDefinition
        {
            name = "Bosque",
            minTemperature = 0.3f,
            maxTemperature = 0.6f,
            minHumidity = 0.5f,
            maxHumidity = 0.8f,
            biomeColor = new Color(0.2f, 0.5f, 0.2f), // Verde oscuro
            heightMultiplier = 0.7f, // Altura media
            heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0.6f) // Colinas suaves
        });

        //// Tundra (frío y seco)
        //biomes.Add(new BiomeDefinition
        //{
        //    name = "Tundra",
        //    minTemperature = 0.0f,
        //    maxTemperature = 0.3f,
        //    minHumidity = 0.0f,
        //    maxHumidity = 0.3f,
        //    biomeColor = new Color(0.8f, 0.8f, 0.8f), // Blanco grisáceo
        //    heightMultiplier = 0.5f, // Altura baja
        //    heightCurve = AnimationCurve.Linear(0, 0, 1, 0.4f) // Terreno relativamente plano
        //});

        //// Pantano (temperatura media, muy húmedo)
        //biomes.Add(new BiomeDefinition
        //{
        //    name = "Pantano",
        //    minTemperature = 0.3f,
        //    maxTemperature = 0.7f,
        //    minHumidity = 0.8f,
        //    maxHumidity = 1.0f,
        //    biomeColor = new Color(0.4f, 0.5f, 0.3f), // Verde parduzco
        //    heightMultiplier = 0.4f, // Altura muy baja (pantanos suelen ser bajos)
        //    heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0.1f) // Terreno muy plano y bajo
        //});
    }

    /// <summary>
    /// Convierte coordenadas de mundo a coordenadas de chunk
    /// </summary>
    public static Vector2Int WorldToChunkCoord(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.y / chunkSize)
        );
    }

    /// <summary>
    /// Convierte coordenadas de chunk a coordenadas de mundo
    /// </summary>
    public static Vector2 ChunkToWorldCoord(Vector2Int chunkCoord)
    {
        return new Vector2(
            (chunkCoord.x * chunkSize) + (chunkSize * 0.5f),
            (chunkCoord.y * chunkSize) + (chunkSize * 0.5f)
        );
    }

    public static Dictionary<Vector2Int, BiomeData> GetNeighborBiomesWithCache(Vector2 centerPosition)
    {
        // Asegurarnos de que neighborBiomeCache jamás sea null
        if (neighborBiomeCache == null)
        {
            lock (syncObj)
            {
                if (neighborBiomeCache == null)
                {
                    neighborBiomeCache = new Dictionary<string, Dictionary<Vector2Int, BiomeData>>();
                }
            }
        }

        // Construir la clave a partir de centerPosition (con un formato sin demasiada precisión)
        string cacheKey = $"{centerPosition.x:F1}_{centerPosition.y:F1}";

        // Si ya existe el entry en el caché, lo devolvemos directamente
        lock (syncObj)
        {
            if (neighborBiomeCache.TryGetValue(cacheKey, out Dictionary<Vector2Int, BiomeData> cached))
            {
                return cached;
            }
        }

        // Si no está en el caché, lo generamos y luego lo guardamos
        Dictionary<Vector2Int, BiomeData> neighbors = new Dictionary<Vector2Int, BiomeData>();

        Vector2Int[] offsets = {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(-1,  0), new Vector2Int(0,  0), new Vector2Int(1,  0),
            new Vector2Int(-1,  1), new Vector2Int(0,  1), new Vector2Int(1,  1)
        };

        foreach (var offset in offsets)
        {
            // Para cada offset (-1..1 en X y Z), calcular posición mundial del centro del chunk vecino:
            Vector2Int centerChunkCoord = WorldToChunkCoord(centerPosition);
            Vector2Int neighborChunkCoord = new Vector2Int(
                centerChunkCoord.x + offset.x,
                centerChunkCoord.y + offset.y
            );
            Vector2 neighborWorldCenter = ChunkToWorldCoord(neighborChunkCoord);

            // Obtener BiomeData para ese chunk vecino
            BiomeData bd = GetBiomeData(new Vector3(neighborWorldCenter.x, 0, neighborWorldCenter.y));
            neighbors[offset] = bd;
        }

        // Guardar en caché de forma segura
        lock (syncObj)
        {
            // Verificamos de nuevo para evitar sobrescribir si otro hilo ya lo guardó
            if (!neighborBiomeCache.ContainsKey(cacheKey))
            {
                neighborBiomeCache[cacheKey] = neighbors;
            }
        }

        // Si el diccionario crece demasiado, limpiamos el más antiguo
        lock (syncObj)
        {
            if (neighborBiomeCache.Count > 1000)
            {
                var oldestKey = neighborBiomeCache.Keys.First();
                neighborBiomeCache.Remove(oldestKey);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Obtiene los datos de bioma para una posición en el mundo
    /// </summary>
    public static BiomeData GetBiomeData(Vector3 worldPosition)
    {
        if (biomes.Count == 0)
        {
            Initialize();
        }

        Vector2Int chunkCoord = WorldToChunkCoord(new Vector2(worldPosition.x, worldPosition.z));

        if (biomeDataCache.TryGetValue(chunkCoord, out BiomeData biomeData))
        {
            return biomeData;
        }

        BiomeData generated = GenerateBiomeData(chunkCoord);
        biomeDataCache[chunkCoord] = generated;
        return generated;
    }

    /// <summary>
    /// Limpia la caché de datos de bioma
    /// </summary>
    public static void ClearCache()
    {
        biomeDataCache.Clear();
    }

    /// <summary>
    /// Realiza una limpieza limitada de la caché para chunks alrededor de una posición
    /// </summary>
    public static void CleanupCache(Vector3 centerPosition, float maxDistance)
    {
        Vector2Int centerChunk = WorldToChunkCoord(new Vector2(centerPosition.x, centerPosition.z));
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        // Identificar chunks para eliminar
        foreach (var chunk in biomeDataCache.Keys)
        {
            if (Vector2Int.Distance(chunk, centerChunk) > maxDistance)
            {
                chunksToRemove.Add(chunk);
            }
        }

        // Eliminar los chunks que están demasiado lejos
        foreach (var chunk in chunksToRemove)
        {
            biomeDataCache.Remove(chunk);
        }
    }

    /// <summary>
    /// Genera los datos de bioma para un chunk específico
    /// </summary>
    private static BiomeData GenerateBiomeData(Vector2Int chunkCoord)
    {
        Vector2 chunkCenter = ChunkToWorldCoord(chunkCoord);

        float temperature = Mathf.PerlinNoise(
            chunkCenter.x * temperatureNoiseScale,
            chunkCenter.y * temperatureNoiseScale
        );
        float humidity = Mathf.PerlinNoise(
            (chunkCenter.x + 1000) * humidityNoiseScale,
            (chunkCenter.y + 1000) * humidityNoiseScale
        );

        string biomeName = "Desconocido";
        Color biomeColor = Color.magenta;

        bool foundExactMatch = false;
        foreach (var biome in biomes)
        {
            if (biome.MatchesConditions(temperature, humidity))
            {
                biomeName = biome.name;
                biomeColor = biome.biomeColor;
                foundExactMatch = true;
                break;
            }
        }

        if (!foundExactMatch && biomes.Count > 0)
        {
            float closestDistance = float.MaxValue;
            BiomeDefinition closestBiome = null;

            foreach (var b in biomes)
            {
                float d = b.GetDistanceToBiome(temperature, humidity);
                if (d < closestDistance)
                {
                    closestDistance = d;
                    closestBiome = b;
                }
            }

            if (closestBiome != null)
            {
                biomeName = closestBiome.name;
                biomeColor = closestBiome.biomeColor;
            }
        }

        return new BiomeData
        {
            temperature = temperature,
            humidity = humidity,
            biomeName = biomeName,
            biomeColor = biomeColor
        };
    }


    /// <summary>
    /// Pregenera datos de bioma para una región alrededor de una posición central
    /// </summary>
    public static void PregenerateRegion(Vector3 centerPosition, int chunkRadius)
    {
        Vector2Int centerChunk = WorldToChunkCoord(new Vector2(centerPosition.x, centerPosition.z));

        for (int x = -chunkRadius; x <= chunkRadius; x++)
        {
            for (int z = -chunkRadius; z <= chunkRadius; z++)
            {
                Vector2Int chunkPos = new Vector2Int(centerChunk.x + x, centerChunk.y + z);

                if (!biomeDataCache.ContainsKey(chunkPos))
                {
                    biomeDataCache[chunkPos] = GenerateBiomeData(chunkPos);
                }
            }
        }
    }

    /// <summary>
    /// Obtiene todos los biomas disponibles
    /// </summary>
    public static List<BiomeDefinition> GetAllBiomes()
    {
        if (biomes.Count == 0)
        {
            Initialize();
        }

        return new List<BiomeDefinition>(biomes);
    }

    /// <summary>
    /// Añade un bioma personalizado a la lista de biomas
    /// </summary>
    public static void AddCustomBiome(BiomeDefinition biome)
    {
        if (biomes.Count == 0)
        {
            Initialize();
        }

        biomes.Add(biome);
    }

    /// <summary>
    /// Aplica un suavizado a los datos de bioma para evitar transiciones bruscas
    /// </summary>
    public static void SmoothBiomes(Vector3 centerPosition, int radius)
    {
        Vector2Int centerChunk = WorldToChunkCoord(new Vector2(centerPosition.x, centerPosition.z));
        Dictionary<Vector2Int, string> newBiomes = new Dictionary<Vector2Int, string>();

        // Asegurarse de que tenemos los chunks necesarios
        PregenerateRegion(centerPosition, radius);

        // Para cada chunk en el radio, verificar si necesita ser suavizado
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                Vector2Int chunkPos = new Vector2Int(centerChunk.x + x, centerChunk.y + z);

                if (!biomeDataCache.ContainsKey(chunkPos))
                    continue;

                // Verificar si el chunk tiene al menos 3 vecinos con el mismo bioma
                if (HasEnoughSameNeighbors(chunkPos))
                    continue;

                // Contar biomas vecinos para encontrar el más común
                Dictionary<string, int> neighborBiomeCounts = CountNeighborBiomes(chunkPos);

                if (neighborBiomeCounts.Count > 0)
                {
                    // Encontrar el bioma más común
                    string mostCommonBiome = "";
                    int maxCount = 0;

                    foreach (var pair in neighborBiomeCounts)
                    {
                        if (pair.Value > maxCount)
                        {
                            maxCount = pair.Value;
                            mostCommonBiome = pair.Key;
                        }
                    }

                    // Si hay suficientes vecinos con este bioma, aplicar el cambio
                    if (maxCount >= 5)
                    {
                        newBiomes[chunkPos] = mostCommonBiome;
                    }
                }
            }
        }

        // Aplicar los nuevos biomas
        foreach (var change in newBiomes)
        {
            BiomeData currentData = biomeDataCache[change.Key];

            // Encontrar la definición del bioma para obtener el color
            BiomeDefinition biomeDefinition = null;
            foreach (var biome in biomes)
            {
                if (biome.name == change.Value)
                {
                    biomeDefinition = biome;
                    break;
                }
            }

            if (biomeDefinition != null)
            {
                // Actualizar los datos de bioma
                currentData.biomeName = change.Value;
                currentData.biomeColor = biomeDefinition.biomeColor;
                biomeDataCache[change.Key] = currentData;
            }
        }
    }

    /// <summary>
    /// Verifica si un chunk tiene suficientes vecinos con el mismo bioma
    /// </summary>
    private static bool HasEnoughSameNeighbors(Vector2Int pos)
    {
        if (!biomeDataCache.ContainsKey(pos))
            return false;

        string currentBiome = biomeDataCache[pos].biomeName;
        int sameNeighbors = 0;

        // Verificar los 8 chunks adyacentes
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                    continue; // Saltar el propio chunk

                Vector2Int neighborPos = new Vector2Int(pos.x + x, pos.y + z);

                if (biomeDataCache.ContainsKey(neighborPos) &&
                    biomeDataCache[neighborPos].biomeName == currentBiome)
                {
                    sameNeighbors++;
                }
            }
        }

        // Si al menos 3 vecinos tienen el mismo bioma, hay continuidad
        return sameNeighbors >= 3;
    }

    /// <summary>
    /// Cuenta los biomas vecinos para un chunk
    /// </summary>
    private static Dictionary<string, int> CountNeighborBiomes(Vector2Int pos)
    {
        Dictionary<string, int> neighborBiomeCounts = new Dictionary<string, int>();

        // Verificar los 8 chunks adyacentes
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                    continue; // Saltar el propio chunk

                Vector2Int neighborPos = new Vector2Int(pos.x + x, pos.y + z);

                if (biomeDataCache.ContainsKey(neighborPos))
                {
                    string neighborBiome = biomeDataCache[neighborPos].biomeName;

                    if (!neighborBiomeCounts.ContainsKey(neighborBiome))
                        neighborBiomeCounts[neighborBiome] = 0;

                    neighborBiomeCounts[neighborBiome]++;
                }
            }
        }

        return neighborBiomeCounts;
    }
}