
#if !defined(PSY_GEOMETRY_INCLUDED)
	#define PSY_GEOMETRY_INCLUDED

	#define CUSTOM_GEOMETRY_INTERPOLATORS \
		float4 uv1uv2 : TEXCOORD8;

	#include "Psy_Lighting_Input.cginc"

	void vert2geom (VertexData v, inout InterpolatorsVertex i) {
		UNITY_INITIALIZE_OUTPUT(InterpolatorsVertex, i);
		UNITY_SETUP_INSTANCE_ID(v);

		i.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex);
		i.pos = v.vertex;

		#if defined(FOG_DEPTH)
			i.worldPos.w = i.pos.z;
		#endif

		i.uv.xy = i.worldPos.xz; //v.uv;
		i.normal = v.normal;
		i.tangent = v.tangent;
		i.color = v.color;

		i.uv1uv2 = float4(v.uv1.x, v.uv1.y, v.uv2.x, v.uv2.y);
	}

	#define VERT_PROGRAM vert2geom

	#include "Psy_Lighting.cginc"

	struct InterpolatorsGeometry {
		InterpolatorsVertex data;
	};

	float3 directionFromAngle(float angleInDegrees) {
		return float3(sin(angleInDegrees * 0.0174532924), 0, cos(angleInDegrees * 0.0174532924));
	}

	void buildQuad(inout TriangleStream<InterpolatorsGeometry> stream, float3 points[4], InterpolatorsVertex data, float3 faceNormal) {

		InterpolatorsGeometry g;

		for (uint i = 0; i < 4; ++i) {
			g.data = data;

			g.data.worldPos.xyz = mul(unity_ObjectToWorld, points[i]);
			g.data.pos = UnityObjectToClipPos(points[i]);

			#if defined(FOG_DEPTH)
				g.data.worldPos.w = g.data.pos.z;
			#endif

			g.data.uv.xy = TRANSFORM_TEX(float2(i % 2, (uint)i / 2), _MainTex);
			g.data.uv.zw = TRANSFORM_TEX(float2(i % 2, (uint)i / 2), _SecondTex);

			#if defined(LIGHTMAP_ON) || ADDITIONAL_MASKED_DIRECTIONAL_SHADOWS
				g.data.lightmapUV = g.uv1uv2.xy * unity_LightmapST.xy + unity_LightmapST.zw;
			#endif

			#if defined(DYNAMICLIGHTMAP_ON)
				g.data.dynamicLightmapUV = g.uv1uv2.zw * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
			#endif

			g.data.normal = UnityObjectToWorldNormal(faceNormal);

			#if defined(BINORMAL_PER_FRAGMENT)
				g.data.tangent = float4(UnityObjectToWorldDir(g.data.tangent.xyz), g.data.tangent.w);
			#endif

			UNITY_TRANSFER_SHADOW(g.data, g.uv1uv2.xy);

			#if defined(VERTEXLIGHT_ON)
				g.data.vertexLightColor = Shade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, g.data.worldPos, g.data.normal
				);
			#endif

			stream.Append(g);
		}

		stream.RestartStrip();
	}

	void buildBillboard(inout TriangleStream<InterpolatorsGeometry> stream, InterpolatorsVertex data, float3 originTop, float3 originBot, float3 staticUp, float rotationOffset, float quadHeight, half quadWidth) {
		
		originTop += staticUp * quadHeight;
		float3 viewDir = normalize(_WorldSpaceCameraPos - data.worldPos.xyz);
		float3 relativeRight = normalize(cross(viewDir, staticUp));
		float3 relativeForward = normalize(cross(directionFromAngle(rotationOffset), staticUp));

		float3 quad[4] = {
			originBot + ((relativeRight * 0.5) * quadWidth),
			originBot - ((relativeRight * 0.5) * quadWidth),
			originTop + ((relativeRight * 0.5) * quadWidth),
			originTop - ((relativeRight * 0.5) * quadWidth)
		};

		relativeForward = relativeForward;
		buildQuad(stream, quad, data, relativeForward);

	}

	void buildStar(inout TriangleStream<InterpolatorsGeometry> stream, InterpolatorsVertex data, float3 originTop, float3 originBot, float3 staticUp, float rotationOffset, half quadWidth) {
	
		float3 lightingRight = directionFromAngle(rotationOffset);
		float3 lightingForward = normalize(cross(lightingRight, staticUp));

		float3 relativeRight = directionFromAngle(0 + rotationOffset);
		float3 relativeForward = cross(relativeRight, staticUp);

		float3 quad0[4] = {
			originBot + ((relativeRight * 0.5) * quadWidth),
			originBot - ((relativeRight * 0.5) * quadWidth),
			originTop + ((relativeRight * 0.5) * quadWidth),
			originTop - ((relativeRight * 0.5) * quadWidth)
		};

		relativeForward = lightingForward;
		buildQuad(stream, quad0, data, relativeForward);

		relativeRight = directionFromAngle(120 + rotationOffset);
		relativeForward = cross(relativeRight, staticUp);

		float3 quad1[4] = {
			originBot + ((relativeRight * 0.5) * quadWidth),
			originBot - ((relativeRight * 0.5) * quadWidth),
			originTop + ((relativeRight * 0.5) * quadWidth),
			originTop - ((relativeRight * 0.5) * quadWidth)
		};

		relativeForward = lightingForward;
		buildQuad(stream, quad1, data, relativeForward);

		relativeRight = directionFromAngle(-120 + rotationOffset);
		relativeForward = cross(relativeRight, staticUp);

		float3 quad2[4] = {
			originBot + ((relativeRight * 0.5) * quadWidth),
			originBot - ((relativeRight * 0.5) * quadWidth),
			originTop + ((relativeRight * 0.5) * quadWidth),
			originTop - ((relativeRight * 0.5) * quadWidth)
		};

		relativeForward = lightingForward;
		buildQuad(stream, quad2, data, relativeForward);
	
	}

	void buildPyramid(inout TriangleStream<InterpolatorsGeometry> stream, InterpolatorsVertex data, float3 originTop, float3 originBot, float3 staticUp, float rotationOffset, half quadWidth) {

		float3 lightingRight = directionFromAngle(rotationOffset);
		float3 lightingForward = normalize(cross(lightingRight, staticUp));

		float topOffset = quadWidth;
		float botOffset = quadWidth;

		float3 relativeRight = directionFromAngle(0 + rotationOffset);
		float3 relativeForward = cross(relativeRight, staticUp);

		float3 quadTop = originTop - (relativeForward * topOffset);
		float3 quadBot = originBot + (relativeForward * botOffset);

		float3 quad0[4] = {
			quadBot + ((relativeRight * 0.5) * quadWidth),
			quadBot - ((relativeRight * 0.5) * quadWidth),
			quadTop + ((relativeRight * 0.5) * quadWidth),
			quadTop - ((relativeRight * 0.5) * quadWidth)
		};

		relativeForward = lightingForward;
		buildQuad(stream, quad0, data, relativeForward);

		relativeRight = directionFromAngle(120 + rotationOffset);
		relativeForward = cross(relativeRight, staticUp);

		quadTop = originTop - (relativeForward * topOffset);
		quadBot = originBot + (relativeForward * botOffset);

		float3 quad1[4] = {
			quadBot + ((relativeRight * 0.5) * quadWidth),
			quadBot - ((relativeRight * 0.5) * quadWidth),
			quadTop + ((relativeRight * 0.5) * quadWidth),
			quadTop - ((relativeRight * 0.5) * quadWidth)
		};

		relativeForward = lightingForward;
		buildQuad(stream, quad1, data, relativeForward);

		relativeRight = directionFromAngle(-120 + rotationOffset);
		relativeForward = cross(relativeRight, staticUp);

		quadTop = originTop - (relativeForward * topOffset);
		quadBot = originBot + (relativeForward * botOffset);

		float3 quad2[4] = {
			quadBot + ((relativeRight * 0.5) * quadWidth),
			quadBot - ((relativeRight * 0.5) * quadWidth),
			quadTop + ((relativeRight * 0.5) * quadWidth),
			quadTop - ((relativeRight * 0.5) * quadWidth)
		};

		relativeForward = lightingForward;
		buildQuad(stream, quad2, data, relativeForward);
	
	}

#endif
