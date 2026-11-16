Shader "Custom/Revealing Under Light"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
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

        fixed4 _Color;
        float4 _LightPos;
        float4 _LightDir;
        float _LightAngle;
        float _StrengthScalor;

        // 유리판 투과 계산용 변수
        float4x4 _GlassWorldToLocal;
        fixed4 _GlassColor;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. 손전등 원뿔 영역 판정
            float3 Dir = normalize(_LightPos.xyz - IN.worldPos);
            float Scale = dot(Dir, -_LightDir.xyz);
            float strength = Scale - cos(_LightAngle * (3.14 / 360.0));
            strength = saturate(strength * _StrengthScalor);

            // 2. 손전등 광원 -> 글자 픽셀 사이의 직선이 유리판(로컬 Z=0 평면)을 관통하는지 계산
            float3 localLightPos = mul(_GlassWorldToLocal, float4(_LightPos.xyz, 1.0)).xyz;
            float3 localPixelPos = mul(_GlassWorldToLocal, float4(IN.worldPos, 1.0)).xyz;
            float3 rayDir = localPixelPos - localLightPos;

            float glassMask = 0.0;
            if (abs(rayDir.z) > 0.0001)
            {
                // 광선과 유리판 평면의 교차점 비율(t) 계산
                float t = -localLightPos.z / rayDir.z;
                // 광원과 글자 '사이'에 유리판이 존재할 때만 통과 (0 < t < 1)
                if (t > 0.0 && t < 1.0)
                {
                    float3 hitPoint = localLightPos + rayDir * t;
                    // 유니티 기본 Cube/Quad 로컬 좌표 범위(-0.5 ~ 0.5) 안에 들어왔는지 체크
                    if (abs(hitPoint.x) <= 0.5 && abs(hitPoint.y) <= 0.5)
                    {
                        glassMask = 1.0;
                    }
                }
            }

            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * _GlassColor;
            float finalAlpha = strength * glassMask * c.a;

            o.Albedo = c.rgb;
            o.Emission = c.rgb * finalAlpha;
            o.Alpha = finalAlpha;
        }
        ENDCG
    }
}