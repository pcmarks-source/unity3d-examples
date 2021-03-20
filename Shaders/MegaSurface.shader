Shader "Custom/MegaSurface" {

	Properties {
		_GridColor("Color", Color) = (1,1,1,1)
		_GridTex("Grid Pattern", 2D) = "white" {}

		_MainTex("Pattern Array (Greyscale)", 2DArray) = "white" {}
		_NormTex("Normal Array", 2DArray) = "white" {}
		_Scale("Texture Scale", Float) = 1
	}

	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM

		#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma target 3.5

		#pragma multi_compile _ GRID_ON

		sampler2D _GridTex;
		UNITY_DECLARE_TEX2DARRAY(_MainTex);
		UNITY_DECLARE_TEX2DARRAY(_NormTex);

		struct Input {
			float2 uv_MainTex;
			float2 uv_NormTex;

			float4 color : COLOR;
			float3 textureProperties;
		};

		void vert(inout appdata_full v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);
			data.textureProperties = v.texcoord2.xyz;
		}

		fixed4 _GridColor;
		float _Scale;

		UNITY_INSTANCING_BUFFER_START(Props)
			
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandard o) {

			fixed4 grid = 0;
			#if defined(GRID_ON)
				grid = tex2D(_GridTex, IN.uv_MainTex);
			#endif

			float3 uvc = float3(IN.uv_MainTex, IN.textureProperties[0]);
			fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvc * _Scale);

			float3 uvn = float3(IN.uv_NormTex, IN.textureProperties[0]);
			fixed3 n = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_NormTex, uvn * _Scale));

			o.Albedo = saturate((c.rgb * IN.color) + (_GridColor * grid.a));
			o.Normal = n;

			o.Smoothness = IN.textureProperties[1];
			o.Metallic = IN.textureProperties[2];

			o.Alpha = c.a;
		}

		ENDCG
	}

	FallBack "Diffuse"
}
