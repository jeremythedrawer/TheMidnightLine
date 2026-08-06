Shader "Custom/s_postProcessing"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		float2 _SnapDiff;
		int _Invert;
		half4 frag(Varyings input) : SV_TARGET
		{
			float2 uvDiff = _SnapDiff / _ScreenParams.xy;
			half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord - uvDiff).rgb;
			half3 invertCol = 1 - col;
			half3 finalCol = lerp(col, invertCol, _Invert);
			return half4(finalCol, 1);

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
