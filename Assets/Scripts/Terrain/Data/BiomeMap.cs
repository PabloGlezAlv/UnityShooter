using UnityEngine;

public struct BiomeMap
{
    public readonly Vector4[] biomeStrengths;
    public readonly Vector4[] biomeIndexes;

    public BiomeMap(Vector4[] biomeStrengths, Vector4[] biomeIndexes)
    {
        this.biomeStrengths = biomeStrengths;
        this.biomeIndexes = biomeIndexes;
    }
} 