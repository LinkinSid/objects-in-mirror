Shader "Custom/ShadowSwimSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Swimming ("Swimming", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _BodyColor ("Body Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Float) = 1.5
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
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Swimming;
                float4 _OutlineColor;
                float4 _BodyColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.color = input.color * _Color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Normal mode: standard sprite rendering
                if (_Swimming < 0.5)
                    return texColor * input.color;

                // Swimming mode: black silhouette + white outline
                float alpha = texColor.a;

                // Sample 8 neighbors for edge detection
                float2 ts = _MainTex_TexelSize.xy * _OutlineWidth;
                float na = 0;
                na = max(na, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( ts.x,  0   )).a);
                na = max(na, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-ts.x,  0   )).a);
                na = max(na, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( 0,     ts.y)).a);
                na = max(na, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( 0,    -ts.y)).a);
                na = max(na, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( ts.x,  ts.y)).a);
                na = max(na, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-ts.x, -ts.y)).a);
                na = max(na, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( ts.x, -ts.y)).a);
                na = max(na, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-ts.x,  ts.y)).a);

                // Opaque pixel -> black body
                if (alpha > 0.1)
                    return half4(_BodyColor.rgb, alpha);

                // Transparent pixel with opaque neighbor -> white outline
                if (na > 0.1)
                    return half4(_OutlineColor.rgb, na);

                // Fully transparent
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
