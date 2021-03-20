
Shader "Simple/Fog" {

	Properties {
		_Color("Color", Color) = (1,1,1,1)
		_Density("Fog Density", Range(0, 2)) = 0.1
	}

	SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		ZWrite Off
		Blend One One
		LOD 200

		GrabPass { "_FogBackground" }

		CGPROGRAM

		#pragma surface surf StandardSpecular alpha
		#pragma target 3.0

		sampler2D _CameraDepthTexture, _FogBackground;
		float4 _CameraDepthTexture_TexelSize;

		struct Input {
			float4 screenPos;
			float4 color : COLOR;
		};

		fixed4 _Color;
		float _Density;

		UNITY_INSTANCING_BUFFER_START(Props)

		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
			float2 uv = IN.screenPos.xy / IN.screenPos.w;
			#if UNITY_UV_STARTS_AT_TOP
				if (_CameraDepthTexture_TexelSize.y < 0) {
					uv.y = 1 - uv.y;
				}
			#endif
			float backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
			float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(IN.screenPos.z);
			float depthDifference = backgroundDepth - surfaceDepth;

			float3 backgroundColor = tex2D(_FogBackground, uv).rgb;
			float fogFactor = exp2(-_Density * depthDifference);

			float3 color = lerp(IN.color, backgroundColor, fogFactor);

			o.Albedo = color;
			o.Specular = 0;
			o.Smoothness = 0;
			o.Alpha = 1;
		}

		ENDCG
	}

}
