// The particle pool drawn procedurally: six vertices per particle, read
// straight from the pool's arrays uploaded as structured buffers. Each
// particle is a camera-facing quad showing one frame of its material's sprite
// atlas, rolled by its own seeded angle and spin. Its color runs along the
// look's ramp by its age ratio and its size from 1 to the look's grow. A ratio
// of 1 collapses the quad, so dead particles draw nothing.
Shader "MagicCircle/Particles"
{
    Properties
    {
        _MainTex ("Sprite Atlas", 2D) = "white" {}
        _Birth ("Birth", Color) = (1, 1, 1, 1)
        _Death ("Death", Color) = (1, 1, 1, 1)
        _Grow ("Size At Death", Float) = 1
        _Opacity ("Opacity", Float) = 1
        _Grid ("Frames Per Side", Float) = 1
        _Animated ("Frames Follow Age", Float) = 0
        _Tumble ("Random Roll", Float) = 0
        _Spin ("Roll Over Life", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Birth;
                half4 _Death;
                float _Grow;
                float _Opacity;
                float _Grid;
                float _Animated;
                float _Tumble;
                float _Spin;
            CBUFFER_END

            static const float2 Corners[6] =
            {
                float2(-1, -1), float2(1, -1), float2(1, 1),
                float2(-1, -1), float2(1, 1), float2(-1, 1)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float ratio : TEXCOORD1;
                nointerpolation float pick : TEXCOORD2;
            };

            /// A stable 0..1 value per pool slot, so a particle keeps its roll and frame for life.
            float Hash(uint i, float salt)
            {
                return frac(sin(i * 12.9898 + salt * 78.233) * 43758.5453);
            }

            Varyings vert(uint vertexID : SV_VertexID)
            {
                uint i = vertexID / 6;
                float2 corner = Corners[vertexID % 6];
                float ratio = _Ratios[i];
                float3 stagePos = float3(_Positions[i * 3], _Positions[i * 3 + 1], _Positions[i * 3 + 2]);
                float3 viewPos = TransformWorldToView(mul(_StageToWorld, float4(stagePos, 1)).xyz);
                // `size` is the sprite's diameter in meters.
                float halfSize = ratio >= 1 ? 0 : _Sizes[i] * 0.5 * lerp(1, _Grow, ratio);
                float direction = Hash(i, 2) < 0.5 ? -1 : 1;
                float roll = (Hash(i, 1) * 2 - 1) * _Tumble + direction * _Spin * ratio;
                float s = sin(roll), c = cos(roll);
                viewPos.xy += float2(corner.x * c - corner.y * s, corner.x * s + corner.y * c) * halfSize;

                Varyings output;
                output.positionCS = TransformWViewToHClip(viewPos);
                output.uv = corner * 0.5 + 0.5;
                output.ratio = ratio;
                output.pick = Hash(i, 3);
                return output;
            }

            /// One frame of the atlas. Frame 0 is the top-left cell, read row by row.
            half4 Frame(float2 uv, float frame)
            {
                float col = fmod(frame, _Grid);
                float row = floor(frame / _Grid);
                float2 cell = float2(col, _Grid - 1 - row);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, (cell + uv) / _Grid);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float frames = _Grid * _Grid;
                half4 sprite;
                if (_Animated > 0.5)
                {
                    // Cross-fade between neighbouring frames so the flipbook never steps.
                    float at = saturate(input.ratio) * (frames - 1);
                    float f0 = floor(at);
                    sprite = lerp(Frame(input.uv, f0), Frame(input.uv, min(f0 + 1, frames - 1)), at - f0);
                }
                else
                {
                    sprite = Frame(input.uv, min(floor(input.pick * frames), frames - 1));
                }
                // A quick fade-in, so births at a dense mouth do not pop.
                float alpha = sprite.a * (1 - input.ratio * input.ratio) * saturate(input.ratio * 10) * _Opacity;
                return half4(lerp(_Birth.rgb, _Death.rgb, input.ratio) * sprite.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
