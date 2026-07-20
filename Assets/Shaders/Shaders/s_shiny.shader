Shader "Custom/s_shiny"
{
    Properties
    {
        [NoScaleOffset] _NoiseTexture("Texture Atlas", 2D) = "white" {}
        _RotSpeed("Rotational Speed", float) = 0.02
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_NoiseTexture);
		    SAMPLER(sampler_NoiseTexture);

            float _DayNight;
            
            float3 _BlackColor;
            float3 _MeridiaColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _RotSpeed;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 centerUV = i.uv * 2 - 1;

                float time = _Time.y * _RotSpeed;
                float rotSin = sin(time);
			    float rotCos = cos(time);
			    float2x2 rotMat =	{ -rotSin, rotCos,
									    rotCos, rotSin,
								    };

			    float2 rotUV = mul(centerUV, rotMat);

			    half4 noiseTex = SAMPLE_TEXTURE2D_X(_NoiseTexture, sampler_NoiseTexture, atan2(rotUV.y, rotUV.x));

                float circleSDF = min(1 - length(centerUV), centerUV.y);

                float mask = saturate(floor(circleSDF + noiseTex.r));
                float3 final = mask * _MeridiaColor;

                return half4(final, mask);
            }
            ENDHLSL
        }
    }
}
