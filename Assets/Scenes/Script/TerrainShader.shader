Shader "TerrainShader"
{
    Properties {
        [NoScaleOffset] _RockTex ("Rock Texture", 2D) = "white" {}
		[NoScaleOffset] _SnowTex ("Snow Texture", 2D) = "white" {}
		_GrassTex ("Grass Texture", 2D) = "white" {}
		[NoScaleOffset] _SandTex ("Sand Texture", 2D) = "white" {}
		_ambientFactor ("ambient",Float) = 1
		_diffuseFactor ("diffuse",Float) = 1
		_specularFactor ("specular", Float) = 1
		_Shininess ("Shininess", Float) = 2 // power used in specular
		_blend ("texture blend", Float) = 2
	}
    SubShader {
        Tags { "RenderType"="Opaque" } // may need to change to transparent
        LOD 200
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc" 
			#include "UnityLightingCommon.cginc"

			// uniform float4 _Color; 
			sampler2D _RockTex;
			sampler2D _GrassTex;
			sampler2D _SnowTex;
			sampler2D _SandTex;

			float4 _GrassTex_ST;
			float _ambientFactor;
			float _diffuseFactor;
			float _specularFactor;
			float _Shininess;
			float _blend;

			uniform float _maxHeight;
			uniform float _minHeight;
			uniform float3 _PointLightColor;
			uniform float3 _PointLightPosition;
			
			struct vertIn {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			/* model -> world -> view -> projection */ 
			struct vertOut {
				float4 vertexWorld : TEXCOORD1;
				float4 vertexProjection : POSITION;
				float3 normalWorld : NORMAL;
				float2 uv : TEXCOORD0;
			};
			
			vertOut vert(vertIn v) {				
				vertOut o;
				o.vertexProjection = UnityObjectToClipPos(v.vertex);
				o.vertexWorld = mul(unity_ObjectToWorld,v.vertex);

				/* normal is a vector rather than a position so to transform from 
					model to world space we need to multiply it by the inverse of 
					unity_ObjectToWorld on the right hand side. Also it is a float3
					and the matrix is 4*4 so we convert to float4 first. Finally 
					since we might have scaling in the transform we need to 
					normalise the result */
				o.normalWorld = normalize(mul(float4(v.normal,0),unity_WorldToObject));
				// o.color = v.color;
				o.uv = TRANSFORM_TEX(v.uv, _GrassTex);
				return o;
			}

			float4 swizRef(float4 weights, int i, float value) {
				if (i==0) {
					weights.x = value;
				} else if (i==1) {
					weights.y = value;
				} else if (i==2) {
					weights.z = value;
				} else {
					weights.w = value;
				}
				return weights; 
			}

			float4 weight_imp(float height) {
				float range = (_maxHeight-_minHeight)/4;
				float4 regions = float4(_minHeight+range, _minHeight+2*range, _minHeight+3*range, _maxHeight);
				float4 weights; 
				float total=0;

				for (int i=0;i<=3;i++) {
					float distance = abs(regions[i]-height);
					if (distance < 0.05) {
						weights = swizRef(weights,i,1);
					} else {
						weights = swizRef(weights,i,1/pow(distance,_blend));
					}
					total += weights[i];
				}
				return weights/total;
			}			

			fixed4 frag(vertOut v) : COLOR {
				/* basic splat mapping based on color defined in diamondsquare.cs */  
				float4 texMix = float4(0,0,0,0);

				float4 weights = weight_imp(v.vertexWorld.y);
				texMix = tex2D(_SnowTex, v.uv) * weights.w +
				        tex2D(_RockTex, v.uv) * weights.z +
        				tex2D(_GrassTex, v.uv) * weights.y +
        				tex2D(_SandTex, v.uv) * weights.x;
				
				/* for ambient light */
				float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb*texMix.rgb;
				float3 normal = normalize(v.normalWorld);

				/* for diffuse, we first need to calculate vector from vertex to light source
					_WorldSpaceLightPos0 : if w component is 1, then it is a light source with position xyz
					else w component is 0, and xzy is a vector of directional light without a position 
					_LightColor0 : color of the first directional light */
				float3 vertexToLightSource = normalize(_PointLightPosition.xyz - v.vertexWorld.xyz);
				float3 diffuse = _PointLightColor.rgb*texMix.rgb*max(dot(normal,vertexToLightSource),0);
				
				/* for specular, we need to calculate vector from vertex to camera, 
					and vector of directional light reflected off surface */
				float3 vertexToCamera = normalize(_WorldSpaceCameraPos - v.vertexWorld.xyz);
				
				/* vector of directional light reflected off surface can be calculated with Cg function
					'reflect' which takes the incident vector and the surface normal  */
				float3 vertexToReflection = reflect(-vertexToLightSource,normal);
				float reflectionDotCamera = max(dot(vertexToReflection,vertexToCamera),0);
				float3 specular = _PointLightColor.rgb*pow(reflectionDotCamera,_Shininess);

				float3 color = _ambientFactor*ambient + _diffuseFactor*diffuse + _specularFactor*specular; //
				return float4(color,1.0f);
			}		

			ENDCG
		}
    }
}

			
			