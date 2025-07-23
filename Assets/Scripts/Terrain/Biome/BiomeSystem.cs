using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Static manager for biome data and climate calculations
/// </summary>
public static class BiomeSystem
{
    // Configuraci�n del ruido para temperatura y humedad
    private static float temperatureNoiseScale = 0.01f;
    private static float humidityNoiseScale = 0.01f;

    // Lista est�tica de definiciones de biomas
    private static List<BiomeDefinition> biomes = new List<BiomeDefinition>();

    // Cache para almacenar datos de biomas por chunks
    private static Dictionary<Vector2Int, BiomeData> biomeDataCache = new Dictionary<Vector2Int, BiomeData>();

    // Tama�o del chunk para c�lculos de bioma
    public static float chunkSize = 50f;

    public struct BiomeInfluence
    {
        public string biomeName;
        public float influence;
    }

    // Estructura para almacenar datos de bioma por chunk
    public struct BiomeData
    {
        public float temperature;
        public float humidity;
        public List<BiomeInfluence> influences;
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
        // Océano (frío y muy húmedo)
        biomes.Add(new BiomeDefinition
        {
            name = "Océano",
            minTemperature = 0.0f,
            maxTemperature = 0.4f,
            minHumidity = 0.7f,
            maxHumidity = 1.0f,
            biomeColor = new Color(0.0f, 0.3f, 0.8f),
            heightMultiplier = 0.3f, // Más bajo
            heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0.2f)
        });

        // Desierto
        biomes.Add(new BiomeDefinition
        {
            name = "Desierto",
            minTemperature = 0.7f,
            maxTemperature = 1.0f,
            minHumidity = 0.0f,
            maxHumidity = 0.2f,
            biomeColor = new Color(0.85f, 0.8f, 0.3f),
            heightMultiplier = 0.8f, // Altura media
            heightCurve = AnimationCurve.Linear(0, 0, 1, 1)
        });

