Shader "Simple/Caustics_Height" {

    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		[NoScaleOffset] _DerivHeightMap("Deriv (AG) Height (B)", 2D) = "black" {}
		_HeightScale("Height Scale, Constant", Float) = 0.25
		_HeightScaleModulated("Height Scale, Modulated", Float) = 0.75

		[NoScaleOffset] _FlowMap("Flow (RG, A noise)", 2D) = "black" {}
		_UJump("U jump per phase", Range(-0.25, 0.25)) = 0.25
		_VJump("V jump per phase", Range(-0.25, 0.25)) = 0.25
		_Tiling("Tiling", Float) = 1
		_Speed("Speed", Float) = 1
		_FlowStrength("Flow Strength", Float) = 1
		_FlowOffset("Flow Offset", Float) = 0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex, _DerivHeightMap, _FlowMap;
		float _UJump, _VJump, _Tiling, _Speed, _FlowStrength, _FlowOffset;

        struct Input {
            float2 uv_MainTex;
			float4 color : COLOR;
        };

		fixed4 _Color;
        half _Glossiness;
        half _Metallic;
		float _HeightScale, _HeightScaleModulated;

        UNITY_INSTANCING_BUFFER_START(Props)
            
        UNITY_INSTANCING_BUFFER_END(Props)

		float3 FlowUVW(float2 uv, float2 flowVector, float2 jump, float flowOffset, float tiling, float time, bool flowB) {
			float phaseOffset = flowB ? 0.5 : 0;
			float progress = frac(time + phaseOffset);
			float3 uvw;

			uvw.xy = uv - flowVector * (progress + flowOffset);
			uvw.xy *= tiling;
			uvw.xy += phaseOffset;
			uvw.xy += (time - progress) * jump;
			uvw.z = 1 - abs(1 - 2 * progress);

			return uvw;
		}

		float3 UnpackDerivativeHeight(float4 textureData) {
			float3 dh = textureData.agb;
			dh.xy = dh.xy * 2 - 1;
			return dh;
		}

        void surf (Input IN, inout SurfaceOutputStandard o) {
			float3 flow = tex2D(_FlowMap, IN.uv_MainTex).rgb;
			flow.xy = flow.xy * 2 - 1;
			flow *= _FlowStrength;

			float noise = tex2D(_FlowMap, IN.uv_MainTex).a;
			float time = _Time.y * _Speed + noise;
			float2 jump = float2(_UJump, _VJump);

			float3 uvwA = FlowUVW(IN.uv_MainTex, flow.xy, jump, _FlowOffset, _Tiling, time, false);
			float3 uvwB = FlowUVW(IN.uv_MainTex, flow.xy, jump, _FlowOffset, _Tiling, time, true);

			float finalHeightScale = flow.z * _HeightScaleModulated + _HeightScale;

			float3 dhA = UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvwA.xy)) * (uvwA.z * finalHeightScale);
			float3 dhB = UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvwB.xy)) * (uvwB.z * finalHeightScale);
			o.Normal = normalize(float3(-(dhA.xy + dhB.xy), 1));

			fixed4 texA = tex2D(_MainTex, uvwA.xy) * uvwA.z;
			fixed4 texB = tex2D(_MainTex, uvwB.xy) * uvwB.z;

			fixed4 c = (texA + texB) * _Color;

			o.Albedo = saturate(c.rgb * IN.color);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        ENDCG
    }
	
    FallBack "Diffuse"
}
