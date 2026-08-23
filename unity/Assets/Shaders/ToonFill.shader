Shader "GrandSluggers/ToonFill"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadowTint ("Shadow", Color) = (0.42, 0.36, 0.48, 1)
        _Rim ("Rim", Color) = (0.08, 0.07, 0.1, 1)
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
                float3 light = normalize(float3(0.35, 0.82, 0.28));
                float ndl = saturate(dot(n, light));
                float band = ndl > 0.62 ? 1.0 : (ndl > 0.28 ? 0.55 : 0.0);
                float3 lit = lerp(_ShadowTint.rgb, _Color.rgb, band);
                lit = lerp(lit, _Color.rgb, 0.18);
                float rim = pow(1.0 - saturate(dot(n, normalize(i.view))), 3.0);
                lit = lerp(lit, _Rim.rgb, rim * 0.55);
                return fixed4(lit, 1);
            }
            ENDCG
        }

    }
    Fallback "Universal Render Pipeline/Unlit"
}