        // Montaña
        biomes.Add(new BiomeDefinition
        {
            name = "Montaña",
            minTemperature = 0.0f,
            maxTemperature = 0.3f,
            minHumidity = 0.3f,
            maxHumidity = 0.7f,
            biomeColor = new Color(0.5f, 0.5f, 0.5f),
            heightMultiplier = 2.0f, // Mucho más alto
            heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1.5f)
        });

        // Llanura (temperatura media, humedad media-baja)
        biomes.Add(new BiomeDefinition
        {
            name = "Llanura",
            minTemperature = 0.4f,
            maxTemperature = 0.7f,
            minHumidity = 0.2f,
            maxHumidity = 0.5f,
            biomeColor = new Color(0.65f, 0.8f, 0.1f), // Verde amarillento
            heightMultiplier = 0.6f, // Altura baja-media
            heightCurve = AnimationCurve.Linear(0, 0, 1, 0.3f) // Terreno plano
        });

        // Jungla (caliente y muy húmedo)
        biomes.Add(new BiomeDefinition
        {
            name = "Jungla",
            minTemperature = 0.7f,
            maxTemperature = 1.0f,
            minHumidity = 0.7f,
            maxHumidity = 1.0f,
            biomeColor = new Color(0.0f, 0.6f, 0.0f), // Verde intenso
            heightMultiplier = 0.9f, // Altura media-alta
            heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0.8f) // Terreno ondulado
        });

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

        // Tundra (frío y seco)
        biomes.Add(new BiomeDefinition
        {
            name = "Tundra",
            minTemperature = 0.0f,
            maxTemperature = 0.3f,
            minHumidity = 0.0f,
            maxHumidity = 0.3f,
            biomeColor = new Color(0.8f, 0.8f, 0.8f), // Blanco grisáceo
            heightMultiplier = 0.5f, // Altura baja
            heightCurve = AnimationCurve.Linear(0, 0, 1, 0.4f) // Terreno relativamente plano
        });

        // Pantano (temperatura media, muy húmedo)
        biomes.Add(new BiomeDefinition
        {
            name = "Pantano",
            minTemperature = 0.3f,
            maxTemperature = 0.7f,
            minHumidity = 0.8f,
            maxHumidity = 1.0f,
            biomeColor = new Color(0.4f, 0.5f, 0.3f), // Verde parduzco
            heightMultiplier = 0.4f, // Altura muy baja (pantanos suelen ser bajos)
            heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0.1f) // Terreno muy plano y bajo
        });
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

    /// <summary>
    /// Obtiene los datos de bioma para una posicin en el mundo, utilizando la cach
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

        BiomeData generated = GenerateBiomeDataForPosition(worldPosition);
        biomeDataCache[chunkCoord] = generated;
        return generated;
    }

    /// <summary>
    /// Obtiene los datos de bioma para una posicin especfica del mundo (vrtice), sin usar cach
    /// </summary>
    public static BiomeData GetBiomeDataForVertex(Vector3 worldPosition)
    {
        if (biomes.Count == 0)
        {
            Initialize();
        }

        return GenerateBiomeDataForPosition(worldPosition);
    }

    /// <summary>
    /// Limpia la cach de datos de bioma
    /// </summary>
    public static void ClearCache()
    {
        biomeDataCache.Clear();
    }

    /// <summary>
    /// Realiza una limpieza limitada de la cach� para chunks alrededor de una posici�n
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

        // Eliminar los chunks que est�n demasiado lejos
        foreach (var chunk in chunksToRemove)
        {
            biomeDataCache.Remove(chunk);
        }
    }

    /// <summary>
    /// Genera los datos de bioma para un chunk específico
    /// </summary>
    private static BiomeData GenerateBiomeDataForPosition(Vector3 worldPosition)
    {
        float temperature = Mathf.PerlinNoise(
            worldPosition.x * temperatureNoiseScale,
            worldPosition.z * temperatureNoiseScale
        );
        float humidity = Mathf.PerlinNoise(
            (worldPosition.x + 1000) * humidityNoiseScale,
            (worldPosition.z + 1000) * humidityNoiseScale
        );

        // Aplicar suavizado adicional para crear transiciones más graduales
        float smoothingRadius = 0.3f; // Radio de suavizado en el espacio temperatura-humedad
        
        var biomeDistances = new List<Tuple<BiomeDefinition, float>>();
        foreach (var biome in biomes)
        {
            float distance = biome.GetDistanceToBiome(temperature, humidity);
            biomeDistances.Add(new Tuple<BiomeDefinition, float>(biome, distance));
        }

        // Sort biomes by distance, ascending
        biomeDistances.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        List<BiomeInfluence> influences = new List<BiomeInfluence>();
        float totalWeight = 0f;

        // Considerar todos los biomas dentro del radio de suavizado
        foreach (var biomeDist in biomeDistances)
        {
            if (biomeDist.Item2 <= smoothingRadius)
            {
                // Usar una función de suavizado más gradual
                float normalizedDistance = biomeDist.Item2 / smoothingRadius;
                float weight = Mathf.Pow(1f - normalizedDistance, 2f); // Función cuadrática inversa
                
                if (weight > 0.01f) // Umbral mínimo para evitar influencias muy pequeñas
                {
                    influences.Add(new BiomeInfluence { 
                        biomeName = biomeDist.Item1.name, 
                        influence = weight 
                    });
                    totalWeight += weight;
                }
            }
        }

        // Si no hay biomas dentro del radio, usar los 3 más cercanos con pesos decrecientes
        if (influences.Count == 0)
        {
            int maxBiomes = Mathf.Min(3, biomeDistances.Count);
            for (int i = 0; i < maxBiomes; i++)
            {
                var biomeDist = biomeDistances[i];
                float weight = 1f / (1f + biomeDist.Item2 * 3f); // Decaimiento más suave
                influences.Add(new BiomeInfluence { 
                    biomeName = biomeDist.Item1.name, 
                    influence = weight 
                });
                totalWeight += weight;
            }
        }

        // Normalize influences
        if (totalWeight > 0)
        {
            for (int i = 0; i < influences.Count; i++)
            {
                var influence = influences[i];
                influence.influence /= totalWeight;
                influences[i] = influence;
            }
        }

        return new BiomeData
        {
            temperature = temperature,
            humidity = humidity,
            influences = influences
        };
    }


    /// <summary>
    /// Pregenera datos de bioma para una regi�n alrededor de una posici�n central
    /// </summary>
    public static void PregenerateRegion(Vector3 centerPosition, int chunkRadius)
    {
        Vector2Int centerChunk = WorldToChunkCoord(new Vector2(centerPosition.x, centerPosition.z));

        for (int x = -chunkRadius; x <= chunkRadius; x++)
        {
            for (int z = -chunkRadius; z <= chunkRadius; z++)
            {
                Vector2Int chunkPos = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                Vector2 worldPos = ChunkToWorldCoord(chunkPos);

                if (!biomeDataCache.ContainsKey(chunkPos))
                {
                    biomeDataCache[chunkPos] = GenerateBiomeDataForPosition(new Vector3(worldPos.x, 0, worldPos.y));
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
    /// A�ade un bioma personalizado a la lista de biomas
    /// </summary>
    public static void AddCustomBiome(BiomeDefinition biome)
    {
        if (biomes.Count == 0)
        {
            Initialize();
        }

        biomes.Add(biome);
    }
}