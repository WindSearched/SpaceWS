Shader "UI/OutlineInnerOuter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,5)) = 1
        _OutlineMode ("0=Outer 1=Inner 2=Both", Range(0,2)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineMode;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldPosition = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offset = _OutlineWidth * _MainTex_TexelSize.xy;

                float alphaCenter = tex2D(_MainTex, i.uv).a;

                float alphaSum = 0;

                alphaSum += tex2D(_MainTex, i.uv + float2( offset.x, 0)).a;
                alphaSum += tex2D(_MainTex, i.uv + float2(-offset.x, 0)).a;
                alphaSum += tex2D(_MainTex, i.uv + float2(0,  offset.y)).a;
                alphaSum += tex2D(_MainTex, i.uv + float2(0, -offset.y)).a;

                alphaSum /= 4.0;

                float outer = (alphaCenter == 0 && alphaSum > 0);
                float inner = (alphaCenter > 0 && alphaSum < 1);

                float outline = 0;

                if (_OutlineMode < 0.5)        outline = outer;
                else if (_OutlineMode < 1.5)   outline = inner;
                else                           outline = max(outer, inner);

                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                col = lerp(col, _OutlineColor, outline);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                return col;
            }
            ENDCG
        }
    }
}