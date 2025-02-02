Shader "Unlit/KaiShader"
{
    Properties{
    [NoScaleOffset] kai_map_tex ("Kai Spawn Map", 2D) = "white" {}
        [HideInInspector]
        _TextureSamplingScale("Sampling Scale", Range(0,0.1))=0.01
        _KaiSpawnMapEnabled("Kai Spawn Map Enabled", Int) = 0
        
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

            sampler2D albert_map_tex;
            sampler2D kai_map_tex;
            float  _TextureSamplingScale;
            int _AlbertSpawnMapEnabled;
            int _KaiSpawnMapEnabled;
            

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
                if(_KaiSpawnMapEnabled == 1)
                {
                    fixed4 col = tex2D(kai_map_tex, i.worldPos*_TextureSamplingScale*30);
                    return col;    
                }
                return 0;
            }
            ENDCG
        }
    }
}
