// The character toon (CF-7, CH-14): a warped half-Lambert diffuse from the main light cut into two bands, and a
// view rim so a body reads against the grass and the sky far from the lights. No specular: the fill is the identity.
// Every number comes from data/art/toon.json through Look.Body; the defaults here only keep a stray material sane.
//
// A real URP shader: the lit pass is UniversalForwardOnly, and the body also writes the depth prepass (DepthOnly), the
// depth-normals prepass (DepthNormals) and the shadow map (ShadowCaster), all in the UnityPerMaterial buffer the SRP
// Batcher needs. The old ToonFill was a built-in CG shader with one SRPDefaultUnlit pass and none of these, so the body
// was missing from every depth and shadow pass URP runs before the colour pass.
Shader "GrandSluggers/ToonRim"
{
    Properties
    {
        _BaseColor ("Fill", Color) = (1, 1, 1, 1)
        _ShadeTint ("Shade tint (multiplies the fill)", Color) = (0.6, 0.54, 0.72, 1)
        _Wrap ("Wrap (0 Lambert, 1 half-Lambert)", Range(0, 1)) = 0.5
        _BandAt ("Band at", Range(0, 1)) = 0.5
        _BandSoft ("Band soft", Range(0, 0.5)) = 0.03
        _ShadowWeight ("Received shadow weight", Range(0, 1)) = 1
        _SunFull ("Sun luminance at full fill", Float) = 1
        _SunTint ("Sun hue tint", Range(0, 1)) = 0.25
        _Ambient ("Ambient SH weight", Range(0, 1)) = 0.2
        _RimColor ("Rim", Color) = (1, 0.96, 0.86, 1)
        _RimAt ("Rim at (1 - N.V)", Range(0, 1)) = 0.62
        _RimSoft ("Rim soft", Range(0, 0.5)) = 0.05
        _RimStrength ("Rim strength", Range(0, 1)) = 0.5
        _RimUp ("Rim favours up-facing", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadeTint;
            half _Wrap;
            half _BandAt;
            half _BandSoft;
            half _ShadowWeight;
            half _SunFull;
            half _SunTint;
            half _Ambient;
            half4 _RimColor;
            half _RimAt;
            half _RimSoft;
            half _RimStrength;
            half _RimUp;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                float3 view = normalize(GetWorldSpaceViewDir(i.positionWS));
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));

                // Warped diffuse (TF2): wrap 0 is Lambert, 1 is half-Lambert, so the terminator slides round the body.
                half ndl = dot(n, sun.direction);
                half warped = saturate((ndl + _Wrap) / (1.0h + _Wrap));
                half shadow = lerp(1.0h, sun.shadowAttenuation, _ShadowWeight);
                half lit = smoothstep(_BandAt - _BandSoft, _BandAt + _BandSoft, warped) * shadow;

                // Two bands of the palette: the fill in the light, the fill times the shade tint out of it.
                half3 fill = _BaseColor.rgb;
                half3 tone = lerp(fill * _ShadeTint.rgb, fill, lit);

                // The sun sets how bright the fill is (night dims it) and lends a little of its hue, never its sheen.
                half lum = max(dot(sun.color, half3(0.299h, 0.587h, 0.114h)), 1e-3h);
                half3 hue = sun.color / lum;
                tone *= lerp(half3(1, 1, 1), hue, _SunTint) * saturate(lum / max(_SunFull, 1e-3h));
                tone += fill * SampleSH(n) * _Ambient;

                // Rim: a crisp band where the surface turns away from the camera, weighted toward the sky side.
                half edge = 1.0h - saturate(dot(n, view));
                half rim = smoothstep(_RimAt - _RimSoft, _RimAt + _RimSoft, edge);
                rim *= lerp(1.0h, saturate(n.y * 0.5h + 0.5h), _RimUp) * _RimStrength;
                tone = lerp(tone, _RimColor.rgb, rim);

                tone = MixFog(tone, i.fog);
                return half4(tone, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 toLight = normalize(_LightPosition - positionWS);
            #else
                float3 toLight = _LightDirection;
            #endif
                o.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, toLight)));
                return o;
            }

            half4 Frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half Frag(Varyings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct = PackNormalOctQuadEncode(n) * 0.5 + 0.5;
                return half4(PackFloat2To888(oct), 0);
            #else
                return half4(n, 0);
            #endif
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
