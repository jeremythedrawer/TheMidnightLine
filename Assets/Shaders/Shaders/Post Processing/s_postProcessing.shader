Shader "Custom/s_postProcessing"
{
	Properties
    {
        [NoScaleOffset] _NoiseTexture("Noise Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
		#include "Assets/Shaders/HLSL/DitherShaderFunctions.hlsl"
		TEXTURE2D(_NoiseTexture);
		SAMPLER(sampler_NoiseTexture);

		float2 _SnapDiff;
		int _Invert;
		float4 _CamVelocity;
		float4 _CamPos;

		half4 frag(Varyings input) : SV_TARGET
		{
			float4 depthTex = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord.xy);

			float centerDist = 1 - abs(input.texcoord.x * 2 - 1);
			float2 noiseUV = input.texcoord;
			noiseUV.y *= centerDist;
			noiseUV.y += _CamPos.y * 0.1;
			noiseUV.y *= 0.1;
			float4 noiseTex = SAMPLE_TEXTURE2D_X(_NoiseTexture, sampler_NoiseTexture, noiseUV);
			float bayerValue = (noiseTex.r - centerDist - depthTex.r) * (1-input.texcoord.y * centerDist) * saturate(_CamVelocity.y / 10);
			float bayerVertLines = BayerX8(bayerValue ,input.texcoord.x * _ScreenParams.x);
			//return bayerVertLines.xxxx;

			float2 uvDiff = _SnapDiff / _ScreenParams.xy;
			half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord - uvDiff).rgb;
			half3 invertCol = 1 - col;
			half3 finalCol = lerp(col, invertCol, _Invert);
			return half4(finalCol + bayerVertLines, 1);

		}
	ENDHLSL

	SubShader
	{
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
		Pass
		{
			Name "PixelPerfect"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment frag
			ENDHLSL
		}
	}
}
