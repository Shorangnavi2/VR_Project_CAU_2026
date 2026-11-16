Shader "Custom/StainedGlass"
{
    Properties
    {
        _Color ("Glass Tint Color", Color) = (1, 0, 0, 1) // 유리판 고유 색상
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        // 뒤쪽 배경과 유리 색상을 곱해서(Multiply) 실제 색셀로판지처럼 투과시킴
        Blend DstColor Zero 
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 유리판 영역만 해당 색상으로 필터링하여 투과
                return _Color;
            }
            ENDCG
        }
    }
}