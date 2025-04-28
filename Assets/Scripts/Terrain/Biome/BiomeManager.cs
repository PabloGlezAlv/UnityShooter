
using System.Collections.Generic;
using UnityEngine;

public class BiomeManager : MonoBehaviour
{
    public Transform player;
    public float temperatureUpdateDistance = 25f;
    public float chunkSize = 50f;

    // Noise settings for temperature and humidity (normalized 0-1)
    [Header("Climate Settings")]
    [Range(0.001f, 0.1f)]
    public float temperatureNoiseScale = 0.01f;
    [Range(0.001f, 0.1f)]
    public float humidityNoiseScale = 0.01f;

    [Header("Biome Definitions")]
    public List<BiomeDefinition> biomes = new List<BiomeDefinition>();

    [Header("Debug")]
    public bool showGizmos = true;
    public float gizmoSize = 5f;
    public bool showBiomeNames = true;

    private Dictionary<Vector2Int, BiomeChunkData> biomeChunks = new Dictionary<Vector2Int, BiomeChunkData>();
    private Vector2 lastPlayerPosition;

    private void Start()
    {
        if (player == null)
            player = Camera.main.transform;

        lastPlayerPosition = new Vector2(player.position.x, player.position.z);

        // Si no hay biomas definidos, crear los biomas especificados
        if (biomes.Count == 0)
        {
            SetupDefaultBiomes();
        }

        UpdateBiomeData();
    }

    private void SetupDefaultBiomes()
    {
        // Océano (frío y muy húmedo)
        biomes.Add(new BiomeDefinition
        {
            name = "Océano",
            minTemperature = 0.0f,
            maxTemperature = 0.4f,
            minHumidity = 0.7f,
            maxHumidity = 1.0f,
            biomeColor = new Color(0.0f, 0.3f, 0.8f) // Azul
        });

        // Desierto (caliente y seco)
        biomes.Add(new BiomeDefinition
        {
            name = "Desierto",
            minTemperature = 0.7f,
            maxTemperature = 1.0f,
            minHumidity = 0.0f,
            maxHumidity = 0.2f,
            biomeColor = new Color(0.85f, 0.8f, 0.3f) // Amarillo arena
        });

        // Llanura (temperatura media, humedad media-baja)
        biomes.Add(new BiomeDefinition
        {
            name = "Llanura",
            minTemperature = 0.4f,
            maxTemperature = 0.7f,
            minHumidity = 0.2f,
            maxHumidity = 0.5f,
            biomeColor = new Color(0.65f, 0.8f, 0.1f) // Verde amarillento
        });

        // Jungla (caliente y muy húmedo)
        biomes.Add(new BiomeDefinition
        {
            name = "Jungla",
            minTemperature = 0.7f,
            maxTemperature = 1.0f,
            minHumidity = 0.7f,
            maxHumidity = 1.0f,
            biomeColor = new Color(0.0f, 0.6f, 0.0f) // Verde intenso
        });

        // Bosque (temperatura media, humedad media-alta)
        biomes.Add(new BiomeDefinition
        {
            name = "Bosque",
            minTemperature = 0.3f,
            maxTemperature = 0.6f,
            minHumidity = 0.5f,
            maxHumidity = 0.8f,
            biomeColor = new Color(0.2f, 0.5f, 0.2f) // Verde oscuro
        });

        // Montaña (frío y humedad variable)
        biomes.Add(new BiomeDefinition
        {
            name = "Montaña",
            minTemperature = 0.0f,
            maxTemperature = 0.3f,
            minHumidity = 0.3f,
            maxHumidity = 0.7f,
            biomeColor = new Color(0.5f, 0.5f, 0.5f) // Gris
        });

        // Asegurarse de que hay cobertura para las áreas faltantes
        // Tundra (frío y seco)
        biomes.Add(new BiomeDefinition
        {
            name = "Tundra",
            minTemperature = 0.0f,
            maxTemperature = 0.3f,
            minHumidity = 0.0f,
            maxHumidity = 0.3f,
            biomeColor = new Color(0.8f, 0.8f, 0.8f) // Blanco grisáceo
        });

        // Pantano (temperatura media, muy húmedo)
        biomes.Add(new BiomeDefinition
        {
            name = "Pantano",
            minTemperature = 0.3f,
            maxTemperature = 0.7f,
            minHumidity = 0.8f,
            maxHumidity = 1.0f,
            biomeColor = new Color(0.4f, 0.5f, 0.3f) // Verde parduzco
        });
    }

