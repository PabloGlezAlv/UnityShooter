Shader "Universal Render Pipeline/Custom/TerrainLayered"
{
    Properties
    {
        _MinHeight ("Min Height", Float) = 0
        _MaxHeight ("Max Height", Float) = 10
        [IntRange] _LayerCount ("Layer Count", Range(1,8)) = 3
        
        [Header(Layer Arrays)]
        [HideInInspector] _BaseColorCount ("Base Color Count", Integer) = 8
        _BaseColors ("Base Colors", Color) = (1,1,1,1)
        _BaseColors1 ("Base Colors 1", Color) = (1,1,1,1)
        _BaseColors2 ("Base Colors 2", Color) = (1,1,1,1)
        _BaseColors3 ("Base Colors 3", Color) = (1,1,1,1)
        _BaseColors4 ("Base Colors 4", Color) = (1,1,1,1)
        _BaseColors5 ("Base Colors 5", Color) = (1,1,1,1)
        _BaseColors6 ("Base Colors 6", Color) = (1,1,1,1)
        _BaseColors7 ("Base Colors 7", Color) = (1,1,1,1)
        
        _BaseStartHeights ("Base Start Heights", Vector) = (0, 0, 0, 0)
        _BaseStartHeights1 ("Base Start Heights 1", Vector) = (0, 0, 0, 0)
        
        _BaseBlends ("Base Blends", Vector) = (0, 0, 0, 0)
        _BaseBlends1 ("Base Blends 1", Vector) = (0, 0, 0, 0)
        
        _BaseColorStrength ("Base Color Strength", Vector) = (1, 1, 1, 1)
        _BaseColorStrength1 ("Base Color Strength 1", Vector) = (1, 1, 1, 1)
        
        _BaseTextureScales ("Base Texture Scales", Vector) = (1, 1, 1, 1)
        _BaseTextureScales1 ("Base Texture Scales 1", Vector) = (1, 1, 1, 1)
        
        [Header(Biome Colors)]
        _BiomeColors0 ("Biome Color 0", Vector) = (1, 1, 1, 1)
        _BiomeColors1 ("Biome Color 1", Vector) = (1, 1, 1, 1)
        _BiomeColors2 ("Biome Color 2", Vector) = (1, 1, 1, 1)
        _BiomeColors3 ("Biome Color 3", Vector) = (1, 1, 1, 1)
        _BiomeColors4 ("Biome Color 4", Vector) = (1, 1, 1, 1)
        _BiomeColors5 ("Biome Color 5", Vector) = (1, 1, 1, 1)
        _BiomeColors6 ("Biome Color 6", Vector) = (1, 1, 1, 1)
        _BiomeColors7 ("Biome Color 7", Vector) = (1, 1, 1, 1)
        
        [Header(Textures)]
        [NoScaleOffset] _BaseTextureArray ("Base Texture Array", 2DArray) = "white" {}
        
        [Header(Biome Blending)]
        _BiomeColorStrength ("Biome Color Strength", Range(0, 1)) = 0.5
        _TextureStrength ("Texture Strength", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1;
                float4 uv3 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 biomeStrengths : TEXCOORD2;
                float4 biomeIndexes : TEXCOORD3;
            };
            
            TEXTURE2D_ARRAY(_BaseTextureArray);
            SAMPLER(sampler_BaseTextureArray);
            
            CBUFFER_START(UnityPerMaterial)
                float _MinHeight;
                float _MaxHeight;
                int _LayerCount;
                int _BaseColorCount;
                float4 _BaseColors[8];
                float4 _BaseStartHeights;
                float4 _BaseStartHeights1;
                float4 _BaseBlends;
                float4 _BaseBlends1;
                float4 _BaseColorStrength;
                float4 _BaseColorStrength1;
                float4 _BaseTextureScales;
                float4 _BaseTextureScales1;
                float4 _BiomeColors0;
                float4 _BiomeColors1;
                float4 _BiomeColors2;
                float4 _BiomeColors3;
                float4 _BiomeColors4;
                float4 _BiomeColors5;
                float4 _BiomeColors6;
                float4 _BiomeColors7;
                float _BiomeColorStrength;
                float _TextureStrength;
            CBUFFER_END
            
            float InverseLerp(float a, float b, float value)
            {
                return saturate((value - a) / (b - a));
            }
            
            float3 TriplanarMapping(float3 worldPos, float scale, float3 blendAxes, int textureIndex)
            {
                float3 scaledWorldPos = worldPos / scale;
                
                float3 xProjection = SAMPLE_TEXTURE2D_ARRAY(
                    _BaseTextureArray, 
                    sampler_BaseTextureArray, 
                    float2(scaledWorldPos.y, scaledWorldPos.z), 
                    textureIndex
                ).rgb * blendAxes.x;
                
                float3 yProjection = SAMPLE_TEXTURE2D_ARRAY(
                    _BaseTextureArray, 
                    sampler_BaseTextureArray, 
                    float2(scaledWorldPos.x, scaledWorldPos.z), 
                    textureIndex
                ).rgb * blendAxes.y;
                
                float3 zProjection = SAMPLE_TEXTURE2D_ARRAY(
                    _BaseTextureArray, 
                    sampler_BaseTextureArray, 
                    float2(scaledWorldPos.x, scaledWorldPos.y), 
                    textureIndex
                ).rgb * blendAxes.z;
                
                return xProjection + yProjection + zProjection;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.biomeStrengths = input.uv2;
                output.biomeIndexes = input.uv3;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Preparar variables
                float3 blendAxes = abs(input.normalWS);
                blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
                
                // Array de colores de bioma
                float4 biomeColors[8];
                biomeColors[0] = _BiomeColors0;
                biomeColors[1] = _BiomeColors1;
                biomeColors[2] = _BiomeColors2;
                biomeColors[3] = _BiomeColors3;
                biomeColors[4] = _BiomeColors4;
                biomeColors[5] = _BiomeColors5;
                biomeColors[6] = _BiomeColors6;
                biomeColors[7] = _BiomeColors7;
                
                // Construir array de escalas de textura
                float baseTextureScales[8];
                baseTextureScales[0] = _BaseTextureScales.x;
                baseTextureScales[1] = _BaseTextureScales.y;
                baseTextureScales[2] = _BaseTextureScales.z;
                baseTextureScales[3] = _BaseTextureScales.w;
                baseTextureScales[4] = _BaseTextureScales1.x;
                baseTextureScales[5] = _BaseTextureScales1.y;
                baseTextureScales[6] = _BaseTextureScales1.z;
                baseTextureScales[7] = _BaseTextureScales1.w;

                // Calcular el color final basado en el blending de biomas
                float3 finalColor = float3(0,0,0);
                float3 finalTexture = float3(0,0,0);
                
                for(int i = 0; i < 4; i++)
                {
                    int biomeIndex = (int)input.biomeIndexes[i];
                    float strength = input.biomeStrengths[i];
                    
                    if(strength > 0.001 && biomeIndex < 8)
                    {
                        // Obtener color del bioma
                        float3 biomeColor = biomeColors[biomeIndex].rgb;
                        
                        // Obtener textura con triplanar mapping
                        float scale = baseTextureScales[biomeIndex];
                        float3 textureColor = TriplanarMapping(
                            input.positionWS, 
                            scale, 
                            blendAxes, 
                            biomeIndex
                        );
                        
                        // Mezclar color de bioma con textura
                        float3 blendedColor = lerp(textureColor, biomeColor * textureColor, _BiomeColorStrength);
                        blendedColor = lerp(biomeColor, blendedColor, _TextureStrength);
                        
                        finalColor += blendedColor * strength;
                    }
                }
                
                // Aplicar iluminación básica
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalColor;
                surfaceData.metallic = 0;
                surfaceData.smoothness = 0.5;
                
                return UniversalFragmentPBR(lightingInput, surfaceData);
            }
            ENDHLSL
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Define las estructuras
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
        
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
        
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
        
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}