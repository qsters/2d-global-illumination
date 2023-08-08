Shader "RayMarching/RayMarchLighting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        LOD 100
        ZWrite Off
        Cull Off
        
        CGINCLUDE
            #define PI 3.14159265359
            #define TWO_PI 6.28318530718
            #define GOLDEN_ANGLE 2.3999632297281
        
            #define E 2.71828f

            #define LINEAR_TO_GAMMA_CONSTANT 0.4545454545454
        ENDCG
        

        Pass // 0
        {
            Name "RAYMARCHLIGHTING"
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

            // Texture Information
            sampler2D _MainTex;
            sampler2D _DistanceFieldTexture;
            sampler2D _EmissionTexture;
            float4 _MainTex_TexelSize;

            // Other Data
            float _frameCount;
            float _time;
            float _OneOverTimeSpan;
            int _samples; // Samples per pixel
            float2 _AspectRatio;
            float4 _ambientColor;

            // Random Function between 0 and 1
            float random01(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            // This function should return the color of the ray that is shot from the given uv and angle
            float3 trace(float2 uv, float angle)
            {
                float totalDistance = 0; //Tracks the total distance traveled
                float2 direction = float2(cos(angle), sin(angle)) / _AspectRatio.xy;
                float2 currentPos = uv;

                [unroll]
                for (int x = 0; x < 15; x++)
                {
                    // Get the distance to the nearest object
                    float currentDistance = tex2D(_DistanceFieldTexture, currentPos);

                    // Check Out of Bounds
                    float2 clampedPos = clamp(currentPos, 0, 1);
                    if (totalDistance > _AspectRatio.x || any(clampedPos != currentPos))
                    {
                        return _ambientColor.rgb;
                    }

                    // If within a certain distance, return the emission color
                    if (currentDistance < 0.001)
                    {
                        return tex2D(_EmissionTexture, currentPos).rgb;
                    }

                    // Move forward
                    currentPos += direction * currentDistance;
                    totalDistance += currentDistance;
                }

                // Return black if no object is hit
                return _ambientColor.rgb;
            }
            
            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float3 Linear_To_RGB(float3 color)
            {
                return pow(color.rgb, LINEAR_TO_GAMMA_CONSTANT);
            }

            float3 frag (v2f i) : SV_Target
            {
                // If this pixel is not emissive, return black, doing this in hope that the game will be mostly cave
                float4 thisEmissionColor = tex2D(_EmissionTexture, i.uv);
                
                if (thisEmissionColor.a == 1)
                {
                    return 0;
                }

                // Get the previous color, Set the out color to black, and get a random angle,
                float3 previousColor = tex2D(_MainTex, i.uv).rgb;
                float3 outColor = float3(0, 0, 0);
                float rand2PI = random01(i.uv + _time) * TWO_PI;
                
                [loop] // Loop for the number of samples, 
                for (float x = 0.0; x < _samples; x++)
                {
                    // Angle starts at the random number, and then is incremented by the golden angle
                    float angle = GOLDEN_ANGLE * x + rand2PI;
                    float3 sampledColor = trace(i.uv.xy, angle); // Trace returns the color shot
                    outColor += sampledColor; 
                }
                
                outColor /= _samples;
                // outColor = Linear_To_RGB(outColor); // Convert to sRGB? I Think?
                outColor = previousColor * (1 - _OneOverTimeSpan) + outColor * _OneOverTimeSpan; // Lerp between the previous color and the new color over time

                outColor = clamp(outColor, 0, 1); // Clamp the color between 0 and 1
                return outColor;
            }
            ENDCG
        }
        pass {
            name "HorizontalBlur"
            CGPROGRAM
             #pragma vertex vert
            #pragma fragment frag_horizontal

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 screenPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            uint _gridSize;
	        float _spread;

            float gaussian(int x)
            {
	            float sigmaSqu = _spread * _spread;
	            return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
            }

             v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag_horizontal (v2f i) : SV_Target
            {
	            float3 col = float3(0.0f, 0.0f, 0.0f);
	            float gridSum = 0.0f;

	            int upper = ((_gridSize - 1) / 2);
	            int lower = -upper;

	            for (int x = lower; x <= upper; ++x)
	            {
		            float gauss = gaussian(x);
		            gridSum += gauss;
		            float2 uv = i.uv + float2(_MainTex_TexelSize.x * x, 0.0f);
		            col += gauss * tex2D(_MainTex, uv).xyz;
	            }

	            col /= gridSum;
	            return float4(col, 1.0f);
            }
            ENDCG
        }
        pass {
            name "VerticalBlur"
            CGPROGRAM
             #pragma vertex vert
            #pragma fragment frag_vertical

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            uint _gridSize;
	        float _spread;

            float gaussian(int x)
            {
	            float sigmaSqu = _spread * _spread;
	            return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
            }

             v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag_vertical (v2f i) : SV_Target
	        {
		        float3 col = float3(0.0f, 0.0f, 0.0f);
		        float gridSum = 0.0f;

		        int upper = ((_gridSize - 1) / 2);
		        int lower = -upper;

		        for (int y = lower; y <= upper; ++y)
		        {
			        float gauss = gaussian(y);
			        gridSum += gauss;
			        float2 uv = i.uv + float2(0.0f, _MainTex_TexelSize.y * y);
			        col += gauss * tex2D(_MainTex, uv).xyz;
		        }

		        col /= gridSum;
		        return float4(col, 1.0f);
	        }
            ENDCG
        }
    }
}
