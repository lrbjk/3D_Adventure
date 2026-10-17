Shader "AtmosphereInscatter/SkyBox"
{
    Properties
    {

    }
    SubShader
    {
        Tags
        {
            "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox"
        }
        Cull Off ZWrite Off
        
        Pass
        {
            Name "SkyBox"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
                        
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "./AtmosphereInscatterLUT.hlsl"

                        float3 _CameraPos;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 vertex : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex);
                o.vertex = v.vertex;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {

                float3 rayStart = _CameraPos;
                float3 rayDir = normalize(mul((float3x3)unity_ObjectToWorld, i.vertex));
                float3 lightDir = -normalize(GetMainLight().direction).xyz;
                float3 planetCenter = _CameraPos;
                planetCenter = float3(0, -_PlanetRadius, 0);
                float4 scatterR = 0;
                float4 scatterM = 0;
                float height = length(rayStart - planetCenter) - _PlanetRadius;
                float3 normal = normalize(rayStart - planetCenter);
                float viewZenith = dot(normal, rayDir);
                float sunZenith = dot(normal, -lightDir);
                float3 coords = float3(height / _AtmosphereHeight, viewZenith * 0.5 + 0.5, sunZenith * 0.5 + 0.5);
                coords.x = pow(height / _AtmosphereHeight, 0.5);
                float ch = -(sqrt(height * (2 * _PlanetRadius + height)) / (_PlanetRadius + height));
                if (viewZenith > ch)
                {
                    coords.y = 0.5 * pow((viewZenith - ch) / (1 - ch), 0.2) + 0.5;
                }
                else
                {
                    coords.y = 0.5 * pow((ch - viewZenith) / (ch + 1), 0.2);
                }
                coords.z = 0.5 * ((atan(max(sunZenith, -0.1975) * tan(1.26 * 1.1)) / 1.1) + (1 - 0.26));
                
                scatterR = tex3D(_SkyboxLUT, coords);

                scatterM.xyz = scatterR.xyz * ((scatterR.w) / (scatterR.x));

                float3 m = scatterM;
                ApplyPhaseFunctionElek(scatterR.xyz, scatterM.xyz, dot(rayDir, -lightDir.xyz));
                float3 lightInscatter = (scatterR * _ScatteringR + scatterM * _ScatteringM) * _IncomingLight.xyz;
				lightInscatter += RenderSun(m, dot(rayDir, -lightDir.xyz)) * _SunIntensity;
                
                return float4(max(0, lightInscatter), 1);
            }

            ENDHLSL
        }


    }
}
