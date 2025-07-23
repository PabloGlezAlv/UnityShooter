using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class BiomeFoliage
{
    public GameObject prefab;
    [Range(0, 1)]
    public float density;
}

// Define la estructura de un bioma
[System.Serializable]
public class BiomeDefinition
{
    public string name;
    public float minTemperature;
    public float maxTemperature;
    public float minHumidity;
    public float maxHumidity;
    public Color biomeColor;

    [Header("Terrain Properties")]
    public float heightMultiplier = 1f;        // Multiplicador de altura
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1); // Curva de altura del bioma
    public Material terrainMaterial;           // Material espec�fico del bioma
    public float noiseScale = 1f;              // Escala del ruido espec�fica del bioma

    [Header("Foliage")]
    public List<BiomeFoliage> foliage;

    [Header("Atmospherics")]
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;


    // Mtodo para comprobar si unas condiciones climticas pertenecen a este bioma
    public bool MatchesConditions(float temperature, float humidity)
    {
        return temperature >= minTemperature && temperature <= maxTemperature &&
               humidity >= minHumidity && humidity <= maxHumidity;
    }

    // Calcula la "distancia" a las condiciones dadas
    public float GetDistanceToBiome(float temperature, float humidity)
    {
        // Calcular el punto medio del bioma
        float midTemp = (minTemperature + maxTemperature) / 2f;
        float midHumid = (minHumidity + maxHumidity) / 2f;

        // Calcular distancia euclidiana al centro del bioma
        return Mathf.Sqrt(Mathf.Pow(temperature - midTemp, 2) +
                         Mathf.Pow(humidity - midHumid, 2));
    }
}