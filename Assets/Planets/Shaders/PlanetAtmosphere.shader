Shader "EbalStudios/PlanetAtmosphere"
//Simple shader to give an atmospheric effect which can be used for plaents or the stars corona
//The sole function of this shader is to make things viewed at an angle invisible.
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MinThreshold("Min Threshold", Range(0,1)) = 0
		_MaxThreshold("Max Threshold", Range(0,1)) = 1
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

		Pass
		{
			Blend One One
			ZWrite Off
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			//Make fog work
			#pragma multi_compile_fog
			#define UNITY_PASS_FORWARDBASE

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD2;
				float3 wNormal : TEXCOORD1;
				UNITY_FOG_COORDS(3)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _MinThreshold;
			float _MaxThreshold;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.wNormal = UnityObjectToWorldNormal(v.normal);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			float InverseLerp(float min, float max, float t)
			{
				t = clamp(t, min,max);
				return (t-min) / (max-min);
			}

			fixed4 frag (v2f i) : SV_Target
			{

				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				float blend = col * InverseLerp(_MinThreshold , _MaxThreshold , abs(dot(normalize(i.viewDir),normalize(i.wNormal))));
				col.x = col.x*blend;
				col.y = col.y*blend;
				col.z = col.z*blend;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
