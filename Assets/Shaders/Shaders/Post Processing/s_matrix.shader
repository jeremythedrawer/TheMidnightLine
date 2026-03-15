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
			float rotSpeed = 0.02;
			float rayAmount = 50;
			float time = _Time.y * rotSpeed;
			float sunSize = lerp(0.05, 0.02, sin(time * 10) * 0.5 + 0.5);
			float noiseIntensity = 6;

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
			float2 rotUV = mul(centerUV, rotMat);
			float sun = (1 - length(centerUV) * 6);
			float angle = atan2(rotUV.x, rotUV.y);

			half4 noiseTex = SAMPLE_TEXTURE2D_X(_NoiseTexture, sampler_NoiseTexture, centerUV + angle - (_Time.y * 0.02));
			float noise = noiseTex.b * noiseIntensity * pow(1 - saturate(sun),4);

			float t = angle * rayAmount;
			float sinT = sin(t);
			float rays = asin(sinT * pow((sinT * 0.5 + 0.5), 5)) + max(asin(sinT * sinT),0);

			float sunRays = round(saturate(rays - noise + sun + saturate(sun)));// * skyMask;
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
