Shader "VTZ/FoW_SketchOverlay"
{
    Properties
    {
        _MainTex ("Sketch Texture", 2D) = "white" {}
        _ExploredMask ("Explored Mask", 2D) = "black" {}
        _VisionMask ("Vision Mask", 2D) = "black" {}
        _WorldMin ("World Min (XY)", Vector) = (-50, -50, 0, 0)
        _WorldMax ("World Max (XY)", Vector) = (50, 50, 0, 0)
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FoWSketchOverlay"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_ExploredMask);
            SAMPLER(sampler_ExploredMask);

            TEXTURE2D(_VisionMask);
            SAMPLER(sampler_VisionMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _WorldMin;
                float4 _WorldMax;
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 maskUV = (input.positionWS.xy - _WorldMin.xy)
                              / (_WorldMax.xy - _WorldMin.xy);

                half explored = 0;
                half vision = 0;

                if (maskUV.x >= 0 && maskUV.x <= 1 && maskUV.y >= 0 && maskUV.y <= 1)
                {
                    explored = SAMPLE_TEXTURE2D(_ExploredMask, sampler_ExploredMask, maskUV).r;
                    vision = SAMPLE_TEXTURE2D(_VisionMask, sampler_VisionMask, maskUV).r;
                }

                // 탐험 완료 AND 시야 밖 = 스케치 보임
                // explored=1, vision=0 → alpha=1 (스케치 표시)
                // explored=1, vision=1 → alpha=0 (풀컬러 노출)
                // explored=0           → alpha=0 (블랙이 덮으므로 불필요)
                half visibility = explored * (1.0 - vision);

                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                mainColor *= input.color * _Color;
                mainColor.a *= visibility;

                return mainColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
