Shader "Custom/s_sunRays"
{
    Properties
    {
        [NoScaleOffset] _NoiseTexture("Texture Atlas", 2D) = "white"
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

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 objPos = v.positionOS.xyz;
                float2 scale;
                scale.x = length(unity_ObjectToWorld._m00_m10_m20);
                scale.y = length(unity_ObjectToWorld._m01_m11_m21);

                float worldCamTop = unity_OrthoParams.y + _WorldSpaceCameraPos.y;

                float3 worldPos = TransformObjectToWorld(objPos);
                worldPos.y = lerp(worldPos.y + worldCamTop, worldPos.y - scale.y, _DayNight);
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.uv = v.uv;
                o.worldPos = worldPos;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float rotSpeed = 0.02;
			    float rayAmount = 50;
			    float time = _Time.y * rotSpeed;
			    float sunSize = lerp(0.05, 0.02, sin(time * 10) * 0.5 + 0.5);
			    float noiseIntensity = 6;


                float2 centerUV = i.uv * 2 - 1;

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

			    float sunRays = round(saturate(rays - noise + sun + saturate(sun)));
               // sunRays = BayerX8(sunRays - 0.5, i.positionHCS.y);

                half horizonThreshold = step(0, i.worldPos.y);
                half alpha = sunRays * horizonThreshold;

                clip(alpha- 0.001);
                return half4(sunRays.xxx, 1);
            }
            ENDHLSL
        }
    }
}
