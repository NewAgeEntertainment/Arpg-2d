Shader "Universal Render Pipeline/2D/SpriteClipTop"
{
    Properties
    {
        _BaseMap ("Sprite", 2D) = "white" {}
        _Color   ("Tint", Color) = (1,1,1,1)
        _ClipTop ("Keep Bottom (0..1)", Range(0,1)) = 0.45
        _Cutoff  ("Alpha Cutoff", Range(0,1)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma multi_compile _ PIXELSNAP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _Color;
            float  _ClipTop;
            float  _Cutoff;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _Color;

                // Keep only the bottom part of the sprite:
                // if UV.y is ABOVE the threshold, discard the pixel.
                if (IN.uv.y > _ClipTop) discard;

                if (c.a <= _Cutoff) discard;
                return c;
            }
            ENDHLSL
        }
    }
}
