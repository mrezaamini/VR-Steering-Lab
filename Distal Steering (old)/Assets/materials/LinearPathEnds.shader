Shader "Custom/LinearPathStartEndXR"
{
    Properties
    {
        _StartColor ("Start Color", Color) = (0,1,0,1)
        _MainColor  ("Main Color", Color)  = (1,1,1,1)
        _EndColor   ("End Color", Color)   = (1,0,0,1)
        _EndPercent ("End Percent", Range(0,0.5)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _StartColor;
            fixed4 _MainColor;
            fixed4 _EndColor;
            float _EndPercent;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float x = i.uv.x;

                if (x < _EndPercent)
                    return _StartColor;

                if (x > (1.0 - _EndPercent))
                    return _EndColor;

                return _MainColor;
            }
            ENDCG
        }
    }
}