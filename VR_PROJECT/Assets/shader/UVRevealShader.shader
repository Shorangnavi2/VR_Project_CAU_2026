Shader "Custom/Revealing Under Light"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _LightDir ("Light Direction", Vector) = (0,0,1,0)
        _LightPos ("Light Position", Vector) = (0,0,0,0)
        _LightAngle ("Light Angle", Range(0, 180)) = 45
        _StrengthScalor ("Strength", Float) = 50
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float4 _LightPos;
        float4 _LightDir;
        float _LightAngle;
        float _StrengthScalor;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 Dir = normalize(_LightPos.xyz - IN.worldPos);
            float Scale = dot(Dir, -_LightDir.xyz);
            float strength = Scale - cos(_LightAngle * (3.14 / 360.0));
            strength = min(max(strength * _StrengthScalor, 0), 1);

            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Emission = c.rgb * c.a * strength;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = strength * c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}