// The particle pool drawn procedurally: six vertices per particle, read
// straight from the pool's arrays uploaded as structured buffers. Each
// particle is a camera-facing soft dot whose color runs along the look's ramp
// by its age ratio. A ratio of 1 collapses the quad, so dead particles draw
// nothing.
Shader "MagicCircle/Particles"
{
    Properties
    {
        _Birth ("Birth", Color) = (1, 1, 1, 1)
        _Death ("Death", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float> _Positions;
            StructuredBuffer<float> _Ratios;
            StructuredBuffer<float> _Sizes;
            float4x4 _StageToWorld;

            CBUFFER_START(UnityPerMaterial)
                half4 _Birth;
                half4 _Death;
            CBUFFER_END

            static const float2 Corners[6] =
            {
                float2(-1, -1), float2(1, -1), float2(1, 1),
                float2(-1, -1), float2(1, 1), float2(-1, 1)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
                float ratio : TEXCOORD1;
            };

            Varyings vert(uint vertexID : SV_VertexID)
            {
                uint i = vertexID / 6;
                float2 corner = Corners[vertexID % 6];
                float ratio = _Ratios[i];
                float3 stagePos = float3(_Positions[i * 3], _Positions[i * 3 + 1], _Positions[i * 3 + 2]);
                float3 viewPos = TransformWorldToView(mul(_StageToWorld, float4(stagePos, 1)).xyz);
                // `size` is the dot's diameter in meters.
                float halfSize = ratio >= 1 ? 0 : _Sizes[i] * 0.5;
                viewPos.xy += corner * halfSize;

                Varyings output;
                output.positionCS = TransformWViewToHClip(viewPos);
                output.corner = corner;
                output.ratio = ratio;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float d = length(input.corner) * 0.5;
                clip(0.5 - d);
                float soft = smoothstep(0.5, 0.0, d);
                return half4(lerp(_Birth.rgb, _Death.rgb, input.ratio), soft * (1 - input.ratio));
            }
            ENDHLSL
        }
    }
}
