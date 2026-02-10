Shader "ws/Outline"
{
    Properties
    {
        [HDR] _OutlineColour ("Outline Colour", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0,1000)) = 4
        _OutlineWidth ("Outline Width", Range(0,0.005)) = 0.002
    }
    SubShader
    {
        Tags
        {
            "RendererType" = "Opaque"
            "RendererPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct Attribuites
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half2 uv : TEXCOORD0;
                half2 offsets[8] : TEXCOORD1;
            };

            TEXTURE2D_X(_OutlineMask);
            SAMPLER(sampler_linear_clamp_OutlineMask);

            half4 _OutlineColour;
            float _Intensity;
            half _OutlineWidth;

            Varyings vert(Attribuites IN)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(IN.vertexID);

                const half correction = _ScreenParams.x / _ScreenParams.y;
                const half oblique = 0.70710678;

                output.offsets[0] = half2(-1,correction) * _OutlineWidth * oblique;
                output.offsets[1] = half2(0,correction) * _OutlineWidth;
                output.offsets[2] = half2(1,correction) * _OutlineWidth * oblique;
                output.offsets[3] = half2(-1,0) * _OutlineWidth;

                output.offsets[4] = half2(1,0) * _OutlineWidth;
                output.offsets[5] = half2(-1,-correction) * _OutlineWidth * oblique;
                output.offsets[6] = half2(0,-correction) * _OutlineWidth;
                output.offsets[7] = half2(1,-correction) * _OutlineWidth * oblique;
                return output;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                const  half kernelX[8] = {
                    -1, 0, 1,
                    -2   , 2,
                    -1, 0, 1
                };
                const half KernelY[8] = {
                    -1, -2, -1,
                     0.   ,  0,
                     1,  2,  1
                };
                half gx = 0;
                half gy = 0;
                half mask = 0;
                for (int i = 0; i < 8; i++)
                {
                    mask = SAMPLE_TEXTURE2D_X(_OutlineMask, sampler_linear_clamp_OutlineMask, IN.uv + IN.offsets[i]).a;
                    gx += mask * kernelX[i];
                    gy += mask * KernelY[i];
                }
                const half alpha = SAMPLE_TEXTURE2D_X(_OutlineMask, sampler_linear_clamp_OutlineMask, IN.uv).a;
                half4 col = _OutlineColour * _Intensity;
                col.a = saturate(abs(gx) + abs(gy)) * (1 - alpha);
                //half4 col = SAMPLE_TEXTURE2D_X(_OutlineMask, sampler_linear_clamp_OutlineMask, IN.uv);
                return col;
            }

            ENDHLSL
        }
    }
}