    private void Update()
    {
        Vector2 currentPlayerPos = new Vector2(player.position.x, player.position.z);

        // Only update if player has moved enough
        if (Vector2.Distance(currentPlayerPos, lastPlayerPosition) > temperatureUpdateDistance)
        {
            lastPlayerPosition = currentPlayerPos;
            UpdateBiomeData();
        }
    }

    private void UpdateBiomeData()
    {
        // Get current chunk coordinates
        Vector2Int currentChunk = WorldToChunkCoord(new Vector2(player.position.x, player.position.z));

        // Clear old chunks that are too far away
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var chunk in biomeChunks.Keys)
        {
            if (Vector2Int.Distance(chunk, currentChunk) > 5) // Keep chunks within 5 chunk radius
            {
                chunksToRemove.Add(chunk);
            }
        }

        foreach (var chunk in chunksToRemove)
        {
            biomeChunks.Remove(chunk);
        }

        // Generate data for new chunks around player
        int viewDistance = 3; // How many chunks to load around player
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(currentChunk.x + x, currentChunk.y + z);
                if (!biomeChunks.ContainsKey(chunkCoord))
                {
                    BiomeChunkData newChunkData = GenerateChunkBiomeData(chunkCoord);
                    biomeChunks.Add(chunkCoord, newChunkData);
                }
            }
        }

        // Ensure biome continuity by smoothing out isolated biomes
        SmoothBiomes();
    }

    // Esta es la línea incorrecta:
    // string mostCommonBiome = neighborBiomeCounts.OrderByDescending(b => b.Value).First().Key;

    // Aquí está la versión corregida para el método SmoothBiomes():
    private void SmoothBiomes()
    {
        Dictionary<Vector2Int, string> newBiomes = new Dictionary<Vector2Int, string>();

        // For each chunk, check neighboring chunks
        foreach (var chunk in biomeChunks)
        {
            Vector2Int pos = chunk.Key;

            // Skip chunks that already have neighboring chunks with the same biome
            if (HasNeighborsWithSameBiome(pos))
                continue;

            // Count neighboring biomes to find the most common one
            Dictionary<string, int> neighborBiomeCounts = new Dictionary<string, int>();

            // Check all 8 adjacent chunks
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0) continue; // Skip self

                    Vector2Int neighborPos = new Vector2Int(pos.x + x, pos.y + z);
                    if (biomeChunks.ContainsKey(neighborPos))
                    {
                        string neighborBiome = biomeChunks[neighborPos].biomeName;
                        if (!neighborBiomeCounts.ContainsKey(neighborBiome))
                            neighborBiomeCounts[neighborBiome] = 0;
                        neighborBiomeCounts[neighborBiome]++;
                    }
                }
            }

            if (neighborBiomeCounts.Count > 0)
            {
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

                if (maxCount >= 5)
                {
                    newBiomes[pos] = mostCommonBiome;
                }
            }
        }

        // Apply new biomes
        foreach (var change in newBiomes)
        {
            biomeChunks[change.Key].biomeName = change.Value;
            // Find the biome definition to get the color
            BiomeDefinition biomeDefinition = biomes.Find(b => b.name == change.Value);
            if (biomeDefinition != null)
            {
                biomeChunks[change.Key].biomeColor = biomeDefinition.biomeColor;
            }
        }
    }

    private bool HasNeighborsWithSameBiome(Vector2Int pos)
    {
        string currentBiome = biomeChunks[pos].biomeName;
        int sameNeighbors = 0;

        // Check all 8 adjacent chunks
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue; // Skip self

                Vector2Int neighborPos = new Vector2Int(pos.x + x, pos.y + z);
                if (biomeChunks.ContainsKey(neighborPos) && biomeChunks[neighborPos].biomeName == currentBiome)
                {
                    sameNeighbors++;
                }
            }
        }

        // If at least 3 neighbors have the same biome, consider it continuity
        return sameNeighbors >= 3;
    }

    private BiomeChunkData GenerateChunkBiomeData(Vector2Int chunkCoord)
    {
        // Center of chunk in world coordinates
        Vector2 chunkCenter = ChunkToWorldCoord(chunkCoord);

        // Generate temperature and humidity using Perlin noise (normalized 0-1)
        float temperature = Mathf.PerlinNoise(chunkCenter.x * temperatureNoiseScale, chunkCenter.y * temperatureNoiseScale);
        float humidity = Mathf.PerlinNoise((chunkCenter.x + 1000) * humidityNoiseScale, (chunkCenter.y + 1000) * humidityNoiseScale);

        // Determine biome based on temperature and humidity
        string biomeName = "Desconocido";
        Color biomeColor = Color.magenta;

        // PARTE MODIFICADA: Mejor manejo del caso cuando no hay coincidencia exacta
        // Primero intentamos una coincidencia exacta
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

        // Si no hay coincidencia exacta, encontrar el bioma más cercano
        if (!foundExactMatch && biomes.Count > 0)
        {
            float closestDistance = float.MaxValue;
            BiomeDefinition closestBiome = null;

            foreach (var biome in biomes)
            {
                float distance = biome.GetDistanceToBiome(temperature, humidity);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBiome = biome;
                }
            }

            if (closestBiome != null)
            {
                biomeName = closestBiome.name;
                biomeColor = closestBiome.biomeColor;
            }
        }

        return new BiomeChunkData(temperature, humidity, biomeName, biomeColor);
    }

    private Vector2Int WorldToChunkCoord(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.y / chunkSize)
        );
    }

    private Vector2 ChunkToWorldCoord(Vector2Int chunkCoord)
    {
        return new Vector2(
            (chunkCoord.x * chunkSize) + (chunkSize * 0.5f),
            (chunkCoord.y * chunkSize) + (chunkSize * 0.5f)
        );
    }

    // Get climate and biome at any world position
    public (float temperature, float humidity, string biomeName) GetBiomeAtPosition(Vector3 worldPosition)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(new Vector2(worldPosition.x, worldPosition.z));

        // If chunk data doesn't exist, generate it
        if (!biomeChunks.ContainsKey(chunkCoord))
        {
            biomeChunks.Add(chunkCoord, GenerateChunkBiomeData(chunkCoord));
        }

        return (biomeChunks[chunkCoord].temperature, biomeChunks[chunkCoord].humidity, biomeChunks[chunkCoord].biomeName);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying)
            return;

        // Draw gizmos for each chunk
        foreach (var chunk in biomeChunks)
        {
            Vector2 chunkCenter = ChunkToWorldCoord(chunk.Key);
            Vector3 chunkPosition = new Vector3(chunkCenter.x, player.position.y, chunkCenter.y);

            // Draw temperature area (wire cube)
            Color temperatureColor = Color.Lerp(Color.black, Color.red, chunk.Value.temperature);
            Gizmos.color = temperatureColor;
            Gizmos.DrawWireCube(chunkPosition, new Vector3(chunkSize, gizmoSize, chunkSize));

            // Draw humidity area (wire cube slightly higher)
            Color humidityColor = Color.Lerp(Color.black, Color.blue, chunk.Value.humidity);
            Gizmos.color = humidityColor;
            Vector3 humidityPosition = chunkPosition + Vector3.up * (gizmoSize * 0.6f);
            Gizmos.DrawWireCube(humidityPosition, new Vector3(chunkSize * 0.6f, gizmoSize * 0.6f, chunkSize * 0.6f));

            // Draw biome area (wire cube even higher)
            Gizmos.color = chunk.Value.biomeColor;
            Vector3 biomePosition = chunkPosition + Vector3.up * (gizmoSize * 1.2f);
            Gizmos.DrawWireCube(biomePosition, new Vector3(chunkSize * 0.8f, gizmoSize * 0.6f, chunkSize * 0.8f));

            // Draw text labels in scene view (only works in editor)
#if UNITY_EDITOR
            if (showBiomeNames)
            {
                UnityEditor.Handles.Label(chunkPosition + Vector3.up * gizmoSize,
                    $"T: {chunk.Value.temperature:F2}\nH: {chunk.Value.humidity:F2}\nBioma: {chunk.Value.biomeName}");
            }
#endif
        }

        // Draw player's current biome values
        if (player != null)
        {
            var biomeInfo = GetBiomeAtPosition(player.position);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(player.position + Vector3.up * gizmoSize * 2.5f,
                $"Player Biome:\nTemp: {biomeInfo.temperature:F2}\nHumidity: {biomeInfo.humidity:F2}\nBioma: {biomeInfo.biomeName}");
#endif
        }
    }
}

// Store climate and biome data for each chunk
public class BiomeChunkData
{
    public float temperature;
    public float humidity;
    public string biomeName;
    public Color biomeColor;

    public BiomeChunkData(float temp, float humid, string biome, Color color)
    {
        temperature = temp;
        humidity = humid;
        biomeName = biome;
        biomeColor = color;
    }
}