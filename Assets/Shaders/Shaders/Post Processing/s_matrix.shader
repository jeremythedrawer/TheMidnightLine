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
		
		float _Lightness;
		float _GreenToRed;
		float _BlueToYellow;

		half4 frag(Varyings input) : SV_TARGET
		{
			half4 blit = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
			float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).r;
			float3 worldPos = ComputeWorldSpacePosition(input.texcoord, depth, UNITY_MATRIX_I_VP);
			
			float linearDepth = pow(worldPos.z/32, 1); // 32 is the far plane
			float3 colLinearDepth = linearDepth * half3(0.1,0.2,0.3) * 0.2;


			return half4(blit.rgb + colLinearDepth, 1); // TODO: Make some cool matrix shader
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
