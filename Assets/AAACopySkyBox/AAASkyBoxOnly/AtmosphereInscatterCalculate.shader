Shader "AtmosphereInscatter/Calculate"
{
    Properties
    {
		_MainTex ("Texture", 2D) = "white" {}
		_ZTest ("ZTest", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "./AtmosphereInscatterLUT.hlsl"

        struct appdata
		{
			float4 vertex : POSITION;
		};
		
		float _DistanceScale;

		struct v2f
		{
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
			float3 wpos : TEXCOORD1;
		};
        ENDHLSL
        
		//Pass 0
        Pass
        {
            Name "Precompute Particle Density"
	        ZTest Off
			Cull Off
			ZWrite Off
			Blend Off
			
			HLSLPROGRAM
			#pragma vertex vertQuad
            #pragma fragment particleDensityLUT
			
			#define UNITY_HDR_ON

			struct v2p
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            struct input
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

			v2p vertQuad(input v)
            {
                v2p o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.texcoord.xy;
                return o;
            }

			float2 particleDensityLUT(v2p i) : SV_Target
			{
                float cosAngle = i.uv.x * 2.0 - 1.0;
                float sinAngle = sqrt(saturate(1 - cosAngle * cosAngle));
                float startHeight = lerp(0.0, _AtmosphereHeight, i.uv.y);

                float3 rayStart = float3(0, startHeight, 0);
                float3 rayDir = float3(sinAngle, cosAngle, 0);
                
				return PrecomputeParticleDensity(rayStart, rayDir);
			}
			
			ENDHLSL
        }

		//Pass 1
		Pass
		{
			Name "Ambient Light LUT"
			ZTest Off
			Cull Off
			ZWrite Off
			Blend Off
			HLSLPROGRAM
			#pragma vertex vertQuad
			#pragma fragment fragDir

						struct v2p
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			struct input
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			v2p vertQuad(input v)
			{
				v2p o;
				o.pos = TransformObjectToHClip(v.vertex.xyz);
				o.uv = v.texcoord.xy;
				return o;
			}

			float4 fragDir(v2f i) : SV_Target
			{
				float cosAngle = i.uv.x * 2.0 - 1.0;// *2.0 - 1.0;
                float sinAngle = sqrt(saturate(1 - cosAngle * cosAngle));
                    
                float3 lightDir = -normalize(float3(sinAngle, cosAngle, 0));

				return PrecomputeAmbientLight(lightDir);
			}
			ENDHLSL
		}

		//Pass 2
		Pass
		{
			Name "Dir Light LUT"
			ZTest Off
			Cull Off
			ZWrite Off
			Blend Off
			
			HLSLPROGRAM
			#pragma vertex vertQuad
			#pragma fragment fragDir

			struct v2p
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			struct input
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			v2p vertQuad(input v)
			{
				v2p o;
				o.pos = TransformObjectToHClip(v.vertex.xyz);
				o.uv = v.texcoord.xy;
				return o;
			}

			float4 fragDir(v2f i) : SV_Target
			{
				float cosAngle = i.uv.x * 1.1 - 0.1;// *2.0 - 1.0;
				
				float sinAngle = sqrt(saturate(1 - cosAngle * cosAngle));				
				float3 rayDir = normalize(float3(sinAngle, cosAngle, 0));

				return PrecomputeDirLight(rayDir);
			}
			
			ENDHLSL
		}
    }
}
