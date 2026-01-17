Shader "Custom/SpriteOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1, 0.5, 0, 1) // 기본 주황색
        _OutlineSize ("Outline Size", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord);
                c.rgb *= c.a;

                // 아웃라인 로직 (픽셀 주변 알파 체크)
                if (c.a == 0)
                {
                    fixed4 outlineC = _OutlineColor;
                    float size = _OutlineSize * _MainTex_TexelSize.x; 

                    // 상하좌우 체크
                    fixed pixelUp = tex2D(_MainTex, IN.texcoord + fixed2(0, size)).a;
                    fixed pixelDown = tex2D(_MainTex, IN.texcoord - fixed2(0, size)).a;
                    fixed pixelRight = tex2D(_MainTex, IN.texcoord + fixed2(size, 0)).a;
                    fixed pixelLeft = tex2D(_MainTex, IN.texcoord - fixed2(size, 0)).a;

                    if (pixelUp + pixelDown + pixelRight + pixelLeft > 0)
                    {
                        return outlineC;
                    }
                }

                return c * IN.color;
            }
            ENDCG
        }
    }
}