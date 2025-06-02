using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre, BiomeSystem.BiomeData biomeData = default)
    {
        //   … (misma lógica de bufferSize, bufferedValues, etc. que antes) …
        int bufferSize = Mathf.Max(Mathf.RoundToInt(width * 0.3f), 16);
        int bufferedWidth = width + (bufferSize * 2);
        int bufferedHeight = height + (bufferSize * 2);

        Vector2 bufferedSampleCentre = sampleCentre - new Vector2(bufferSize, bufferSize) * (settings.noiseSettings.scale / width);
        float[,] bufferedValues = Noise.GenerateNoiseMap(bufferedWidth, bufferedHeight, settings.noiseSettings, bufferedSampleCentre);

        Dictionary<Vector2Int, BiomeSystem.BiomeData> neighborBiomes = GetNeighborBiomes(sampleCentre, settings.noiseSettings.scale);

        float[,] values = new float[width, height];
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int bufferedI = i + bufferSize;
                int bufferedJ = j + bufferSize;

                float blendFactor = CalculateBlendFactor(i, j, width, height, bufferSize);

                float heightValue = ApplyInterpolatedBiomeModifiers(
                    bufferedValues[bufferedI, bufferedJ],
                    biomeData,
                    neighborBiomes,
                    blendFactor,
                    i, j, width, height,
                    sampleCentre,
                    settings
                );

                values[i, j] = heightValue;
                if (heightValue > maxValue) maxValue = heightValue;
                if (heightValue < minValue) minValue = heightValue;
            }
        }

        return new HeightMap(values, minValue, maxValue);
    }

    private static Dictionary<Vector2Int, BiomeSystem.BiomeData> GetNeighborBiomes(Vector2 sampleCentre, float noiseScale)
    {
        return BiomeSystem.GetNeighborBiomesWithCache(sampleCentre);
    }

    private static float CalculateBlendFactor(int x, int y, int width, int height, int bufferSize)
    {
        float distanceToLeft = x;
        float distanceToRight = width - 1 - x;
        float distanceToBottom = y;
        float distanceToTop = height - 1 - y;

        float minDistanceToEdge = Mathf.Min(distanceToLeft, distanceToRight, distanceToBottom, distanceToTop);
        float blendZone = bufferSize * 1.2f; // zona ligeramente mayor que el buffer

        if (minDistanceToEdge >= blendZone)
            return 0f;

        float t = minDistanceToEdge / blendZone;
        return 1f - (t * t * (3f - 2f * t)); // smoothstep
    }

    private static float ApplyInterpolatedBiomeModifiers(
        float noiseValue,
        BiomeSystem.BiomeData centerBiome,
        Dictionary<Vector2Int, BiomeSystem.BiomeData> neighborBiomes,
        float blendFactor,
        int x, int y, int width, int height,
        Vector2 sampleCentre,
        HeightMapSettings settings)
    {
        // --- 1) Obtener curva y multiplicador del bioma central
        float centerHeightMultiplier = 1f;
        AnimationCurve centerCurve = settings.heightCurve;

        if (!string.IsNullOrEmpty(centerBiome.biomeName))
        {
            var biomes = BiomeSystem.GetAllBiomes();
            var currentBiome = biomes.Find(b => b.name == centerBiome.biomeName);
            if (currentBiome != null)
            {
                centerHeightMultiplier = currentBiome.heightMultiplier;
                centerCurve = currentBiome.heightCurve;
                // Si quisieras variar la escala de ruido por bioma, podrías hacer algo así:
                // settings.noiseSettings.scale = currentBiome.noiseScale;
            }
        }

        // Sin blending: devolvemos altura simple
        if (blendFactor <= 0.001f)
        {
            return ApplyBiomeHeight(noiseValue, centerHeightMultiplier, centerCurve, settings.heightMultiplier);
        }

        // --- 2) Calcular posición mundial del píxel (x,j) dentro del chunk
        float normalizedX = (float)x / (width - 1);
        float normalizedY = (float)y / (height - 1);
        float chunkWorldSize = BiomeSystem.chunkSize;
        Vector2 chunkCorner = sampleCentre - new Vector2(chunkWorldSize * 0.5f, chunkWorldSize * 0.5f);
        Vector2 worldPos = chunkCorner + new Vector2(normalizedX * chunkWorldSize, normalizedY * chunkWorldSize);

        // --- 3) Obtener vecinos relevantes y sus pesos basados en distancia real
        var relevantNeighbors = GetRelevantNeighborsForPosition(worldPos, sampleCentre, neighborBiomes);

        if (relevantNeighbors.Count == 0)
        {
            return ApplyBiomeHeight(noiseValue, centerHeightMultiplier, centerCurve, settings.heightMultiplier);
        }

        // --- 4) Mezcla progresiva de curvas de altura
        float totalWeight = 0f;
        float baseHeight = noiseValue * settings.heightMultiplier;

        // Evaluación del bioma central
        float centerEval = centerCurve.Evaluate(noiseValue) * centerHeightMultiplier;
        float weightedEval = centerEval * (1f - blendFactor);
        totalWeight += (1f - blendFactor);

        // Iterar vecinos
        foreach (var kv in relevantNeighbors)
        {
            Vector2Int offset = kv.Key;       // offset del chunk vecino
            float neighborWeight = kv.Value * blendFactor;

            if (neighborBiomes.TryGetValue(offset, out BiomeSystem.BiomeData neighborBiome))
            {
                var allBiomes = BiomeSystem.GetAllBiomes();
                var neighborDef = allBiomes.Find(b => b.name == neighborBiome.biomeName);

                float neighborHeightMultiplier = neighborDef?.heightMultiplier ?? 1f;
                AnimationCurve neighborCurve = neighborDef?.heightCurve ?? settings.heightCurve;

                // Evaluación de la curva del bioma vecino
                float neighborEval = neighborCurve.Evaluate(noiseValue) * neighborHeightMultiplier;
                weightedEval += neighborEval * neighborWeight;
                totalWeight += neighborWeight;
            }
        }

        // Altura final = ruido básico * (evaluación ponderada / suma de pesos)
        return (totalWeight > 0f)
            ? baseHeight * (weightedEval / totalWeight)
            : ApplyBiomeHeight(noiseValue, centerHeightMultiplier, centerCurve, settings.heightMultiplier);
    }

    private static float ApplyBiomeHeight(float noiseValue, float biomeHeightMultiplier, AnimationCurve biomeCurve, float settingsHeightMultiplier)
    {
        return noiseValue * biomeCurve.Evaluate(noiseValue) * settingsHeightMultiplier * biomeHeightMultiplier;
    }

    /// <summary>
    /// Construye un diccionario con offsets a cada vecino (–1,0,1) y el peso  
    /// proporcional a la distancia real entre worldPos y el centro de ese chunk vecino.
    /// </summary>
    private static Dictionary<Vector2Int, float> GetRelevantNeighborsForPosition(
        Vector2 worldPos,
        Vector2 sampleCentre,
        Dictionary<Vector2Int, BiomeSystem.BiomeData> neighborBiomes)
    {
        var relevantNeighbors = new Dictionary<Vector2Int, float>();

        // 1) Obtener el chunkCoord del chunk central
        Vector2Int centerChunkCoord = BiomeSystem.WorldToChunkCoord(sampleCentre);

        // 2) Distancia máxima posible (diagonal de un chunk)
        float maxDist = BiomeSystem.chunkSize * Mathf.Sqrt(2f);

        // 3) Iterar offsets: [–1,0,1] × [–1,0,1]
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                Vector2Int offset = new Vector2Int(dx, dz);
                if (dx == 0 && dz == 0) continue; // el propio chunk central no es “vecino”

                // 4) Calcular coord del chunk vecino
                Vector2Int neighborChunkCoord = new Vector2Int(centerChunkCoord.x + dx, centerChunkCoord.y + dz);

                // 5) Obtener posición mundial del centro de ese chunk vecino
                Vector2 neighborCenterWorld = BiomeSystem.ChunkToWorldCoord(neighborChunkCoord);

                // 6) Calcular distancia entre worldPos (píxel) y ese centro
                float dist = Vector2.Distance(worldPos, neighborCenterWorld);

                // 7) Peso inverso a la distancia, normalizado [0..1]
                float weight = Mathf.Clamp01(1f - (dist / maxDist));
                if (weight > 0f && neighborBiomes.ContainsKey(offset))
                {
                    relevantNeighbors[offset] = weight;
                }
            }
        }

        return relevantNeighbors;
    }

    // Métodos obsoletos (se mantienen para compatibilidad, pero no se usarán)
    [System.Obsolete("Usar GetRelevantNeighborsForPosition en su lugar")]
    private static Vector2Int[] GetRelevantNeighbors(int x, int y, int width, int height)
    {
        List<Vector2Int> relevant = new List<Vector2Int>();
        relevant.Add(new Vector2Int(0, 0));

        bool nearLeft = x < width * 0.2f;
        bool nearRight = x > width * 0.8f;
        bool nearBottom = y < height * 0.2f;
        bool nearTop = y > height * 0.8f;

        if (nearLeft) relevant.Add(new Vector2Int(-1, 0));
        if (nearRight) relevant.Add(new Vector2Int(1, 0));
        if (nearBottom) relevant.Add(new Vector2Int(0, -1));
        if (nearTop) relevant.Add(new Vector2Int(0, 1));

        if (nearLeft && nearBottom) relevant.Add(new Vector2Int(-1, -1));
        if (nearRight && nearBottom) relevant.Add(new Vector2Int(1, -1));
        if (nearLeft && nearTop) relevant.Add(new Vector2Int(-1, 1));
        if (nearRight && nearTop) relevant.Add(new Vector2Int(1, 1));

        return relevant.ToArray();
    }

    [System.Obsolete("Usar GetRelevantNeighborsForPosition en su lugar")]
    private static float CalculateNeighborWeight(Vector2Int neighborOffset, int x, int y, int width, int height)
    {
        float distance = Mathf.Sqrt(neighborOffset.x * neighborOffset.x + neighborOffset.y * neighborOffset.y);
        if (distance == 0) return 1f;
        return 1f / (distance * distance);
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
