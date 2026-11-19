Shader "CG/S23/DoubleSidedSurface"
{
    Properties
    {
        _BaseMap("Base Map (RGBA)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Name "DoubleSided"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor; float _Cutoff;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; };
            V vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz); o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.uv=TRANSFORM_TEX(a.uv,_BaseMap); return o; }
            half4 frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                clip(color.a-_Cutoff);
                // Back faces remain visible and use the reversed normal for lighting.
                float3 n=normalize(i.normalWS)*IS_FRONT_VFACE(face,1.0,-1.0);
                Light light=GetMainLight();
                return half4(color.rgb*(0.4+0.6*saturate(dot(n,light.direction))*light.color),1);
            }
            ENDHLSL
        }
    }
}
