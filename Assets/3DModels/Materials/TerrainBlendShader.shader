Shader "Custom/TerrainBlendShader"
{
    Properties
    {
        _BiomeTexArray ("Biome Textures Array", 2DArray) = "white" {}
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _TextureScale ("Texture Scale", Float) = 10.0
        [Toggle(_VERTEXCOLOR_ON)] _UseVertexColor ("Use Vertex Color", Float) = 1
        _NormalInfluence ("Normal Blend Influence", Range(0,1)) = 0.2
        _BlendSharpness ("Blend Sharpness", Range(1,20)) = 8.0
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
            #pragma shader_feature_local _VERTEXCOLOR_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D_ARRAY(_BiomeTexArray);
            SAMPLER(sampler_BiomeTexArray);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Glossiness;
                float _Metallic;
                float _TextureScale;
                float _NormalInfluence;
                float _BlendSharpness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Transformaciones de espacio
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.uv = input.uv;
                
                // Transmitir el color del vértice que contiene la información de bioma
                output.color = input.color;
                
                return output;
            }

            // Función para convertir colores a pesos de bioma
            float4 GetBiomeWeights(float3 vertexColor)
            {
                // Extraer componentes RGB para 3 biomas principales
                float3 weights = vertexColor.rgb;
                
                // Aplicar función de potencia para hacer más pronunciadas las transiciones
                weights = pow(weights, _BlendSharpness);
                
                // Normalizar para asegurar que suman 1
                float sum = weights.r + weights.g + weights.b;
                if (sum > 0)
                    weights /= sum;
                
                // El cuarto componente puede usarse para un cuarto bioma o quedarse en cero
                return float4(weights, 0);
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Coordenadas UV basadas en posición mundial para mejor escalado
                float2 scaledUV = input.positionWS.xz / _TextureScale;
                
                // Muestrear textura base con las UVs escaladas
                float4 baseAlbedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scaledUV) * _Color;
                
                float4 finalColor = baseAlbedo;
                
                #ifdef _VERTEXCOLOR_ON
                    // Convertir colores de vértice a pesos de bioma
                    float4 biomeWeights = GetBiomeWeights(input.color.rgb);
                    
                    // Para esta implementación simple, usamos:
                    // R = peso del bioma 0 (por ejemplo, agua)
                    // G = peso del bioma 1 (por ejemplo, arena)
                    // B = peso del bioma 2 (por ejemplo, hierba)
                    
                    // Si estuvieras usando un TextureArray real, aquí muestrearíamos cada textura
                    // y las mezclaríamos según los pesos
                    
                    // Por ahora, simulamos el color mezclando el color del vértice con la textura base
                    finalColor.rgb = lerp(baseAlbedo.rgb, input.color.rgb, 0.7f);
                    
                    // Alternativa con Texture Array:
                    /*
                    // Muestrear las diferentes texturas de bioma del array
                    float4 biomeColor0 = SAMPLE_TEXTURE2D_ARRAY(_BiomeTexArray, sampler_BiomeTexArray, scaledUV, 0);
                    float4 biomeColor1 = SAMPLE_TEXTURE2D_ARRAY(_BiomeTexArray, sampler_BiomeTexArray, scaledUV, 1);
                    float4 biomeColor2 = SAMPLE_TEXTURE2D_ARRAY(_BiomeTexArray, sampler_BiomeTexArray, scaledUV, 2);
                    
                    // Mezclar texturas según pesos
                    finalColor = biomeColor0 * biomeWeights.r +
                                 biomeColor1 * biomeWeights.g +
                                 biomeColor2 * biomeWeights.b;
                    */
                #endif
                
                // Calcular normales basadas en la derivada de la posición
                float3 ddxPos = ddx(input.positionWS);
                float3 ddyPos = ddy(input.positionWS);
                float3 crossNormal = normalize(cross(ddyPos, ddxPos));
                
                // Preparar datos para el modelo de iluminación
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                
                // Mezclar la normal calculada con la normal original del modelo
                float3 blendedNormal = normalize(lerp(input.normalWS, crossNormal, _NormalInfluence));
                lightingInput.normalWS = blendedNormal;
                
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                // Configuración de niebla y datos adicionales que URP necesita
                #if UNITY_VERSION >= 202120
                    lightingInput.positionCS = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    lightingInput.positionCS = 0;
                #endif
                
                lightingInput.fogCoord = ComputeFogFactor(input.positionHCS.z);
                lightingInput.vertexLighting = half3(0, 0, 0);
                lightingInput.bakedGI = half3(0, 0, 0); // En un shader completo, aquí usarías SampleSH
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionHCS);
                lightingInput.shadowMask = half4(1, 1, 1, 1);
                
                // Propiedades de la superficie para PBR
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = _Glossiness;
                surfaceData.occlusion = 1.0;
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.alpha = finalColor.a;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;
                
                // Calcular iluminación usando el modelo PBR de URP
                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                
                // Aplicar niebla
                color.rgb = MixFog(color.rgb, lightingInput.fogCoord);
                
                return color;
            }
            ENDHLSL
        }
        
        // Shadow caster pass (sin cambios)
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Glossiness;
                float _Metallic;
                float _TextureScale;
                float _NormalInfluence;
                float _BlendSharpness;
            CBUFFER_END
            
            float3 _LightDirection;
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
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
            
            half4 frag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        // Depth only pass (sin cambios)
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Glossiness;
                float _Metallic;
                float _TextureScale;
                float _NormalInfluence;
                float _BlendSharpness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 frag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}