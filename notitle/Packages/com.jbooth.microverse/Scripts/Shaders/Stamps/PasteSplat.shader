Shader "Hidden/MicroVerse/PasteSplat"
{
    Properties
    {


    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _USEFALLOFF _USEFALLOFFRANGE _USEFALLOFFTEXTURE _USEFALLOFFSPLINEAREA
            #pragma shader_feature_local_fragment _ _MEGASPLAT

            #if _MEGASPLAT
                #define TEXCOUNT 256
            #else
                #define TEXCOUNT 32
            #endif

            #include_with_pragmas "UnityCG.cginc"
            #include_with_pragmas "/../SplatMerge.cginc"
            #include_with_pragmas "/../Filtering.cginc"


            sampler2D _IndexMap;
            sampler2D _WeightMap;
            sampler2D _OrigIndexMap;
            sampler2D _OrigWeightMap;
            float _Channels[TEXCOUNT];

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 stampUV : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.stampUV = mul(_Transform, float4(v.uv, 0, 1)).xy;
                return o;
            }

            FragmentOutput frag(v2f i)
            {
                half4 weightMap = tex2D(_WeightMap, i.stampUV);
                half4 indexMap = round(tex2D(_IndexMap, i.stampUV) * TEXCOUNT);

                half4 origWeightMap = tex2D(_OrigWeightMap, i.uv);
                half4 origIndexMap = round(tex2D(_OrigIndexMap, i.uv) * TEXCOUNT);

                FragmentOutput o;
                bool cp = (i.stampUV.x <= 0 || i.stampUV.x >= 1 || i.stampUV.y <= 0 || i.stampUV.y >= 1);
                if (cp)
                {
                    o.indexMap = origIndexMap / TEXCOUNT;
                    o.weightMap = origWeightMap;
                    return o;
                }

                float mask = 1;
                float falloff = ComputeFalloff(i.uv, i.stampUV, float2(0, 0), 0);
                mask *= falloff;

                indexMap[0] = _Channels[indexMap[0]];
                indexMap[1] = _Channels[indexMap[1]];
                indexMap[2] = _Channels[indexMap[2]];
                indexMap[3] = _Channels[indexMap[3]];
                weightMap *= mask;

                o = FilterSplatWeights(weightMap.x, origWeightMap, origIndexMap, indexMap.x);
                o = FilterSplatWeights(weightMap.y, o.weightMap, o.indexMap, indexMap.y);
                o = FilterSplatWeights(weightMap.z, o.weightMap, o.indexMap, indexMap.z);
                o = FilterSplatWeights(weightMap.w, o.weightMap, o.indexMap, indexMap.w);

                o.indexMap /= 32;
                return o;
            }
            ENDCG
        }
    }
}