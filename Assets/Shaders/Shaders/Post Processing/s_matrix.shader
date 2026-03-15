Shader "Custom/s_matrix"
{
	HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		#include "Assets/Shaders/HLSL/ColorSpace.hlsl"
		#include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
		TEXTURE2D(_SourceTex);
		SAMPLER(sampler_SourceTex);
		
		TEXTURE2D(_CameraDepthTexture);
		SAMPLER(sampler_CameraDepthTexture);

		TEXTURE2D(_NoiseTexture);
		SAMPLER(sampler_NoiseTexture);

		float _PlayerDepth;
		float3 _Color1;
		float3 _Color2;


		half4 frag(Varyings input) : SV_TARGET
		{
			float rotSpeed = 0.04;
			float rayAmount = 60;
			float time = _Time.y * rotSpeed;
			float sunSize = lerp(0.05, 0.2, sin(time * 10) * 0.5 + 0.5);
			float noiseIntensity = 1;

			half4 blit = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
			float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).r;

			float3 worldPos = ComputeWorldSpacePosition(input.texcoord, depth, UNITY_MATRIX_I_VP);
			
			float linearDepth = pow(saturate((worldPos.z - _PlayerDepth) / 64), 1); // 1 is the depth of the player
			float2 aspect = float2(_ScreenParams.x / _ScreenParams.y, 1);

			float2 centerUV = input.texcoord * aspect - 0.5 * aspect;
			
			float rotSin = sin(time);
			float rotCos = cos(time);
			float2x2 rotMat =	{ -rotSin, rotCos,
									rotCos, rotSin,
								};
			centerUV = mul(centerUV, rotMat);
			half4 noiseTex = SAMPLE_TEXTURE2D_X(_NoiseTexture, sampler_NoiseTexture, (centerUV * 0.5) - (_Time.y * 0.01));
			float sun = (1 - length(centerUV) * 4) + sunSize;
			float noise = (noiseTex.b * 2 - 1) * noiseIntensity * (1 - saturate(sun));

			float angle = atan2(centerUV.x, centerUV.y);
			float t = angle * rayAmount;
			float rays = asin(sin(t * 0.25) * sin(t * 2)) + noise;

			float sunRays = round(saturate(rays + sun) + pow(saturate(sun), 5));// * skyMask;
			//return half4(sunRays.xxx, 1);
			float skyMask = 1 - step(worldPos.z, 64);
			sunRays *= skyMask;


			float grey = (blit.r * (1 - skyMask)) + sunRays + (linearDepth * 0.05);

			float3 finalColor = lerp(_Color1, _Color2, grey);
			return half4(finalColor, 1);
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
