Shader "Unlit/SamplerShader"
{
    Properties
    {
     [NoScaleOffset]
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0,1,0,1)
       [HideInInspector] _TextureSamplingScale("Sampling Scale", Range(0,0.1))=0.01
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
                
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color;
            float  _TextureSamplingScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.worldPos*_TextureSamplingScale);
                return col;
            }
            ENDCG
        }
    }
}
