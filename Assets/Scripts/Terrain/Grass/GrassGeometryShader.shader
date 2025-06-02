Shader "Universal Render Pipeline/Custom/GrassGeometryShader"
{
    // PROPIEDADES: Variables que aparecen en el Inspector del Material
    Properties
    {
        _TopColor ("Top Color", Color) = (0.1, 0.8, 0.2, 1)      // Color de la punta del césped (más claro)
        _BottomColor ("Bottom Color", Color) = (0.05, 0.4, 0.1, 1) // Color de la base del césped (más oscuro)
        _GrassHeight ("Grass Height", Range(0.1, 3.0)) = 1.0       // Altura máxima de las hojas
        _GrassWidth ("Grass Width", Range(0.01, 0.2)) = 0.05       // Ancho de las hojas en la base
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.3          // Intensidad del viento
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2.0               // Velocidad del viento
        _GrassDensity ("Grass Density", Range(1, 10)) = 3           // Cuántas hojas por triángulo (aumentado máximo)
        _BendAmount ("Bend Amount", Range(0, 1)) = 0.5              // Cuánto se curva el césped naturalmente
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5                 // Umbral para hacer transparente bordes
        
        // NUEVAS PROPIEDADES PARA LÍMITES DE ALTURA
        [Header(Height Limits)]
        _MinHeight ("Min Height", Float) = -50.0                    // Altura mínima donde crece césped
        _MaxHeight ("Max Height", Float) = 50.0                     // Altura máxima donde crece césped
        _HeightFadeRange ("Height Fade Range", Range(0.1, 20.0)) = 5.0  // Rango de transición suave
        
        // NUEVAS PROPIEDADES PARA DENSIDAD VARIABLE
        [Header(Density Variation)]
        _DensityScale ("Density Noise Scale", Range(0.01, 1.0)) = 0.1   // Escala del ruido de densidad
        _DensityStrength ("Density Variation Strength", Range(0, 2)) = 1.0  // Intensidad de la variación
        _HighDensityThreshold ("High Density Threshold", Range(0, 1)) = 0.6  // Umbral para zonas densas
    }
    
    SubShader
    {
        // TAGS: Le dicen a Unity cómo y cuándo renderizar este material
        Tags 
        { 
            "RenderType" = "Opaque"              // Es un material opaco (no transparente)
            "RenderPipeline" = "UniversalPipeline" // Solo funciona con URP
            "Queue" = "Geometry"                 // Se renderiza con otros objetos sólidos
        }
        
        // PRIMER PASS: El renderizado principal (lo que vemos)
        Pass
        {
            Name "ForwardLit"                    // Nombre del pass para debug
            Tags { "LightMode" = "UniversalForward" } // Dice que este pass maneja la iluminación principal
            
            Cull Off    // No elimina caras traseras (importante para césped que se ve desde ambos lados)
            ZWrite On   // Escribe en el depth buffer (para oclusión correcta)
            
            HLSLPROGRAM
            // PRAGMAS: Instrucciones para el compilador
            #pragma vertex vert          // Función que maneja vértices
            #pragma geometry geom        // Función que GENERA nueva geometría (el truco del césped)
            #pragma fragment frag        // Función que maneja píxeles
            #pragma target 4.6           // Versión de shaders (4.6 soporta geometry shaders)
            
            // KEYWORDS: Para que funcionen las sombras en URP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            
            // INCLUDES: Librerías de Unity con funciones útiles
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            // CBUFFER: Agrupa las propiedades para mejor rendimiento
            CBUFFER_START(UnityPerMaterial)
            float4 _TopColor;      // Los colores como float4 (RGBA)
            float4 _BottomColor;
            float _GrassHeight;    // Las propiedades numéricas como float
            float _GrassWidth;
            float _WindStrength;
            float _WindSpeed;
            int _GrassDensity;     // Densidad como entero
            float _BendAmount;
            float _Cutoff;
            // Nuevas variables
            float _MinHeight;
            float _MaxHeight;
            float _HeightFadeRange;
            float _DensityScale;
            float _DensityStrength;
            float _HighDensityThreshold;
            CBUFFER_END
            
            // STRUCTS: Definen qué datos pasan entre las etapas del shader
            
            // Attributes: Lo que llega del mesh original (vértices del plano)
            struct Attributes
            {
                float4 positionOS : POSITION;  // Posición en Object Space (espacio local del objeto)
                float3 normalOS : NORMAL;      // Normal en Object Space
                float2 uv : TEXCOORD0;         // Coordenadas UV del mesh original
            };
            
            // Varyings: Lo que va del geometry shader al fragment shader
            struct Varyings
            {
                float4 positionCS : SV_POSITION;  // Posición en Clip Space (lo que ve la cámara)
                float3 positionWS : TEXCOORD0;    // Posición en World Space (coordenadas del mundo)
                float3 normalWS : TEXCOORD1;      // Normal en World Space
                float2 uv : TEXCOORD2;            // UV coordinates para la hoja de césped
                float4 shadowCoord : TEXCOORD3;   // Coordenadas para recibir sombras
            };
            
            // VertexOutput: Lo que va del vertex shader al geometry shader
            struct VertexOutput
            {
                float4 positionOS : POSITION;     // Mantenemos posición original
                float3 normalOS : NORMAL;         // Y normal original
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;    // Pero añadimos posición mundial
            };
            
            // FUNCIONES AUXILIARES
            
            // Función de ruido pseudo-aleatorio mejorada
            float rand(float3 co)
            {
                return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.1414))) * 43758.5453);
            }
            
            // Función de ruido Perlin simplificado para variación de densidad
            float noise(float3 pos)
            {
                float3 i = floor(pos);
                float3 f = frac(pos);
                f = f * f * (3.0 - 2.0 * f); // Suavizado hermite
                
                float a = rand(i);
                float b = rand(i + float3(1, 0, 0));
                float c = rand(i + float3(0, 1, 0));
                float d = rand(i + float3(1, 1, 0));
                float e = rand(i + float3(0, 0, 1));
                float f2 = rand(i + float3(1, 0, 1));
                float g = rand(i + float3(0, 1, 1));
                float h = rand(i + float3(1, 1, 1));
                
                float x1 = lerp(a, b, f.x);
                float x2 = lerp(c, d, f.x);
                float x3 = lerp(e, f2, f.x);
                float x4 = lerp(g, h, f.x);
                
                float y1 = lerp(x1, x2, f.y);
                float y2 = lerp(x3, x4, f.y);
                
                return lerp(y1, y2, f.z);
            }
            
            // Función que calcula el factor de altura (0 = no césped, 1 = césped completo)
            float getHeightFactor(float worldY)
            {
                // Transición suave en el límite inferior
                float lowerFade = smoothstep(_MinHeight - _HeightFadeRange, _MinHeight, worldY);
                // Transición suave en el límite superior
                float upperFade = smoothstep(_MaxHeight + _HeightFadeRange, _MaxHeight, worldY);
                // Combinamos ambas transiciones
                return lowerFade * upperFade;
            }
            
            // Función que calcula el factor de densidad basado en ruido
            float getDensityFactor(float3 worldPos)
            {
                // Ruido base
                float noiseValue = noise(worldPos * _DensityScale);
                // Añadimos una octava más pequeña para más detalle
                noiseValue += noise(worldPos * _DensityScale * 3.0) * 0.5;
                noiseValue /= 1.5; // Normalizamos
                
                // Aplicamos la intensidad de variación
                float densityVariation = lerp(1.0, noiseValue, _DensityStrength);
                
                // Creamos zonas de alta densidad
                float highDensityMask = step(_HighDensityThreshold, noiseValue);
                densityVariation = lerp(densityVariation, densityVariation * 2.0, highDensityMask);
                
                return saturate(densityVariation);
            }
            
            // Función que calcula el movimiento del viento
            float3 windOffset(float3 worldPos, float time)
            {
                float windX = sin(time * _WindSpeed + worldPos.x * 0.1) * _WindStrength;
                float windZ = cos(time * _WindSpeed * 0.7 + worldPos.z * 0.1) * _WindStrength * 0.5;
                return float3(windX, 0, windZ);
            }
            
            // VERTEX SHADER: Se ejecuta para cada vértice del mesh original
            VertexOutput vert(Attributes input)
            {
                VertexOutput output;
                output.positionOS = input.positionOS;
                output.normalOS = input.normalOS;
                output.uv = input.uv;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }
            
            // GEOMETRY SHADER: Se ejecuta para cada triángulo del mesh original
            [maxvertexcount(40)] // Aumentado para permitir más hojas de césped
            void geom(triangle VertexOutput input[3], inout TriangleStream<Varyings> triStream)
            {
                // Calculamos el centro del triángulo
                float3 centerWS = (input[0].positionWS + input[1].positionWS + input[2].positionWS) / 3.0;
                
                // Verificamos si estamos en el rango de altura válido
                float heightFactor = getHeightFactor(centerWS.y);
                if (heightFactor <= 0.01) return; // No generamos césped si está fuera del rango
                
                // Calculamos el factor de densidad
                float densityFactor = getDensityFactor(centerWS);
                
                // Calculamos la densidad final combinando altura y variación de densidad
                int finalDensity = round(_GrassDensity * heightFactor * densityFactor);
                finalDensity = max(1, finalDensity); // Mínimo 1 hoja
                
                // Calculamos la normal promedio
                float3 normalWS = normalize((input[0].normalOS + input[1].normalOS + input[2].normalOS) / 3.0);
                normalWS = TransformObjectToWorldNormal(normalWS);
                
                // BUCLE: Generamos hojas de césped según la densidad calculada
                for (int i = 0; i < finalDensity && i < 10; i++) // Limitamos a 10 para rendimiento
                {
                    // Generamos números "aleatorios" basados en la posición
                    float r1 = rand(centerWS + i);
                    float r2 = rand(centerWS + i + 1.5);
                    
                    // Saltamos algunas hojas para distribución más natural
                    if (sqrt(r1) + r2 > 1.2) continue;
                    
                    // COORDENADAS BARICÉNTRICAS: posición aleatoria dentro del triángulo
                    float3 randomPosWS = input[0].positionWS * (1 - sqrt(r1)) + 
                                        input[1].positionWS * (sqrt(r1) * (1 - r2)) + 
                                        input[2].positionWS * (sqrt(r1) * r2);
                    
                    // VARIACIONES: Hacemos cada hoja ligeramente diferente
                    float heightVar = 0.7 + rand(randomPosWS) * 0.6;
                    float widthVar = 0.8 + rand(randomPosWS + 2.0) * 0.4;
                    float bendVar = rand(randomPosWS + 3.0);
                    
                    // Aplicamos el factor de altura a la variación de altura
                    heightVar *= heightFactor;
                    
                    // Aplicamos las variaciones a los parámetros base
                    float grassHeight = _GrassHeight * heightVar;
                    float grassWidth = _GrassWidth * widthVar;
                    
                    // Calculamos el efecto del viento
                    float3 wind = windOffset(randomPosWS, _Time.y);
                    
                    // ORIENTACIÓN: Cada hoja mira en dirección aleatoria
                    float angle = rand(randomPosWS + 4.0) * TWO_PI;
                    float3 grassDir = float3(cos(angle), 0, sin(angle));
                    float3 grassRight = normalize(cross(normalWS, grassDir)) * grassWidth;
                    
                    // POSICIONES: Base y punta de la hoja
                    float3 basePos = randomPosWS;
                    float3 tipPos = basePos + normalWS * grassHeight;
                    
                    // DEFORMACIONES: Curvatura natural + viento
                    float3 bend = grassDir * _BendAmount * bendVar * grassHeight * 0.5;
                    tipPos += bend + wind * grassHeight * 0.3;
                    
                    // CREAR GEOMETRÍA: Cada hoja es un quad (4 vértices, 2 triángulos)
                    Varyings grassVerts[4];
                    
                    // Vértice 0: Base izquierda
                    grassVerts[0].positionWS = basePos - grassRight * 0.5;
                    grassVerts[0].positionCS = TransformWorldToHClip(grassVerts[0].positionWS);
                    grassVerts[0].normalWS = normalWS;
                    grassVerts[0].uv = float2(0, 0);
                    grassVerts[0].shadowCoord = TransformWorldToShadowCoord(grassVerts[0].positionWS);
                    
                    // Vértice 1: Base derecha
                    grassVerts[1].positionWS = basePos + grassRight * 0.5;
                    grassVerts[1].positionCS = TransformWorldToHClip(grassVerts[1].positionWS);
                    grassVerts[1].normalWS = normalWS;
                    grassVerts[1].uv = float2(1, 0);
                    grassVerts[1].shadowCoord = TransformWorldToShadowCoord(grassVerts[1].positionWS);
                    
                    // Vértice 2: Punta izquierda (más estrecha)
                    grassVerts[2].positionWS = tipPos - grassRight * 0.1;
                    grassVerts[2].positionCS = TransformWorldToHClip(grassVerts[2].positionWS);
                    grassVerts[2].normalWS = normalWS;
                    grassVerts[2].uv = float2(0.2, 1);
                    grassVerts[2].shadowCoord = TransformWorldToShadowCoord(grassVerts[2].positionWS);
                    
                    // Vértice 3: Punta derecha (más estrecha)
                    grassVerts[3].positionWS = tipPos + grassRight * 0.1;
                    grassVerts[3].positionCS = TransformWorldToHClip(grassVerts[3].positionWS);
                    grassVerts[3].normalWS = normalWS;
                    grassVerts[3].uv = float2(0.8, 1);
                    grassVerts[3].shadowCoord = TransformWorldToShadowCoord(grassVerts[3].positionWS);
                    
                    // EMITIR TRIÁNGULOS
                    triStream.Append(grassVerts[0]);
                    triStream.Append(grassVerts[1]);
                    triStream.Append(grassVerts[2]);
                    triStream.RestartStrip();
                    
                    triStream.Append(grassVerts[1]);
                    triStream.Append(grassVerts[3]);
                    triStream.Append(grassVerts[2]);
                    triStream.RestartStrip();
                }
            }
            
            // FRAGMENT SHADER: Se ejecuta para cada píxel de cada hoja de césped
            half4 frag(Varyings input) : SV_Target
            {
                // GRADIENTE DE COLOR: Interpolamos entre color base y punta según UV.y
                half4 grassColor = lerp(_BottomColor, _TopColor, input.uv.y);
                
                // FORMA DE HOJA: Creamos transparencia en los bordes
                half alpha = 1.0 - abs(input.uv.x - 0.5) * 2.0;
                
                // FADE EN LA BASE: Evitamos bordes duros donde el césped toca el suelo
                alpha *= smoothstep(0.0, 0.1, input.uv.y);
                
                // ALPHA CUTOFF: Si el alpha es muy bajo, descartamos el píxel
                if (alpha < _Cutoff)
                    discard;
                
                // ILUMINACIÓN URP
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lighting = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 diffuse = grassColor.rgb * lighting * NdotL;
                
                // LUZ AMBIENTAL
                half3 ambient = SampleSH(input.normalWS) * grassColor.rgb;
                
                // COLOR FINAL
                half3 finalColor = diffuse + ambient * 0.3;
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
        
        // SEGUNDO PASS: Para proyectar sombras (simplificado)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.6
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // Variables necesarias para las nuevas funciones
            CBUFFER_START(UnityPerMaterial)
            float _MinHeight;
            float _MaxHeight;
            float _HeightFadeRange;
            float _GrassHeight;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct VertexOutput
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float3 positionWS : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            // Función de altura simplificada para sombras
            float getHeightFactor(float worldY)
            {
                float lowerFade = smoothstep(_MinHeight - _HeightFadeRange, _MinHeight, worldY);
                float upperFade = smoothstep(_MaxHeight + _HeightFadeRange, _MaxHeight, worldY);
                return lowerFade * upperFade;
            }
            
            VertexOutput vert(Attributes input)
            {
                VertexOutput output;
                output.positionOS = input.positionOS;
                output.normalOS = input.normalOS;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }
            
            [maxvertexcount(12)]
            void geom(triangle VertexOutput input[3], inout TriangleStream<Varyings> triStream)
            {
                float3 centerWS = (input[0].positionWS + input[1].positionWS + input[2].positionWS) / 3.0;
                float3 normalWS = TransformObjectToWorldNormal((input[0].normalOS + input[1].normalOS + input[2].normalOS) / 3.0);
                
                // Verificamos altura para sombras también
                float heightFactor = getHeightFactor(centerWS.y);
                if (heightFactor <= 0.01) return;
                
                // Solo 2 hojas por triángulo para sombras (mejor rendimiento)
                for (int i = 0; i < 2; i++)
                {
                    float r1 = frac(sin(dot(centerWS + i, float3(12.9898, 78.233, 53.1414))) * 43758.5453);
                    float3 grassPos = centerWS + normalWS * _GrassHeight * heightFactor * (0.7 + r1 * 0.6);
                    
                    Varyings output;
                    // Aplicación manual simple de bias para evitar shadow acne
                    float3 biasedPos = grassPos + normalWS * 0.01;
                    output.positionCS = TransformWorldToHClip(biasedPos);
                    
                    triStream.Append(output);
                    triStream.Append(output);
                    triStream.Append(output);
                    triStream.RestartStrip();
                }
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}