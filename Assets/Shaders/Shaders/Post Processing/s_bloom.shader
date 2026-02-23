Shader "Custom/s_bloom"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		#define E 2.71828
		#define THRESHOLD 1.5
		#define SAMPLES 3

        float _BloomSpread;
		float _MipLevel;
        float _BloomIntensity;

		TEXTURE2D(_SourceTex);
		SAMPLER(sampler_SourceTex);

		float Gaussian(int x)
		{
			float sigmaSqu = _BloomSpread * _BloomSpread;
			return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
		}



		half4 bloomPre(Varyings input) : SV_TARGET
		{
			half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

			half brightness = max(col.r, max(col.g, col.b));
			half softness = clamp(brightness - THRESHOLD + 1, 0.0, 2.0);
			softness = (softness * softness) * 0.25;
			half multiplier = max(brightness - THRESHOLD, softness) / max(brightness, 1e-4);
			col *= multiplier;
			col = max(col, 0);

			return half4(col, 1);
		}

		half4 bloomH(Varyings input) : SV_TARGET
		{
			float2 texelSize = _BlitTexture_TexelSize.xy * _BloomSpread;

			half3 col = half3(0,0,0);
			float2 maxUVSize = _RTHandleScale.xy - 0.5 * texelSize;
			for (int i = -SAMPLES; i <= SAMPLES; i++)
			{
				float2 bilUV = min(input.texcoord + float2(texelSize.x * i, 0.0), maxUVSize);
				float gausWeight = Gaussian(i);
				half3 blit = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, bilUV, _MipLevel).rgb;
				col += blit * gausWeight;
			}

			return half4(col, 1);
		}

		half4 bloomV(Varyings input) : SV_TARGET
		{
			float2 texelSize = _BlitTexture_TexelSize.xy * _BloomSpread;

			half3 col = half3(0,0,0);
			float2 maxUVSize = _RTHandleScale.xy - 0.5 * texelSize;

			for (int i = -SAMPLES; i <= SAMPLES; i++)
			{
				float2 bilUV = min(input.texcoord + float2(0.0, texelSize.y * i), maxUVSize);
				float gausWeight =  Gaussian(i);
				half3 blit = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, bilUV, _MipLevel).rgb;
				col += blit * gausWeight;
			}

			half3 origCol = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, input.texcoord).rgb;

			col = origCol + col * _BloomIntensity;
			return half4(col, 1);
		}
	ENDHLSL

	SubShader
	{
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off


		Pass
		{
			Name "BloomPre"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment bloomPre
			ENDHLSL
		}

		Pass
		{
			Name "BloomHorizontal"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment bloomH
			ENDHLSL
		}

		Pass
		{
			Name "BloomVerical"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment bloomV
			ENDHLSL
		}
	}
}
