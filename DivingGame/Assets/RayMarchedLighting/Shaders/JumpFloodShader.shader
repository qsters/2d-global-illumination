Shader "RayMarching/JumpFloodShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        tags { "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Cull Off
        
          CGINCLUDE
        // just inside the precision of a R16G16_SNorm to keep encoded range 1.0 >= and > -1.0
            #define FLOOD_NULL_POS 0.0
            #define FLOOD_NULL_POS_FLOAT4 float4(FLOOD_NULL_POS, FLOOD_NULL_POS, FLOOD_NULL_POS, FLOOD_NULL_POS)
        ENDCG
        
        Pass // 0
        {
            Name "UVSILHOUETTE"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 screenPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                half mask = tex2D(_MainTex, i.uv).a;
                if (mask <= 0) // discard if alpha is 0
                {
                    discard;
                }
                mask = step(0.99, mask); // do not draw if alpha is less than 1, not sure why i did this
                return float4(i.screenPos.xy, 1.0, 0.0) * mask; // Draw screen position
            }
            ENDCG
        }
        
        Pass // 1
        {
            Name "JUMPFLOOD"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma target 4.5

            struct appdata
            {
                float4 vertex : POSITION;
                float4 texcord: TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            Texture2D _MainTex;
            float4 _MainTex_TexelSize;
            int _StepLength;
            float2 _AspectRatio;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = v.texcord;
                return o;
            }

            float4 frag (v2f i) : SV_Target {
                // integer pixel position
                int2 uvInt = int2(i.pos.xy);
                
                // initialize best distance at infinity
                float bestDist = 1.#INF;
                float4 bestCoord;
                
                // jump samples
                UNITY_UNROLL
                for(int u=-1; u<=1; u++)
                {
                    UNITY_UNROLL
                    for(int v=-1; v<=1; v++)
                    {
                        // calculate offset sample position
                        int2 offsetUV = uvInt + int2(u, v) * _StepLength;
                        
                        // decode position from buffer, if out of bounds should (I hope) return all 0's
                        float4 sensingColorPosition = _MainTex.Load(int3(offsetUV, 0));
                        
                        // the offset from current position
                        float2 disp = i.screenPos.xy - sensingColorPosition;
                        disp *= _AspectRatio;
                        
                        // square distance
                        float dist = dot(disp, disp);
                
                        // if offset position isn't a null position and is closer than the best
                        // set as the new best and store the position
                        if (sensingColorPosition.b != FLOOD_NULL_POS && dist < bestDist)
                        {
                            bestDist = dist;
                            bestCoord = sensingColorPosition;
                        }
                    }
                }
              
                // if not valid best distance output null position, otherwise output encoded position
                return isinf(bestDist) ? FLOOD_NULL_POS_FLOAT4 : bestCoord;
            }
            ENDCG
        }
        Pass // 2
        {
            Name "DISTANCEFIELD"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 screenPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float2 _AspectRatio;

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = v.texcoord;
                return o;
            }

            float3 frag (v2f i) : SV_Target
            {
                float2 colorPosition = tex2D(_MainTex, i.screenPos.xy).xy;

                float2 thisPosition = i.screenPos.xy;
                
                float2 displacement = colorPosition - thisPosition;
                displacement *= _AspectRatio;

                float distance = length(displacement);
                
                // Distance Precision for zero distance is < 0.001
                return distance;
            }
            
            ENDCG
        }
    }
}
