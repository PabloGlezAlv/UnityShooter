using UnityEngine;
using System.Collections;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData {

	const int textureSize = 512;
	const TextureFormat textureFormat = TextureFormat.RGB565;

	public Layer[] layers;

	float savedMinHeight;
	float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        // Establecer el número de capas
        material.SetInt("_LayerCount", layers.Length);

        // Establecer los colores base
        for (int i = 0; i < layers.Length && i < 8; i++)
        {
            material.SetColor("_BaseColors" + (i > 0 ? i.ToString() : ""), layers[i].tint);
        }

        // Preparar los vectores para alturas, mezclas, intensidad de color y escalas
        Vector4 baseStartHeights = Vector4.zero;
        Vector4 baseStartHeights1 = Vector4.zero;
        Vector4 baseBlends = Vector4.zero;
        Vector4 baseBlends1 = Vector4.zero;
        Vector4 baseColorStrength = Vector4.zero;
        Vector4 baseColorStrength1 = Vector4.zero;
        Vector4 baseTextureScales = Vector4.zero;
        Vector4 baseTextureScales1 = Vector4.zero;

        // Llenar los vectores (los primeros 4 valores van en el primer vector, los siguientes 4 en el segundo)
        for (int i = 0; i < layers.Length && i < 8; i++)
        {
            if (i < 4)
            {
                baseStartHeights[i] = layers[i].startHeight;
                baseBlends[i] = layers[i].blendStrength;
                baseColorStrength[i] = layers[i].tintStrength;
                baseTextureScales[i] = layers[i].textureScale;
            }
            else
            {
                baseStartHeights1[i - 4] = layers[i].startHeight;
                baseBlends1[i - 4] = layers[i].blendStrength;
                baseColorStrength1[i - 4] = layers[i].tintStrength;
                baseTextureScales1[i - 4] = layers[i].textureScale;
            }
        }

        // Aplicar los vectores al material
        material.SetVector("_BaseStartHeights", baseStartHeights);
        material.SetVector("_BaseStartHeights1", baseStartHeights1);
        material.SetVector("_BaseBlends", baseBlends);
        material.SetVector("_BaseBlends1", baseBlends1);
        material.SetVector("_BaseColorStrength", baseColorStrength);
        material.SetVector("_BaseColorStrength1", baseColorStrength1);
        material.SetVector("_BaseTextureScales", baseTextureScales);
        material.SetVector("_BaseTextureScales1", baseTextureScales1);

        // Generar y aplicar el array de texturas
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("_BaseTextureArray", texturesArray);

        // Aplicar colores de bioma al material
        ApplyBiomeColorsToMaterial(material);

        // Actualizar alturas del mesh
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void ApplyBiomeColorsToMaterial(Material material)
    {
        var allBiomes = BiomeSystem.GetAllBiomes();
        
        // Crear arrays de colores de bioma
        Color[] biomeColors = new Color[8];
        
        for (int i = 0; i < 8; i++)
        {
            if (i < allBiomes.Count)
            {
                biomeColors[i] = allBiomes[i].biomeColor;
            }
            else
            {
                biomeColors[i] = Color.white; // Color por defecto
            }
        }
        
        // Aplicar colores al material como vectores
        material.SetVector("_BiomeColors0", new Vector4(biomeColors[0].r, biomeColors[0].g, biomeColors[0].b, biomeColors[0].a));
        material.SetVector("_BiomeColors1", new Vector4(biomeColors[1].r, biomeColors[1].g, biomeColors[1].b, biomeColors[1].a));
        material.SetVector("_BiomeColors2", new Vector4(biomeColors[2].r, biomeColors[2].g, biomeColors[2].b, biomeColors[2].a));
        material.SetVector("_BiomeColors3", new Vector4(biomeColors[3].r, biomeColors[3].g, biomeColors[3].b, biomeColors[3].a));
        material.SetVector("_BiomeColors4", new Vector4(biomeColors[4].r, biomeColors[4].g, biomeColors[4].b, biomeColors[4].a));
        material.SetVector("_BiomeColors5", new Vector4(biomeColors[5].r, biomeColors[5].g, biomeColors[5].b, biomeColors[5].a));
        material.SetVector("_BiomeColors6", new Vector4(biomeColors[6].r, biomeColors[6].g, biomeColors[6].b, biomeColors[6].a));
        material.SetVector("_BiomeColors7", new Vector4(biomeColors[7].r, biomeColors[7].g, biomeColors[7].b, biomeColors[7].a));
    }

    public int GetBiomeTextureIndex(string biomeName)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].biomeName == biomeName)
            {
                return i;
            }
        }
        return 0; // default to the first layer if not found
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;
        material.SetFloat("_MinHeight", minHeight);
        material.SetFloat("_MaxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures) {
		Texture2DArray textureArray = new Texture2DArray (textureSize, textureSize, textures.Length, textureFormat, true);
		for (int i = 0; i < textures.Length; i++) {
			textureArray.SetPixels (textures [i].GetPixels (), i);
		}
		textureArray.Apply ();
		return textureArray;
	}

	[System.Serializable]
	public class Layer {
        public string biomeName; // Name of the biome this layer corresponds to
		public Texture2D texture;
		public Color tint;
		[Range(0,1)]
		public float tintStrength;
		[Range(0,1)]
		public float startHeight;
		[Range(0,1)]
		public float blendStrength;
		public float textureScale;
	}
		
	 
}
