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
        _GrassDensity ("Grass Density", Range(1, 5)) = 3            // Cuántas hojas por triángulo
        _BendAmount ("Bend Amount", Range(0, 1)) = 0.5              // Cuánto se curva el césped naturalmente
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5                 // Umbral para hacer transparente bordes
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
            
            // Función de ruido pseudo-aleatorio
            // Input: cualquier vector3, Output: número entre 0 y 1
            float rand(float3 co)
            {
                // Fórmula matemática que convierte 3 números en uno "aleatorio"
                return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.1414))) * 43758.5453);
            }
            
            // Función que calcula el movimiento del viento
            float3 windOffset(float3 worldPos, float time)
            {
                // Onda sinusoidal en X basada en posición y tiempo
                float windX = sin(time * _WindSpeed + worldPos.x * 0.1) * _WindStrength;
                // Onda cosinusoidal en Z (diferente frecuencia para más realismo)
                float windZ = cos(time * _WindSpeed * 0.7 + worldPos.z * 0.1) * _WindStrength * 0.5;
                // Solo viento horizontal (Y = 0)
                return float3(windX, 0, windZ);
            }
            
            // VERTEX SHADER: Se ejecuta para cada vértice del mesh original
            VertexOutput vert(Attributes input)
            {
                VertexOutput output;
                // Simplemente pasamos los datos al geometry shader
                output.positionOS = input.positionOS;
                output.normalOS = input.normalOS;
                output.uv = input.uv;
                // Convertimos la posición a coordenadas mundiales
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }
            
            // Se ejecuta para cada triángulo del mesh original
            [maxvertexcount(12)] // Máximo 12 vértices de salida (4 vértices × 3 hojas máximo)
            void geom(triangle VertexOutput input[3], inout TriangleStream<Varyings> triStream)
            {
                // Calculamos el centro del triángulo (donde crecerá el césped)
                float3 centerWS = (input[0].positionWS + input[1].positionWS + input[2].positionWS) / 3.0;
                
                // Calculamos la normal promedio (dirección "arriba" del césped)
                float3 normalWS = normalize((input[0].normalOS + input[1].normalOS + input[2].normalOS) / 3.0);
                normalWS = TransformObjectToWorldNormal(normalWS); // Convertir a world space
                
                // BUCLE: Generamos varias hojas de césped por triángulo
                for (int i = 0; i < _GrassDensity; i++)
                {
                    // Generamos números "aleatorios" basados en la posición
                    float r1 = rand(centerWS + i);
                    float r2 = rand(centerWS + i + 1.5);
                    
                    // Saltamos algunas hojas para distribución más natural
                    if (sqrt(r1) + r2 > 1.0) continue;
                    
                    // COORDENADAS BARICÉNTRICAS: Calculamos posición aleatoria dentro del triángulo
                    // Es una técnica matemática para puntos aleatorios en triángulos
                    float3 randomPosWS = input[0].positionWS * (1 - sqrt(r1)) + 
                                        input[1].positionWS * (sqrt(r1) * (1 - r2)) + 
                                        input[2].positionWS * (sqrt(r1) * r2);
                    
                    // VARIACIONES: Hacemos cada hoja ligeramente diferente
                    float heightVar = 0.7 + rand(randomPosWS) * 0.6;        // Altura entre 70% y 130%
                    float widthVar = 0.8 + rand(randomPosWS + 2.0) * 0.4;   // Ancho entre 80% y 120%
                    float bendVar = rand(randomPosWS + 3.0);                 // Curvatura aleatoria
                    
                    // Aplicamos las variaciones a los parámetros base
                    float grassHeight = _GrassHeight * heightVar;
                    float grassWidth = _GrassWidth * widthVar;
                    
                    // Calculamos el efecto del viento en esta posición
                    float3 wind = windOffset(randomPosWS, _Time.y); // _Time.y es tiempo en segundos
                    
                    // ORIENTACIÓN: Cada hoja mira en dirección aleatoria
                    float angle = rand(randomPosWS + 4.0) * TWO_PI; // Ángulo aleatorio (0 a 2π)
                    float3 grassDir = float3(cos(angle), 0, sin(angle)); // Vector dirección
                    // Vector perpendicular para el ancho de la hoja
                    float3 grassRight = normalize(cross(normalWS, grassDir)) * grassWidth;
                    
                    // POSICIONES: Base y punta de la hoja
                    float3 basePos = randomPosWS;                           // Base en el suelo
                    float3 tipPos = basePos + normalWS * grassHeight;       // Punta hacia arriba
                    
                    // DEFORMACIONES: Curvatura natural + viento
                    float3 bend = grassDir * _BendAmount * bendVar * grassHeight * 0.5;
                    tipPos += bend + wind * grassHeight * 0.3;
                    
                    // CREAR GEOMETRÍA: Cada hoja es un quad (4 vértices, 2 triángulos)
                    Varyings grassVerts[4];
                    
                    // Vértice 0: Base izquierda
                    grassVerts[0].positionWS = basePos - grassRight * 0.5;
                    grassVerts[0].positionCS = TransformWorldToHClip(grassVerts[0].positionWS); // Convertir para la cámara
                    grassVerts[0].normalWS = normalWS;
                    grassVerts[0].uv = float2(0, 0);    // UV: esquina inferior izquierda
                    grassVerts[0].shadowCoord = TransformWorldToShadowCoord(grassVerts[0].positionWS);
                    
                    // Vértice 1: Base derecha
                    grassVerts[1].positionWS = basePos + grassRight * 0.5;
                    grassVerts[1].positionCS = TransformWorldToHClip(grassVerts[1].positionWS);
                    grassVerts[1].normalWS = normalWS;
                    grassVerts[1].uv = float2(1, 0);    // UV: esquina inferior derecha
                    grassVerts[1].shadowCoord = TransformWorldToShadowCoord(grassVerts[1].positionWS);
                    
                    // Vértice 2: Punta izquierda (más estrecha)
                    grassVerts[2].positionWS = tipPos - grassRight * 0.1;
                    grassVerts[2].positionCS = TransformWorldToHClip(grassVerts[2].positionWS);
                    grassVerts[2].normalWS = normalWS;
                    grassVerts[2].uv = float2(0.2, 1);  // UV: cerca del borde superior izquierdo
                    grassVerts[2].shadowCoord = TransformWorldToShadowCoord(grassVerts[2].positionWS);
                    
                    // Vértice 3: Punta derecha (más estrecha)
                    grassVerts[3].positionWS = tipPos + grassRight * 0.1;
                    grassVerts[3].positionCS = TransformWorldToHClip(grassVerts[3].positionWS);
                    grassVerts[3].normalWS = normalWS;
                    grassVerts[3].uv = float2(0.8, 1);  // UV: cerca del borde superior derecho
                    grassVerts[3].shadowCoord = TransformWorldToShadowCoord(grassVerts[3].positionWS);
                    
                    // EMITIR TRIÁNGULOS: Convertimos los 4 vértices en 2 triángulos
                    // Primer triángulo: base izquierda -> base derecha -> punta izquierda
                    triStream.Append(grassVerts[0]);
                    triStream.Append(grassVerts[1]);
                    triStream.Append(grassVerts[2]);
                    triStream.RestartStrip(); // Termina el triángulo actual
                    
                    // Segundo triángulo: base derecha -> punta derecha -> punta izquierda
                    triStream.Append(grassVerts[1]);
                    triStream.Append(grassVerts[3]);
                    triStream.Append(grassVerts[2]);
                    triStream.RestartStrip(); // Termina el triángulo actual
                }
            }
            
            // FRAGMENT SHADER: Se ejecuta para cada píxel de cada hoja de césped
            half4 frag(Varyings input) : SV_Target
            {
                // GRADIENTE DE COLOR: Interpolamos entre color base y punta según UV.y
                // UV.y = 0 en la base, UV.y = 1 en la punta
                half4 grassColor = lerp(_BottomColor, _TopColor, input.uv.y);
                
                // FORMA DE HOJA: Creamos transparencia en los bordes
                // abs(input.uv.x - 0.5) va de 0 (centro) a 0.5 (bordes)
                // Multiplicado por 2 va de 0 (centro) a 1 (bordes)
                // 1.0 - eso = 1 (centro) a 0 (bordes) = forma puntiaguda
                half alpha = 1.0 - abs(input.uv.x - 0.5) * 2.0;
                
                // FADE EN LA BASE: Evitamos bordes duros donde el césped toca el suelo
                alpha *= smoothstep(0.0, 0.1, input.uv.y); // Transición suave en la base
                
                // ALPHA CUTOFF: Si el alpha es muy bajo, descartamos el píxel (transparente)
                if (alpha < _Cutoff)
                    discard;
                
                // ILUMINACIÓN URP: Obtenemos información de la luz principal
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lighting = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                
                // CÁLCULO DE ILUMINACIÓN: Producto punto entre normal y dirección de luz
                // NdotL = 1 cuando la luz llega perpendicular, 0 cuando es paralela
                half NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 diffuse = grassColor.rgb * lighting * NdotL;
                
                // LUZ AMBIENTAL: Iluminación indirecta del ambiente
                half3 ambient = SampleSH(input.normalWS) * grassColor.rgb;
                
                // COLOR FINAL: Combinamos luz directa + ambiental
                half3 finalColor = diffuse + ambient * 0.3;
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
        
        // SEGUNDO PASS: Para proyectar sombras
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On      // Escribimos profundidad para las sombras
            ZTest LEqual   // Test de profundidad estándar
            ColorMask 0    // No escribimos color (solo sombras)
            Cull Off       // No eliminamos caras traseras
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.6
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            float3 _LightDirection; // Dirección de la luz (para calcular sombras)
            
            // Structs simplificados para sombras
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
                float4 positionCS : SV_POSITION; // Solo necesitamos posición para sombras
            };
            
            VertexOutput vert(Attributes input)
            {
                VertexOutput output;
                output.positionOS = input.positionOS;
                output.normalOS = input.normalOS;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }
            
            // Geometry shader simplificado para sombras (menos calidad = mejor rendimiento)
            [maxvertexcount(12)]
            void geom(triangle VertexOutput input[3], inout TriangleStream<Varyings> triStream)
            {
                float3 centerWS = (input[0].positionWS + input[1].positionWS + input[2].positionWS) / 3.0;
                float3 normalWS = TransformObjectToWorldNormal((input[0].normalOS + input[1].normalOS + input[2].normalOS) / 3.0);
                
                // Solo 2 hojas por triángulo para sombras (menos carga)
                for (int i = 0; i < 2; i++)
                {
                    float r1 = frac(sin(dot(centerWS + i, float3(12.9898, 78.233, 53.1414))) * 43758.5453);
                    float3 grassPos = centerWS + normalWS * _GrassHeight * (0.7 + r1 * 0.6);
                    
                    Varyings output;
                    // ApplyShadowBias evita "shadow acne" (artefactos de sombra)
                    output.positionCS = TransformWorldToHClip(ApplyShadowBias(grassPos, normalWS, _LightDirection));
                    
                    // Emitimos un triángulo simple para cada hoja (para las sombras)
                    triStream.Append(output);
                    triStream.Append(output);
                    triStream.Append(output);
                    triStream.RestartStrip();
                }
            }
            
            // Fragment shader para sombras: no devuelve color
            half4 frag(Varyings input) : SV_Target
            {
                return 0; // Solo importa la profundidad, no el color
            }
            ENDHLSL
        }
    }
    
    // Fallback si algo falla
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}