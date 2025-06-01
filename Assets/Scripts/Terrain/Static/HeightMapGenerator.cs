using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {

    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre, BiomeSystem.BiomeData biomeData = default)
    {
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCentre);

        // Obtener configuración del bioma si está disponible
        float biomeHeightMultiplier = 1f;
        AnimationCurve biomeCurve = settings.heightCurve;

        if (!string.IsNullOrEmpty(biomeData.biomeName))
        {
            var biomes = BiomeSystem.GetAllBiomes();
            var currentBiome = biomes.Find(b => b.name == biomeData.biomeName);
            if (currentBiome != null)
            {
                biomeHeightMultiplier = currentBiome.heightMultiplier;
                biomeCurve = new AnimationCurve(currentBiome.heightCurve.keys);
            }
        }
        else
        {
            biomeCurve = new AnimationCurve(settings.heightCurve.keys);
        }

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                // APLICAR MODIFICADORES DEL BIOMA
                values[i, j] *= biomeCurve.Evaluate(values[i, j]) * settings.heightMultiplier * biomeHeightMultiplier;

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
}

public struct HeightMap {
	public readonly float[,] values;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap (float[,] values, float minValue, float maxValue)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}

