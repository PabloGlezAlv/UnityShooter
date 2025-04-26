using UnityEngine;

public class BiomeGenerator : MonoBehaviour
{
    public BiomeSettings[] biomes;
    public NoiseSettings temperatureNoiseSettings;
    public NoiseSettings moistureNoiseSettings;
    public int seed = 0;

    private System.Random prng;

    public void Initialize()
    {
        prng = new System.Random(seed);
        // Inicializar settings de ruido con semillas diferentes pero determinísticas
        temperatureNoiseSettings.seed = seed;
        moistureNoiseSettings.seed = seed + 1;
    }

    public BiomeSettings GetBiomeAt(Vector2 worldPosition)
    {
        // Generar valores de temperatura y humedad basados en la posición mundial
        float temperature = Mathf.Clamp01(NoiseGenerator.GenerateNoise(worldPosition, temperatureNoiseSettings));
        float moisture = Mathf.Clamp01(NoiseGenerator.GenerateNoise(worldPosition, moistureNoiseSettings));

        // Encontrar el bioma correspondiente
        foreach (BiomeSettings biome in biomes)
        {
            if (biome.ContainsPoint(temperature, moisture))
            {
                return biome;
            }
        }

        // Bioma por defecto (el primero) si ninguno coincide
        Debug.LogWarning("No matching biome found for temperature: " + temperature + " and moisture: " + moisture);
        return biomes.Length > 0 ? biomes[0] : null;
    }

    // Método para visualizar el mapa de biomas
    public Texture2D GenerateBiomeMap(int width, int height, float scale)
    {
        Texture2D biomeMap = new Texture2D(width, height);
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 pos = new Vector2(x, y) * scale;
                BiomeSettings biome = GetBiomeAt(pos);
                colorMap[y * width + x] = biome != null ? biome.baseColor : Color.black;
            }
        }

        biomeMap.SetPixels(colorMap);
        biomeMap.Apply();
        return biomeMap;
    }
}