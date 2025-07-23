Shader "Custom/VolumetricCloudsURP"
{
    Properties
    {
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _Density ("Density", Range(0, 1)) = 0.5
        _NoiseScale ("Noise Scale", Float) = 1.0
        _Speed ("Speed", Float) = 0.1
        _StepSize ("Step Size", Range(0.01, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CloudColor;
                float _Density;
                float _NoiseScale;
                float _Speed;
                float _StepSize;
            CBUFFER_END

            // Simple 3D noise function
            float noise(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(input.worldPos - rayOrigin);
                float3 pos = input.worldPos;
                float density = 0.0;

                // Ray marching
                for (int i = 0; i < 50; i++)
                {
                    float3 p = pos + rayDir * _StepSize * i;
                    float n = noise(p * _NoiseScale + _Time.y * _Speed);
                    density += max(0, n - (1.0 - _Density));
                }

                float alpha = saturate(density);
                return float4(_CloudColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}