Shader "Custom/ChromaKeyShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _KeyColor ("Key Color", Color) = (1,1,1,1)
        _Threshold ("Threshold", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_UI_ALPHACLIP
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _KeyColor;
            float _Threshold;
            float _Smoothness;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
        fixed4 frag (v2f i) : SV_Target
        {
            fixed4 col = tex2D(_MainTex, i.uv) * i.color;
    
            // Calculate color similarity to white
            float whiteness = (col.r + col.g + col.b) / 3.0;
    
            // Calculate the color difference between pixel and key color
            float3 delta = abs(col.rgb - _KeyColor.rgb);
            float dist = length(delta);
    
            // Enhanced detection for near-white pixels
            float edgeFactor = 1.0;
            if (whiteness > 0.9) {
                edgeFactor = 0.7; // Make it easier to remove near-white pixels
            }
    
            // Apply smoothness to the alpha with edge enhancement
            float alpha = smoothstep((_Threshold * edgeFactor) - _Smoothness, 
                                   (_Threshold * edgeFactor) + _Smoothness, 
                                   dist);
    
            // Apply the alpha
            col.a *= alpha;
    
            return col;
        }

            ENDCG
        }
    }
}
