Shader "GrandSluggers/ToonFill"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadowTint ("Shadow", Color) = (0.42, 0.36, 0.48, 1)
        _Rim ("Rim", Color) = (1, 0.94, 0.82, 1)
        _OutlineColor ("Outline", Color) = (0.06, 0.04, 0.08, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Back
        ZWrite On

        Pass
        {
            Name "Toon"
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _ShadowTint;
            float4 _Rim;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
                float3 view : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.view = normalize(_WorldSpaceCameraPos - wpos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 view = normalize(i.view);
                float3 light = normalize(float3(0.35, 0.82, 0.28));
                float ndl = saturate(dot(n, light));
                float band = ndl > 0.55 ? 1.0 : (ndl > 0.18 ? 0.38 : 0.0);
                float3 lit = lerp(_ShadowTint.rgb, _Color.rgb, band);
                float warm = saturate((ndl - 0.72) * 4.0);
                lit = lerp(lit, _Rim.rgb, warm * 0.22);
                float ink = smoothstep(0.38, 0.72, 1.0 - saturate(dot(n, view)));
                lit = lerp(lit, _OutlineColor.rgb, ink);
                return fixed4(lit, 1);
            }
            ENDCG
        }

    }
    Fallback "Universal Render Pipeline/Unlit"
}
