Shader "UI/DirectionalFade"
{
    Properties
    {
        _Progress ("Progress", Range(0, 1)) = 0
        _Softness ("Gradient Width", Range(0.001, 1.5)) = 0.9
        _BaseDarkness ("Base Darkness", Range(0, 1)) = 0.65
        _EdgeDarkness ("Edge Darkness", Range(0, 1)) = 0.95
        _Direction ("Direction", Float) = 1
        _Overscan ("Edge Overscan", Range(0, 0.25)) = 0.08
        _BlackPlateau ("Black Width", Range(0, 0.9)) = 0.5
        _EffectMode ("Effect Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct AppData
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct VertexToFragment
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float _Progress;
            float _Softness;
            float _BaseDarkness;
            float _EdgeDarkness;
            float _Direction;
            float _Overscan;
            float _BlackPlateau;
            float _EffectMode;

            VertexToFragment vert(AppData input)
            {
                VertexToFragment output;

                output.vertex =
                    UnityObjectToClipPos(input.vertex);

                output.color = input.color;
                output.uv = input.uv;

                return output;
            }

            fixed4 frag(VertexToFragment input) : SV_Target
            {
                float softness =
                    max(_Softness, 0.001);

                float edgeAxis =
                    _Direction >= 0
                        ? input.uv.x
                        : 1.0 - input.uv.x;

                float blackWidth =
                    saturate(_BlackPlateau);

                float outgoingGradient =
                    1.0 - smoothstep(
                        blackWidth,
                        min(1.0, blackWidth + softness),
                        edgeAxis);

                float distanceFromSeam = abs(input.uv.x - 0.5);
                float blackHalfWidth = blackWidth * 0.5;

                float gradientEnd = 0.5;
                float gradientLength =
                    max(gradientEnd - blackHalfWidth, 0.001);

                // Guaranteed values for the seam overlay:
                // center plateau = 1, outer edges = 0.
                float seamGradient = saturate(
                    (gradientEnd - distanceFromSeam) / gradientLength);

                float effectAlpha;

                if (_EffectMode >= 2.5)
                {
                    effectAlpha = 1.0;
                }
                else if (_EffectMode >= 1.5)
                {
                    float wipeAxis =
                        _Direction < 0
                            ? 1.0 - input.uv.x
                            : input.uv.x;

                    float wipeStart =
                        saturate(_Progress) * (1.0 + softness) - softness;

                    effectAlpha =
                        1.0 - smoothstep(
                            wipeStart,
                            wipeStart + softness,
                            wipeAxis);
                }
                else if (_EffectMode >= 0.5)
                {
                    effectAlpha =
                        outgoingGradient * saturate(_Progress);
                }
                else
                {
                    effectAlpha = seamGradient;
                }

                float finalAlpha =
                    effectAlpha * saturate(_EdgeDarkness);

                return fixed4(
                    0.0,
                    0.0,
                    0.0,
                    finalAlpha
                    * input.color.a);
            }

            ENDCG
        }
    }
}
