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

        // Actualizar alturas del mesh
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
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
