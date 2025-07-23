using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "BiomeTextureSetup", menuName = "Terrain/Biome Texture Setup")]
public class BiomeTextureSetup : ScriptableObject
{
    [System.Serializable]
    public class BiomeTextureConfig
    {
        public string biomeName;
        public Texture2D texture;
        public Color tintColor = Color.white;
        [Range(0, 1)]
        public float tintStrength = 0.2f;
        [Range(0, 1)]
        public float startHeight = 0f;
        [Range(0, 1)]
        public float blendStrength = 0.1f;
        public float textureScale = 15f;
    }
    
    [Header("Biome Texture Configurations")]
    public List<BiomeTextureConfig> biomeConfigs = new List<BiomeTextureConfig>();
    
    [Header("Default Textures")]
    public Texture2D grassTexture;
    public Texture2D rockTexture;
    public Texture2D sandTexture;
    public Texture2D snowTexture;
    public Texture2D mudTexture;
    public Texture2D waterTexture;
    
    [Header("Target TextureData")]
    public TextureData targetTextureData;
    
#if UNITY_EDITOR
    [ContextMenu("Auto Setup Biome Textures")]
    public void AutoSetupBiomeTextures()
    {
        if (targetTextureData == null)
        {
            Debug.LogError("No TextureData assigned!");
            return;
        }
        
        var allBiomes = BiomeSystem.GetAllBiomes();
        if (allBiomes.Count == 0)
        {
            BiomeSystem.Initialize();
            allBiomes = BiomeSystem.GetAllBiomes();
        }
        
        // Clear existing layers
        targetTextureData.layers = new TextureData.Layer[allBiomes.Count];
        
        for (int i = 0; i < allBiomes.Count; i++)
        {
            var biome = allBiomes[i];
            var config = biomeConfigs.Find(c => c.biomeName == biome.name);
            
            targetTextureData.layers[i] = new TextureData.Layer();
            targetTextureData.layers[i].biomeName = biome.name;
            
            if (config != null)
            {
                // Use custom configuration
                targetTextureData.layers[i].texture = config.texture;
                targetTextureData.layers[i].tint = config.tintColor;
                targetTextureData.layers[i].tintStrength = config.tintStrength;
                targetTextureData.layers[i].startHeight = config.startHeight;
                targetTextureData.layers[i].blendStrength = config.blendStrength;
                targetTextureData.layers[i].textureScale = config.textureScale;
            }
            else
            {
                // Auto-assign based on biome name
                AssignDefaultTexture(targetTextureData.layers[i], biome);
            }
        }
        
        EditorUtility.SetDirty(targetTextureData);
        Debug.Log($"Successfully configured {allBiomes.Count} biome textures!");
    }
    
    private void AssignDefaultTexture(TextureData.Layer layer, BiomeDefinition biome)
    {
        // Auto-assign textures based on biome names
        switch (biome.name.ToLower())
        {
            case "océano":
            case "ocean":
                layer.texture = waterTexture ?? grassTexture;
                layer.tint = new Color(0.1f, 0.4f, 0.7f);
                layer.tintStrength = 0.8f;
                layer.textureScale = 25f;
                break;
                
            case "desierto":
            case "desert":
                layer.texture = sandTexture ?? grassTexture;
                layer.tint = new Color(0.9f, 0.7f, 0.4f);
                layer.tintStrength = 0.6f;
                layer.textureScale = 20f;
                break;
                
            case "montaña":
            case "mountain":
                layer.texture = rockTexture ?? grassTexture;
                layer.tint = new Color(0.6f, 0.6f, 0.65f);
                layer.tintStrength = 0.4f;
                layer.textureScale = 15f;
                break;
                
            case "tundra":
                layer.texture = snowTexture ?? grassTexture;
                layer.tint = new Color(0.85f, 0.9f, 0.95f);
                layer.tintStrength = 0.7f;
                layer.textureScale = 18f;
                break;
                
            case "pantano":
            case "swamp":
                layer.texture = mudTexture ?? grassTexture;
                layer.tint = new Color(0.4f, 0.5f, 0.35f);
                layer.tintStrength = 0.5f;
                layer.textureScale = 12f;
                break;
                
            default: // Llanura, Bosque, Jungla
                layer.texture = grassTexture;
                layer.tint = biome.biomeColor;
                layer.tintStrength = 0.3f;
                layer.textureScale = 15f;
                break;
        }
        
        layer.startHeight = 0f;
        layer.blendStrength = 0.1f;
    }
    
    [ContextMenu("Create Default Biome Configs")]
    public void CreateDefaultBiomeConfigs()
    {
        biomeConfigs.Clear();
        
        var allBiomes = BiomeSystem.GetAllBiomes();
        if (allBiomes.Count == 0)
        {
            BiomeSystem.Initialize();
            allBiomes = BiomeSystem.GetAllBiomes();
        }
        
        foreach (var biome in allBiomes)
        {
            var config = new BiomeTextureConfig();
            config.biomeName = biome.name;
            config.tintColor = biome.biomeColor;
            biomeConfigs.Add(config);
        }
        
        EditorUtility.SetDirty(this);
        Debug.Log($"Created {biomeConfigs.Count} default biome configurations!");
    }
#endif
} 