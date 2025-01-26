// Author: Mario A. Gutierrez
// Description: Bubble wrap type material.
// Contact: mariosubspace@gmail.com
// License: No license.
// Feel free to adapt for your needs, but the math and setup is extremely tailored to this project.

Shader "Custom/BubbleWrap"
{
    Properties
    {
        // Base and reflection are more like light colors.
        _Color ("Ambient/Base Light Color", Color) = ( 1, 1, 1, 1 )
        _ReflectionColor ("Light Color", Color) = ( 1, 1, 1, 1 )
        _LightDirection ("Light Direction", Vector) = ( 0, 0, 1, 0 )
        _FresnelStrength ("Fresnel Strength", Float) = 0.5
        _SpecStrength ("Specular Strength", Float) = 0.5
        _LambertStrength ("Lambert Strength", Float) = 1
        [NoScaleOffset] _LightNoiseTex ("Light Noise", 2D) = "white" {}
        _LightNoiseScale ("Light Noise", Float) = 0.5
        _LightNoiseStrength ("Light Noise Strength", Float) = 0.5
        _LightNoiseOffset ("Light Noise Offset", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 positionNDC : TEXCOORD2;
            };

            sampler2D _LightNoiseTex;

            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float4 _Color;
            float4 _ReflectionColor;
            float4 _LightDirection;
            float _FresnelStrength;
            float _SpecStrength;
            float _LambertStrength;

            float _LightNoiseScale;
            float _LightNoiseStrength;
            float4 _LightNoiseOffset;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz); 

                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS.xyz);

                // NDC/Screen-relative coords.
                OUT.positionNDC = ComputeNormalizedDeviceCoordinates(IN.positionOS.xyz, UNITY_MATRIX_MVP);

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Renormalize because interpolation may alter length.
                float3 normalWS = normalize(IN.normalWS);

                // Note: _WorldSpaceCameraPos is set by legacy code, this may break
                // whenever Unity decides to update that. It seems this is still what they're using too.
                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);

                // 1 when normal is toward camera, 0 when normal is perpendicular or behind object.
                float NdotVSat = max(0, dot(normalWS, viewDirectionWS));
                // Fresnel mask goes from 0 to 1 at the edges of the object.
                float fresnelMask = 1.0 - NdotVSat;
                fresnelMask *= fresnelMask;
                fresnelMask *= _FresnelStrength;

                // Direction toward light.
                float3 L = normalize(_LightDirection.xyz);

                float NdotL = clamp(dot(normalWS, L), 0, 1);

                // Half vector between light and view dir.
                float3 H = normalize(L + viewDirectionWS);
                float NdotH = clamp(dot(normalWS, H), 0, 1);
                float spec = NdotH * NdotH;
                spec *= spec;
                spec *= spec;
                spec *= spec;
                spec *= spec;
                spec *= spec;
                spec *= spec;
                //spec *= spec;

                float noiseX = dot(normalWS, float3(1,0,0));
                float noiseY = dot(normalWS, float3(0,1,0));

                float lightNoise = tex2D(_LightNoiseTex, (float2(noiseX,noiseY)+_LightNoiseOffset.xy)*_LightNoiseScale).r*_LightNoiseStrength;
                
                float light = clamp(fresnelMask + NdotL*_LambertStrength + spec*_SpecStrength + lightNoise, 0, 1);
                float4 color = lerp(_Color, _ReflectionColor, light);

                return float4(color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
