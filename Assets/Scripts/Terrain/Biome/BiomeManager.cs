using UnityEngine;
using System.Collections.Generic;

public class BiomeManager : MonoBehaviour
{
    public Transform player;
    public float temperatureUpdateDistance = 25f;
    public float chunkSize = 50f;

    // Noise settings for temperature and humidity
    [Header("Temperature Settings")]
    public float temperatureNoiseScale = 0.01f;
    public float temperatureMin = -20f;
    public float temperatureMax = 40f;

    [Header("Humidity Settings")]
    public float humidityNoiseScale = 0.01f;
    public float humidityMin = 0f;
    public float humidityMax = 100f;

    [Header("Debug")]
    public bool showGizmos = true;
    public float gizmoSize = 5f;

    private Dictionary<Vector2Int, BiomeChunkData> biomeChunks = new Dictionary<Vector2Int, BiomeChunkData>();
    private Vector2 lastPlayerPosition;

    private void Start()
    {
        if (player == null)
            player = Camera.main.transform;

        lastPlayerPosition = new Vector2(player.position.x, player.position.z);
        UpdateBiomeData();
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
    }

    private BiomeChunkData GenerateChunkBiomeData(Vector2Int chunkCoord)
    {
        // Center of chunk in world coordinates
        Vector2 chunkCenter = ChunkToWorldCoord(chunkCoord);

        // Generate temperature and humidity using Perlin noise
        float temperature = Mathf.Lerp(temperatureMin, temperatureMax,
                                     Mathf.PerlinNoise(chunkCenter.x * temperatureNoiseScale, chunkCenter.y * temperatureNoiseScale));

        float humidity = Mathf.Lerp(humidityMin, humidityMax,
                                   Mathf.PerlinNoise((chunkCenter.x + 1000) * humidityNoiseScale, (chunkCenter.y + 1000) * humidityNoiseScale));

        return new BiomeChunkData(temperature, humidity);
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

    // Get temperature and humidity at any world position
    public (float temperature, float humidity) GetClimateAtPosition(Vector3 worldPosition)
    {
        Vector2Int chunkCoord = WorldToChunkCoord(new Vector2(worldPosition.x, worldPosition.z));

        // If chunk data doesn't exist, generate it
        if (!biomeChunks.ContainsKey(chunkCoord))
        {
            biomeChunks.Add(chunkCoord, GenerateChunkBiomeData(chunkCoord));
        }

        return (biomeChunks[chunkCoord].temperature, biomeChunks[chunkCoord].humidity);
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
            float temperatureNormalized = Mathf.InverseLerp(temperatureMin, temperatureMax, chunk.Value.temperature);
            Color temperatureColor = Color.Lerp(Color.black, Color.red, temperatureNormalized);
            Gizmos.color = temperatureColor;
            Gizmos.DrawWireCube(chunkPosition, new Vector3(chunkSize, gizmoSize, chunkSize));

            // Draw humidity area (wire cube slightly higher)
            float humidityNormalized = Mathf.InverseLerp(humidityMin, humidityMax, chunk.Value.humidity);
            Color humidityColor = Color.Lerp(Color.black, Color.blue, humidityNormalized);
            Gizmos.color = humidityColor;
            Vector3 humidityPosition = chunkPosition + Vector3.up * (gizmoSize * 0.6f);
            Gizmos.DrawWireCube(humidityPosition, new Vector3(chunkSize * 0.6f, gizmoSize * 0.6f, chunkSize * 0.6f));

            // Draw text labels in scene view (only works in editor)
#if UNITY_EDITOR
            UnityEditor.Handles.Label(chunkPosition + Vector3.up * gizmoSize,
                $"T: {chunk.Value.temperature:F1}°C\nH: {chunk.Value.humidity:F1}%");
#endif
        }

        // Draw player's current climate values
        if (player != null)
        {
            var climate = GetClimateAtPosition(player.position);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(player.position + Vector3.up * gizmoSize * 2f,
                $"Player Climate:\nTemp: {climate.temperature:F1}°C\nHumidity: {climate.humidity:F1}%");
#endif
        }
    }
}

// Store climate data for each chunk
public class BiomeChunkData
{
    public float temperature;
    public float humidity;

    public BiomeChunkData(float temp, float humid)
    {
        temperature = temp;
        humidity = humid;
    }
}