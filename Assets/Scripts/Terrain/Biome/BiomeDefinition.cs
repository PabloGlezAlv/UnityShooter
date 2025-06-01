using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    public Material terrainMaterial;           // Material específico del bioma
    public float noiseScale = 1f;              // Escala del ruido específica del bioma

    // Método para comprobar si unas condiciones climáticas pertenecen a este bioma
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