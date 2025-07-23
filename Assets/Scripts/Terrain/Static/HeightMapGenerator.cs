using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre, Vector2 worldCenter, float meshWorldSize)
    {
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);

        AnimationCurve heightCurve = settings.heightCurve;
        Vector2 topLeft = new Vector2(-1, 1) * meshWorldSize / 2f;

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                // Calcular posición exactamente igual que en MeshGenerator
                Vector2 percent = new Vector2(i - 1, j - 1) / (width - 3);
                Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshWorldSize;
                Vector2 vertexWorldPosition = worldCenter + vertexPosition2D;
                
                // Aplicar suavizado espacial muestreando posiciones vecinas
                BiomeSystem.BiomeData blendedBiomeData = GetSmoothedBiomeData(vertexWorldPosition, meshWorldSize / width * 2f);

                float finalHeight = 0;
                if (blendedBiomeData.influences != null && blendedBiomeData.influences.Count > 0)
                {
                    float totalInfluence = 0;
                    foreach (var influence in blendedBiomeData.influences)
                    {
                        var biomeDef = BiomeSystem.GetAllBiomes().Find(b => b.name == influence.biomeName);
                        if (biomeDef != null)
                        {
                            float biomeHeight = biomeDef.heightCurve.Evaluate(values[i, j]) * biomeDef.heightMultiplier;
                            finalHeight += biomeHeight * influence.influence;
                            totalInfluence += influence.influence;
                        }
                    }

                    if (totalInfluence > 0)
                    {
                        finalHeight /= totalInfluence;
                    }
                }
                else
                {
                    finalHeight = heightCurve.Evaluate(values[i, j]);
                }

                values[i, j] = finalHeight * settings.heightMultiplier;

                if (values[i, j] > maxValue)
                {
                    maxValue = values[i, j];
                }
                if (values[i, j] < minValue)
                {
                    minValue = values[i, j];
                }
            }
        }

        return new HeightMap(values, minValue, maxValue);
    }

    private static BiomeSystem.BiomeData GetSmoothedBiomeData(Vector2 centerPosition, float smoothingRadius)
    {
        // Muestrear múltiples posiciones alrededor del centro
        Vector2[] sampleOffsets = {
            Vector2.zero,
            new Vector2(smoothingRadius, 0),
            new Vector2(-smoothingRadius, 0),
            new Vector2(0, smoothingRadius),
            new Vector2(0, -smoothingRadius),
            new Vector2(smoothingRadius * 0.7f, smoothingRadius * 0.7f),
            new Vector2(-smoothingRadius * 0.7f, smoothingRadius * 0.7f),
            new Vector2(smoothingRadius * 0.7f, -smoothingRadius * 0.7f),
            new Vector2(-smoothingRadius * 0.7f, -smoothingRadius * 0.7f)
        };

        float[] sampleWeights = { 2f, 1f, 1f, 1f, 1f, 0.5f, 0.5f, 0.5f, 0.5f };

        var combinedInfluences = new Dictionary<string, float>();
        float totalSampleWeight = 0f;

        for (int i = 0; i < sampleOffsets.Length; i++)
        {
            Vector2 samplePos = centerPosition + sampleOffsets[i];
            BiomeSystem.BiomeData sampleData = BiomeSystem.GetBiomeData(new Vector3(samplePos.x, 0, samplePos.y));
            
            if (sampleData.influences != null)
            {
                foreach (var influence in sampleData.influences)
                {
                    if (!combinedInfluences.ContainsKey(influence.biomeName))
                        combinedInfluences[influence.biomeName] = 0f;
                    
                    combinedInfluences[influence.biomeName] += influence.influence * sampleWeights[i];
                }
            }
            totalSampleWeight += sampleWeights[i];
        }

        // Normalizar las influencias combinadas
        var finalInfluences = new List<BiomeSystem.BiomeInfluence>();
        foreach (var kvp in combinedInfluences)
        {
            finalInfluences.Add(new BiomeSystem.BiomeInfluence
            {
                biomeName = kvp.Key,
                influence = kvp.Value / totalSampleWeight
            });
        }

        // Obtener temperatura y humedad del centro
        BiomeSystem.BiomeData centerData = BiomeSystem.GetBiomeData(new Vector3(centerPosition.x, 0, centerPosition.y));

        return new BiomeSystem.BiomeData
        {
            temperature = centerData.temperature,
            humidity = centerData.humidity,
            influences = finalInfluences
        };
    }
}

public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
