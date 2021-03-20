
#if !defined(CLOUD_BILLBOARD_INCLUDED)
	#define CLOUD_BILLBOARD_INCLUDED

	#define VERTEX_COLOR_TINT

	half _PointScale;
	half _Overcast;
	half _WindSpeed;
	sampler2D _NoiseTex;

	#include "Psy_Geometry.cginc"

	[maxvertexcount(4)]
	void geom (point InterpolatorsVertex i[1], inout TriangleStream<InterpolatorsGeometry> stream) {
		
		float4 scrolledUV = float4(i[0].uv.x - (_Time.x * _WindSpeed), i[0].uv.y, 0, 0);
		float4 noise = tex2Dlod(_NoiseTex, scrolledUV);
		float density = saturate((_Overcast - noise.x) * 2);

		float3 halfSizeUp = ((UNITY_MATRIX_V[1].xyz * (_PointScale * 0.5)) * density);
		float3 halfSizeRight = ((UNITY_MATRIX_V[0].xyz * (_PointScale * 0.5)) * density);
		float3 faceNormal = _WorldSpaceLightPos0;
		i[0].color.xyz = float3(1, 1, 1) * saturate(noise.x * 8);

		float3 originTop = i[0].pos.xyz + halfSizeUp;
		float3 originBot = i[0].pos.xyz - halfSizeUp;

		float3 quad[4] = {
			originBot + halfSizeRight,
			originBot - halfSizeRight,
			originTop + halfSizeRight,
			originTop - halfSizeRight
		};

		buildQuad(stream, quad, i, faceNormal);
	}

#endif
