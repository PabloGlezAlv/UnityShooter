using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class HeightMapGenerator
{
    public static TerrainData GenerateTerrainData(int width, int height, HeightMapSettings settings, Vector2 sampleCentre, Vector2 worldCenter, float meshWorldSize, TextureData textureData)
    {
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);

        var biomeStrengths = new Vector4[width * height];
        var biomeIndexes = new Vector4[width * height];

        AnimationCurve heightCurve = settings.heightCurve;
        Vector2 topLeft = new Vector2(-1, 1) * meshWorldSize / 2f;

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        var allBiomes = BiomeSystem.GetAllBiomes();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Vector2 percent = new Vector2(x - 1, y - 1) / (width - 3);
                Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshWorldSize;
                Vector2 vertexWorldPosition = worldCenter + vertexPosition2D;

                BiomeSystem.BiomeData biomeData = BiomeSystem.GetBiomeDataForVertex(new Vector3(vertexWorldPosition.x, 0, vertexWorldPosition.y));
                var topBiomes = biomeData.influences.OrderByDescending(i => i.influence).ToList();

                float finalHeight = 0;
                if (topBiomes.Count > 0)
                {
                    float totalInfluence = 0;
                    Vector4 strengths = Vector4.zero;
                    Vector4 indexes = Vector4.zero;

                    for (int i = 0; i < Mathf.Min(4, topBiomes.Count); i++)
                    {
                        var influence = topBiomes[i];
                        var biomeDef = allBiomes.Find(b => b.name == influence.biomeName);
                        if (biomeDef != null)
                        {
                            float biomeHeight = biomeDef.heightCurve.Evaluate(values[x, y]) * biomeDef.heightMultiplier;
                            finalHeight += biomeHeight * influence.influence;
                            totalInfluence += influence.influence;

                            strengths[i] = influence.influence;
                            indexes[i] = textureData.GetBiomeTextureIndex(biomeDef.name);
                        }
                    }
                    
                    if (totalInfluence > 0)
                    {
                        finalHeight /= totalInfluence;
                        for(int i = 0; i < 4; i++)
                        {
                            strengths[i] /= totalInfluence;
                        }
                    }

                    biomeStrengths[index] = strengths;
                    biomeIndexes[index] = indexes;
                }
                else
                {
                    finalHeight = heightCurve.Evaluate(values[x, y]);
                }

                values[x, y] = finalHeight * settings.heightMultiplier;

                if (values[x, y] > maxValue)
                {
                    maxValue = values[x, y];
                }
                if (values[x, y] < minValue)
                {
                    minValue = values[x, y];
                }
            }
        }
        
        var heightMap = new HeightMap(values, minValue, maxValue);
        var biomeMap = new BiomeMap(biomeStrengths, biomeIndexes);

        return new TerrainData(heightMap, biomeMap);
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
