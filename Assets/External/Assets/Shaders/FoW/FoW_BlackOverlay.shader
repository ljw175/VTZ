Shader "VTZ/FoW_BlackOverlay"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ExploredMask ("Explored Mask", 2D) = "black" {}
        _WorldMin ("World Min (XY)", Vector) = (-50, -50, 0, 0)
        _WorldMax ("World Max (XY)", Vector) = (50, 50, 0, 0)
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FoWBlackOverlay"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_ExploredMask);
            SAMPLER(sampler_ExploredMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _WorldMin;
                float4 _WorldMax;
                float4 _FogColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 maskUV = (input.positionWS.xy - _WorldMin.xy)
                              / (_WorldMax.xy - _WorldMin.xy);

                // 마스크 범위 밖은 완전 불투명 (미탐험)
                half explored = 0;
                if (maskUV.x >= 0 && maskUV.x <= 1 && maskUV.y >= 0 && maskUV.y <= 1)
                    explored = SAMPLE_TEXTURE2D(_ExploredMask, sampler_ExploredMask, maskUV).r;

                // 미탐험 = 불투명, 탐험 완료 = 투명
                half4 col = _FogColor;
                col.a = 1.0 - explored;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
