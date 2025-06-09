Shader "Custom/HealthBarSimple"
{
    Properties
    {
       [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
        _Health ("Health", Range(0,1))=1
        _Clamp ("Clamp Value", Range(0.5,1))=0.6
        _BorderRadius ("Border Radius", Range(0,0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
           
            #include "UnityCG.cginc"

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Health;
            float _Clamp;
            float _BorderRadius;

            float InverseLerp(float a, float b, float v)
            {
                return (v-a)/(b-a);
            }
            
            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (Interpolators i) : SV_Target
            {
                float2 coords = i.uv;
                coords.x*=8;
                
                // Health bar mask
                float healthbarmask = _Health > i.uv.x;
                
                // Line segment for rounded corners
                float2 LineSigment = float2(clamp(coords.x,_Clamp,8-_Clamp),0.5);
                float sdf = distance(coords,LineSigment)*2-1;
                clip(-sdf);

                float bordersdf = sdf + _BorderRadius;
                float bordermask = step(0,-bordersdf);
                
                // Calculate health color gradient directly
                float3 HealthColor;
                
                // Green to yellow to red gradient based on health
                if (_Health > 0.5) {
                    // Green to yellow (health 1.0 -> 0.5)
                    float t = saturate((1.0 - _Health) * 2.0);
                    HealthColor = lerp(float3(0.0, 1.0, 0.0), float3(1.0, 1.0, 0.0), t);
                } else {
                    // Yellow to red (health 0.5 -> 0.0)
                    float t = saturate((0.5 - _Health) * 2.0);
                    HealthColor = lerp(float3(1.0, 1.0, 0.0), float3(1.0, 0.0, 0.0), t);
                }
                
                // Flash effect for low health
                if (_Health < 0.2)
                {
                    float flash = cos(_Time.y * 4) * 0.4 + 0.6;
                    HealthColor *= flash;
                }
                
                // Combine with masks
                return float4(HealthColor * healthbarmask * bordermask, healthbarmask * bordermask);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
