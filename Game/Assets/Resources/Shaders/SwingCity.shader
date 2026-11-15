// Buildings and ground. One cheap pass: a colour per vertex, fixed sunlight from the surface
// direction, windows drawn from the wall coordinates, and distance haze. It uses no lights,
// shadows or textures, which is what keeps the whole city inside the weight budget.
Shader "Swing/City"
{
    Properties
    {
        _HazeColor ("Haze colour", Color) = (0.66, 0.78, 0.92, 1)
        _HazeStart ("Haze starts at (m)", Float) = 300
        _HazeEnd ("Haze is full at (m)", Float) = 2600
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _HazeColor;
            float _HazeStart;
            float _HazeEnd;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;     // walls: metres along the wall, metres above the ground
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float distance : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.distance = distance(world, _WorldSpaceCameraPos);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.normal);
                float3 sun = normalize(float3(0.35, 0.8, 0.45));
                float light = 0.55 + 0.45 * saturate(dot(normal, sun));

                // Windows: only on walls, one per 3.2 m across and 3.6 m up, fading out with
                // distance so far buildings do not shimmer.
                float isWall = step(abs(normal.y), 0.5);
                float2 cell = frac(i.uv / float2(3.2, 3.6));
                float window = step(0.2, cell.x) * step(cell.x, 0.8) * step(0.3, cell.y) * step(cell.y, 0.8);
                float nearby = saturate(1.0 - (i.distance - 120.0) / 260.0);
                float glass = isWall * lerp(0.36, window, nearby);

                fixed3 colour = i.color.rgb * light;
                colour = lerp(colour, colour * fixed3(0.45, 0.55, 0.7), glass);

                float haze = saturate((i.distance - _HazeStart) / (_HazeEnd - _HazeStart));
                colour = lerp(colour, _HazeColor.rgb, haze);
                return fixed4(colour, 1);
            }
            ENDCG
        }
    }
}
