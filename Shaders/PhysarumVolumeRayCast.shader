
Shader "Physarum/VolumeRayCast" 
{
	Properties
	{
		_Absorption("Absorbtion", float) = 60.0
	}
	SubShader 
	{
		Tags { "Queue" = "Transparent" }
	
    	Pass 
    	{
    	
    		Cull front
    		Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Random.cginc"
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			
			#define NUM_SAMPLES 64
			
			float _Absorption;
			uniform float3 _Translate, _Scale, _Size;
			
			StructuredBuffer<float4> _Density;
		
			struct v2f 
			{
    			float4 pos : SV_POSITION;
    			float3 worldPos : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = UnityObjectToClipPos(v.vertex);
    			OUT.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    			return OUT;
			}
			
			struct Ray {
				float3 origin;
				float3 dir;
			};
			
			struct AABB {
			    float3 Min;
			    float3 Max;
			};
			
			//find intersection points of a ray with a box
			bool intersectBox(Ray r, AABB aabb, out float t0, out float t1)
			{
			    float3 invR = 1.0 / r.dir;
			    float3 tbot = invR * (aabb.Min-r.origin);
			    float3 ttop = invR * (aabb.Max-r.origin);
			    float3 tmin = min(ttop, tbot);
			    float3 tmax = max(ttop, tbot);
			    float2 t = max(tmin.xx, tmin.yz);
			    t0 = max(t.x, t.y);
			    t = min(tmax.xx, tmax.yz);
			    t1 = min(t.x, t.y);
			    return t0 <= t1;
			}

			float4 SampleBilinearUV(StructuredBuffer<float4> buffer, float3 uv, float3 size)
			{
				uv = saturate(uv);
				uv = uv * (size - 1.0);

				int x = uv.x;
				int y = uv.y;
				int z = uv.z;

				int X = size.x;
				int XY = size.x * size.y;

				float fx = uv.x - x;
				float fy = uv.y - y;
				float fz = uv.z - z;

				int xp1 = min(size.x - 1, x + 1);
				int yp1 = min(size.y - 1, y + 1);
				int zp1 = min(size.z - 1, z + 1);

				float4 x0 = buffer[x + y * X + z * XY] * (1.0f - fx) + buffer[xp1 + y * X + z * XY] * fx;
				float4 x1 = buffer[x + y * X + zp1 * XY] * (1.0f - fx) + buffer[xp1 + y * X + zp1 * XY] * fx;

				float4 x2 = buffer[x + yp1 * X + z * XY] * (1.0f - fx) + buffer[xp1 + yp1 * X + z * XY] * fx;
				float4 x3 = buffer[x + yp1 * X + zp1 * XY] * (1.0f - fx) + buffer[xp1 + yp1 * X + zp1 * XY] * fx;

				float4 z0 = x0 * (1.0f - fz) + x1 * fz;
				float4 z1 = x2 * (1.0f - fz) + x3 * fz;

				return z0 * (1.0f - fy) + z1 * fy;

			}
			
			float4 frag(v2f IN) : COLOR
			{
			
				float3 pos = _WorldSpaceCameraPos;
			
				Ray r;
				r.origin = pos;
				r.dir = normalize(IN.worldPos-pos);
				
				AABB aabb;
				aabb.Min = float3(-0.5,-0.5,-0.5)*_Scale + _Translate;
				aabb.Max = float3(0.5,0.5,0.5)*_Scale + _Translate;

				//figure out where ray from eye hit front of cube
				float tnear, tfar;
				intersectBox(r, aabb, tnear, tfar);
				
				//if eye is in cube then start ray at eye
				if (tnear < 0.0) tnear = 0.0;

				float3 rayStart = r.origin + r.dir * tnear;
    			float3 rayStop = r.origin + r.dir * tfar;
    			
    			//convert to texture space
    			rayStart -= _Translate;
    			rayStop -= _Translate;
   				rayStart = (rayStart + 0.5*_Scale)/_Scale;
   				rayStop = (rayStop + 0.5*_Scale)/_Scale;
   				
				float3 start = rayStart;
				float dist = distance(rayStop, rayStart);
				float stepSize = dist/float(NUM_SAMPLES);
			    float3 ds = normalize(rayStop-rayStart) * stepSize;

				float3 color = float3(0, 0, 0);
			    float alpha = 1.0;

   				for(int i=0; i < NUM_SAMPLES; i++, start += ds) 
   				{
   				 
   					float4 D = SampleBilinearUV(_Density, start, _Size);

        			//alpha *= 1.0-saturate(D.a*stepSize*_Absorption);
					alpha -= D.a;
					color += D.rgb;
        			
        			if(alpha <= 0.01) break;
			    }

				return float4(color.xyz, saturate(1-alpha));
			}
			
			ENDCG

    	}
	}
}





















