using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BiomeMapGenerator
{
    public static BiomeMap GenerateBiomeMap(int width, int height, Vector2 worldCenter, float meshWorldSize)
    {
        var biomeStrengths = new Vector4[width * height];
        var biomeIndexes = new Vector4[width * height];

        Vector2 topLeft = new Vector2(-1, 1) * meshWorldSize / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Vector2 percent = new Vector2(x - 1, y - 1) / (width - 3);
                Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshWorldSize;
                Vector2 vertexWorldPosition = worldCenter + vertexPosition2D;
                
                BiomeSystem.BiomeData biomeData = BiomeSystem.GetBiomeDataForVertex(new Vector3(vertexWorldPosition.x, 0, vertexWorldPosition.y));
                
                var topBiomes = biomeData.influences
                    .OrderByDescending(i => i.influence)
                    .Take(4)
                    .ToList();

                Vector4 strengths = Vector4.zero;
                Vector4 indexes = Vector4.zero;
                float totalStrength = 0;
                
                var allBiomes = BiomeSystem.GetAllBiomes();

                for(int i = 0; i < topBiomes.Count; i++)
                {
                    strengths[i] = topBiomes[i].influence;
                    totalStrength += topBiomes[i].influence;

                    var biomeDef = allBiomes.Find(b => b.name == topBiomes[i].biomeName);
                    if (biomeDef != null)
                    {
                        //This needs to be fixed to get the index from the texture data
                        //indexes[i] = allBiomes.IndexOf(biomeDef);
                        indexes[i] = 0; // temp index until texture data is available
                    }
                }
                
                //Normalize strengths
                if (totalStrength > 0)
                {
                    strengths /= totalStrength;
                }

                biomeStrengths[index] = strengths;
                biomeIndexes[index] = indexes;
            }
        }
        
        return new BiomeMap(biomeStrengths, biomeIndexes);
    }
} 