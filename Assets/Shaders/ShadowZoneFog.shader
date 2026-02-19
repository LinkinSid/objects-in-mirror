Shader "Custom/ShadowZoneFog"
{
    Properties
    {
        _Color ("Fog Color", Color) = (0, 0, 0, 0.4)
        _NoiseScale ("Noise Scale", Float) = 2.0
        _NoiseSpeed ("Noise Speed", Float) = 0.3
        _EdgeFade ("Edge Fade", Range(0, 0.5)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
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
                float2 worldXY : TEXCOORD1;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _NoiseScale;
                float _NoiseSpeed;
                float _EdgeFade;
            CBUFFER_END

            // Hash & value noise
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal Brownian Motion — layered noise for organic look
            float fbm(float2 p)
            {
                float v = 0.0;
                float amp = 0.5;
                for (int i = 0; i < 3; i++)
                {
                    v += amp * vnoise(p);
                    p *= 2.0;
                    amp *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.worldXY = TransformObjectToWorld(input.positionOS.xyz).xy;
                o.color = input.color;
                return o;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // World-space noise so pattern tiles consistently across zones
                float t = _Time.y * _NoiseSpeed;
                float2 wuv = input.worldXY * _NoiseScale;

                float n = fbm(wuv + float2(t * 0.3, t * 0.2));
                n += fbm(wuv * 1.4 - float2(t * 0.2, t * 0.15)) * 0.5;
                n = saturate(n * 0.7 + 0.3); // bias away from pure black

                // Soft edge fade using sprite UVs
                float2 lo = smoothstep(0.0, _EdgeFade, input.uv);
                float2 hi = smoothstep(0.0, _EdgeFade, 1.0 - input.uv);
                float edgeMask = lo.x * lo.y * hi.x * hi.y;

                float4 col = _Color;
                col.a *= n * edgeMask * input.color.a;
                return col;
            }
            ENDHLSL
        }
    }
}
