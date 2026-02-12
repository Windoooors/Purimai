Shader "UI/HoleMask"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Center ("Hole Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Hole Radius", Range(0, 1)) = 0.2
        _Aspect ("Aspect Ratio", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            float4 _Center;
            float _Radius;
            float _Aspect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv.xyxy;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv.xy;
                float2 diff = uv - _Center.xy;
                diff.x *= _Aspect;

                float dist = length(diff);

                if (dist < _Radius)
                {
                    discard;
                }

                return i.color;
            }
            ENDHLSL
        }
    }
}