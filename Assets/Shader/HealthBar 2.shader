Shader "Unlit/HealthBar 2"
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
        Tags { "RenderType"="Trasparent" "Queue"="Transparent" }
         

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
           
            

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
                //float3 bgColor = float3(0,0,0);
                float healthbarmask =_Health > i.uv.x;
                //clip(healthbarmask-0.5);
                
                float2 LineSigment = float2(clamp(coords.x,_Clamp,8-_Clamp),0.5);
                float sdf = distance(coords,LineSigment)*2-1;
                clip(-sdf);

                float bordersdf = sdf + _BorderRadius;

                float bordermask = step(0,-bordersdf);

               // return float4(bordermask.xxx,1);
                
                float3 HealthColor = tex2D(_MainTex, float2(_Health,i.uv.y));
             //   float3 outcolor = lerp(bgColor, HealthColor,  healthbarmask);
                if (_Health < 0.2)
                {
                   float flash = cos(_Time.y * 4) * 0.4+0.6;
                    HealthColor*=flash;
                }
                //clip(healthbarmask-0.5);
                return float4(HealthColor*healthbarmask*bordermask,1);
                
            }
            ENDCG
        }
    }
}
