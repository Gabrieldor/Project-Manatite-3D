Shader "Manatite/FlatColorSkybox"
{
    // Uniform, direction-independent skybox: renders a single color in every
    // direction with no ground/horizon. Placeholder for the custom space shader.
    Properties
    {
        _Tint ("Tint Color", Color) = (0.015, 0.02, 0.04, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            fixed4 _Tint;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Tint;
            }
            ENDCG
        }
    }
}
