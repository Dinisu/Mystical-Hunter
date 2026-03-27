Shader "Custom/Test_01"
{
    Properties
    {
        _MainTex1 ("Texture 1", 2D) = "white" {}
        _Speed1 ("Speed 1", Float) = 1.0
        _Dir1 ("Direction 1", Float) = 1.0

        _MainTex2 ("Texture 2", 2D) = "white" {}
        _Speed2 ("Speed 2", Float) = 1.0
        _Dir2 ("Direction 2", Float) = -1.0

        _Color ("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex1;
            float4 _MainTex1_ST;
            float _Speed1;
            float _Dir1;

            sampler2D _MainTex2;
            float4 _MainTex2_ST;
            float _Speed2;
            float _Dir2;

            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 各テクスチャのオフセット計算
                float offset1 = _Time.y * _Speed1 * _Dir1;
                float2 uv1 = i.uv * _MainTex1_ST.xy + _MainTex1_ST.zw + float2(offset1, 0);

                float offset2 = _Time.y * _Speed2 * _Dir2;
                float2 uv2 = i.uv * _MainTex2_ST.xy + _MainTex2_ST.zw + float2(offset2, 0);

                // 各テクスチャのカラーを取得
                fixed4 col1 = tex2D(_MainTex1, uv1);
                fixed4 col2 = tex2D(_MainTex2, uv2);

                // アルファを考慮しながらブレンド（重ねる）
                fixed4 col = col1 + col2 * (1 - col1.a);

                return col * _Color;
            }
            ENDCG
        }
    }
}