Shader "GridForge/ChiikawaCombatFaceV01"
{
    Properties
    {
        _MainTex ("Original expression atlas (4x4)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FaceIndex ("Expression index", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        float _FaceIndex;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            float tile = clamp(floor(_FaceIndex + 0.5), 0.0, 14.0);
            float2 cell = float2(fmod(tile, 4.0), 3.0 - floor(tile / 4.0));
            // Half a source texel keeps bilinear filtering inside the original image.
            float2 uv = (cell + clamp(IN.uv_MainTex, 0.5 / 1024.0, 1.0 - 0.5 / 1024.0)) / 4.0;
            fixed4 color = tex2D(_MainTex, uv) * _Color;
            o.Albedo = color.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
