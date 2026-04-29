Shader "Custom/s_matrix"
{
	HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		#include "Assets/Shaders/HLSL/ColorSpace.hlsl"
		#include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
		#include "Assets/Shaders/HLSL/AtlasParticles.hlsl"
		TEXTURE2D(_SourceTex);
		SAMPLER(sampler_SourceTex);
		
		TEXTURE2D(_CameraDepthTexture);
		SAMPLER(sampler_CameraDepthTexture);

		TEXTURE2D(_NoiseTexture);
		SAMPLER(sampler_NoiseTexture);

		float _PlayerDepth;

		float _MetersTravelled;
		float _DayNight;
		float3 _MainColor;
		half4 frag(Varyings input) : SV_TARGET
		{
			float2 aspect = float2(_ScreenParams.x / _ScreenParams.y, 1);
			float2 centerUV = input.texcoord * aspect - 0.5 * aspect;
			float4 noiseTex = SAMPLE_TEXTURE2D_X(_NoiseTexture, sampler_NoiseTexture, centerUV);

			float gradient = input.texcoord.y; 

			float horizon = sin(min(gradient + _DayNight, PI * 0.5) * PI) * 0.5 + 0.5;
			float stars = step(0.55,saturate(noiseTex.r - 1 + _DayNight) * (1-horizon));

			horizon = BayerX8(horizon, input.texcoord.y * _ScreenParams.y);


			float final = horizon + stars;

			return half4(final.xxx + _MainColor,0);
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
