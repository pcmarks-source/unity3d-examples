
#if !defined(GRASS_HYBRID_INCLUDED)
	#define GRASS_HYBRID_INCLUDED

	#define CUTOUT_OVERRIDE
	#define VERTEX_COLOR_TINT
	#define DISTANCECULLING

	//#define CLUMP_STAR
	#define CLUMP_PYRAMID

	#define USEWIND
	//#define USECOLLISION

	half _CullDistance;
	half _LevelOfDetailDistance;
	half _TransitionDistance;
	half _GrassHeight;
	half _GrassWidth;
	half _WindStrength;
	half _WindSpeed;
	sampler2D _NoiseTex, _RandomTex;
	sampler2D _CollisionTex;
	float _CollisionStr;

	#include "Psy_Geometry.cginc"

	float Map(float value, float originalMin, float originalMax, float targetMin, float targetMax) {
        return (value - originalMin) * (targetMax - targetMin) / (originalMax - originalMin) + targetMin;
    }

	[maxvertexcount(12)]
	void geom (point InterpolatorsVertex i[1], inout TriangleStream<InterpolatorsGeometry> stream) {

		float distanceFromCamera = distance(_WorldSpaceCameraPos, i[0].worldPos.xyz);

		#if defined(DISTANCECULLING)
			if (distanceFromCamera > _CullDistance) return;
		#endif

		float transitionPercent = saturate(Map(distanceFromCamera - _LevelOfDetailDistance, 0, _TransitionDistance, 0, 1));

		float3 originBot = i[0].pos.xyz;
		float3 originNormal = i[0].normal;
		float3 originTop = originBot + (originNormal * _GrassHeight);
		float rotationOffset = tex2Dlod(_RandomTex, float4(i[0].uv.xy,0,0)).x * 360;

		#if defined(USEWIND)
			float distanceHarshener = (distanceFromCamera < _LevelOfDetailDistance) ? 0 : (distanceFromCamera < _TransitionDistance) ? transitionPercent : transitionPercent + transitionPercent;
			
			float4 scrolledUV = float4(i[0].uv.x - (_Time.x * _WindSpeed), i[0].uv.y, 0, 0);
			originTop += float3(tex2Dlod(_NoiseTex, scrolledUV).x * (_WindStrength + distanceHarshener),0,0);
		#endif

		#if defined(USECOLLISION)
			float displace = tex2Dlod(_CollisionTex, float4(i[0].uv.xy,0,0)).r * _CollisionStr;
			originTop.xyz -= originNormal.xyz * displace;
		#endif

		if (distanceFromCamera < _LevelOfDetailDistance) {
			
			#if defined(CLUMP_STAR)
				buildStar(stream, i, originTop, originBot, normalize(originNormal), rotationOffset, _GrassWidth);
			#endif

			#if defined(CLUMP_PYRAMID)
				buildPyramid(stream, i, originTop, originBot, normalize(originNormal), rotationOffset, _GrassWidth);
			#endif

		} else buildBillboard(stream, i, originTop, originBot, originNormal, rotationOffset, transitionPercent, _GrassWidth + transitionPercent);

	}

#endif
