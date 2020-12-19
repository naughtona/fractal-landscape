Shader "WaterShader"
{
    Properties {
        _MainTex ("Water Texture", 2D) = "white" {}
		_ambientFactor ("ambient",Float) = 1
		_diffuseFactor ("diffuse",Float) = 1
		_specularFactor ("specular", Float) = 1
		_Shininess ("Shininess", Float) = 2
		_waveHeight ("WaveHeight", Float) = 0.5
        _waveSpeed ("WaveSpeed", Float) = 1
        _waveLength ("WaveLength", Float) = 5
		_waveDirection ("Wave Direction", Vector) = (1,0,0,0)
		_rotationSpeed ("Wave rotation speed", Float) = 1
        _Transparency ("Transparency", Float) = 0.8
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
		LOD 200

		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert 
			#pragma fragment frag

			#include "UnityCG.cginc" 
			#include "UnityLightingCommon.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _ambientFactor;
			float _diffuseFactor;
			float _specularFactor;
			float _Shininess;

			float _waveHeight;
			float _waveSpeed;
			float _waveLength;
			float2 _waveDirection;
			float _rotationSpeed;
			float _Transparency;

			uniform float3 _PointLightColor;
			uniform float3 _PointLightPosition;
			
			struct vertIn {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			// model -> world -> view -> projection
			struct vertOut {
				float4 vertexWorld : TEXCOORD1;
				float4 vertexProjection : POSITION;
				float3 normalWorld : NORMAL;
				float2 uv : TEXCOORD0;
			};
			
			vertOut vert(vertIn v) {
				// wave displacement
				float3 displacedVertex = v.vertex.xyz;

				float period = 2*UNITY_PI/_waveLength;
				// direction is a vector for direction in xz but swizzle 
				//	makes us use xy

				// change direction with time
				float xDir = 0.5;
				float zDir = 1 - xDir;

				float2 direction = normalize(float2(xDir,zDir));

				// function inside the sin function for vertex height displacement
				float sinBody = period*(dot(direction,v.vertex.xz)-_waveSpeed*_Time.y);
				displacedVertex.y = _waveHeight*sin(sinBody);

				// add direction displacement to x and z
				displacedVertex.x += direction.x;
				displacedVertex.z += direction.y;

				float sinBodyDerivativeWRTX = period*_waveHeight*cos(sinBody);
							
				// recalculate normals using cross product of tangent and binormal
				float3 tangent = float3(1, direction.x*sinBodyDerivativeWRTX, 0);

				float3 binormal = float3(0, 
					direction.y*sinBodyDerivativeWRTX,
					1);

				v.normal = normalize(cross(binormal,tangent));

				vertOut o;
				o.vertexProjection = UnityObjectToClipPos(displacedVertex);
				o.vertexWorld = mul(unity_ObjectToWorld,displacedVertex);

				// normal is a vector rather than a position so to transform from 
				// model to world space we need to multiply it by the inverse of 
				// unity_ObjectToWorld on the right hand side. Also it is a float3
				// and the matrix is 4*4 so we convert to float4 first. Finally 
				// since we might have scaling in the transform we need to 
				// normalise the result
				o.normalWorld = normalize(mul(float4(v.normal,0),unity_WorldToObject));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(vertOut v) : COLOR {
				float4 tex = tex2D(_MainTex, v.uv);
				// for ambient light
				float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb*tex.rgb;
				float3 normal = normalize(v.normalWorld);

				// for diffuse, we first need to calculate vector from vertex to light source
				// _WorldSpaceLightPos0 : if w component is 1, then it is a light source with position xyz
				// else w component is 0, and xzy is a vector of directional light without a position 
				// _LightColor0 : color of the first directional light
				float3 L = normalize(_PointLightPosition - v.vertexWorld.xyz);
				float LdotN = dot(L, normal.xyz);
				float3 diffuse = _PointLightColor.rgb*tex.rgb*saturate(LdotN);
				
				// for specular, we need to calculate vector from vertex to camera, 
				// and vector of directional light reflected off surface
				float3 V = normalize(_WorldSpaceCameraPos - v.vertexWorld.xyz);
				
				// vector of directional light reflected off surface can be calculated with Cg function
				// 'reflect' which takes the incident vector and the surface normal
				float3 R = 2*LdotN*normal.xyz-L;
				float VdotR = saturate(dot(V,R));
				float3 specular = _PointLightColor.rgb*pow(VdotR,_Shininess);

				float3 color = _ambientFactor*ambient + _diffuseFactor*diffuse + _specularFactor*specular;

				return float4(color,_Transparency);
			}

			ENDCG
		}
    }
}

			
			