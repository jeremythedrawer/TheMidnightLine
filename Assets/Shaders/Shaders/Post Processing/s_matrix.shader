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
			float horizon = sin((input.texcoord.y + (_DayNight * 0.5)) * PI) * 0.5 + 0.5;
			horizon = BayerX8(horizon, input.texcoord.y * _ScreenParams.y);
			return half4(horizon.xxx + _MainColor,0);
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
