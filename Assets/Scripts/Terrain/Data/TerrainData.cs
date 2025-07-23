public struct TerrainData
{
    public readonly HeightMap heightMap;
    public readonly BiomeMap biomeMap;

    public TerrainData(HeightMap heightMap, BiomeMap biomeMap)
    {
        this.heightMap = heightMap;
        this.biomeMap = biomeMap;
    }
} 