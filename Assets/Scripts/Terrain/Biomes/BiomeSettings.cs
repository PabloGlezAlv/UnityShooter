using UnityEngine;

[System.Serializable]
public class BiomeSettings
{
    [Header("Biome Identification")]
    public string biomeName;
    [Range(0f, 1f)]
    public float minTemperature;
    [Range(0f, 1f)]
    public float maxTemperature;
    [Range(0f, 1f)]
    public float minMoisture;
    [Range(0f, 1f)]
    public float maxMoisture;

    [Header("Terrain Configuration")]
    public float heightMultiplier = 1f;
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Visual Settings")]
    public Color baseColor = Color.white;
    [Range(0f, 1f)]
    public float waterLevel = 0.3f;
    public Material terrainMaterial;
    public Material waterMaterial;

    [Header("Details")]
    public float treeDensity = 0f;
    public GameObject[] treePrefabs;
    public float grassDensity = 0f;
    public GameObject[] grassPrefabs;
    public float rockDensity = 0f;
    public GameObject[] rockPrefabs;

    // Método para comprobar si un punto con cierta temperatura y humedad pertenece a este bioma
    public bool ContainsPoint(float temperature, float moisture)
    {
        return temperature >= minTemperature && temperature <= maxTemperature
            && moisture >= minMoisture && moisture <= maxMoisture;
    }
}