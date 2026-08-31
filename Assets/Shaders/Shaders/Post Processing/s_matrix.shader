Shader "Custom/s_matrix"
{
	Properties
    {
        [NoScaleOffset] _StarTexture("Star Texture", 2D) = "white" {}
        [NoScaleOffset] _NoiseTexture("Noise Texture", 2D) = "white" {}
		_Test("Test", Range(0,1)) = 0
    }
	HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

		#include "Assets/Shaders/HLSL/ColorSpace.hlsl"
		#include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
		#include "Assets/Shaders/HLSL/AtlasParticles.hlsl"
		
		TEXTURE2D(_SourceTex);
		SAMPLER(sampler_SourceTex);
		
		TEXTURE2D(_NoiseTexture);
		SAMPLER(sampler_NoiseTexture);

		TEXTURE2D(_StarTexture);
		SAMPLER(sampler_StarTexture);
		float4 _StarTexture_TexelSize;
		float _PlayerDepth;

		float _MetersTravelled;
		float _DayNight;
		float3 _BlackColor;
		float3 _WhiteColor;

		CBUFFER_START(UnityPerMaterial)
		float _Test;
		CBUFFER_END

		half4 frag(Varyings input) : SV_TARGET
		{
			float2 pixelPos = input.texcoord * _ScreenParams.xy;
			float2 texSize = _StarTexture_TexelSize.zw;

			float2 starUV = frac(pixelPos / texSize);

			float4 starTex = SAMPLE_TEXTURE2D_X(_StarTexture, sampler_StarTexture, starUV);

			float4 noiseTex = SAMPLE_TEXTURE2D_X(_NoiseTexture, sampler_NoiseTexture, starUV + _Time.y * 0.02);

			float glowStars = step(0.01, starTex.r * noiseTex.r + starTex.g);
			float gradient = input.texcoord.y;

			float horizon = sin(min(gradient + _DayNight, PI * 0.5) * PI) * 0.5 + 0.5;
			float stars = glowStars * _DayNight * gradient;
			horizon = BayerX8(horizon, input.texcoord.y * _ScreenParams.y);
			//horizon += (1 - _DayNight) * 0.25;

			float depth = 0;

			#if UNITY_REVERSED_Z
				depth = SampleSceneDepth(input.texcoord);
			#else
				depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(input.texcoord);
			#endif

			float3 worldPos = ComputeWorldSpacePosition(input.texcoord, depth, UNITY_MATRIX_I_VP);

			float ground = step(0, worldPos.y);
			float greyScale = saturate(horizon + stars);

			float3 final = lerp(_BlackColor, _WhiteColor, greyScale);
			return half4(final,0);
		}
	ENDHLSL

	Subshader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		LOD 100
		
		Pass
		{
			Name "Matrix"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			ENDHLSL
		}
	}
}
