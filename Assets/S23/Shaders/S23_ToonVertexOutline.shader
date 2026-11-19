Shader "CG/S23/ToonVertexOutline"
{
    Properties
    {
        _BaseColor("Base Color",Color)=(1,1,1,1)
        _Ambient("Ambient",Range(0,1))=0.2
        _SpecularColor("Specular Color",Color)=(1,1,1,1)
        _Shininess("Shininess",Range(1,128))=32
        _ToonSteps("Toon Steps",Range(2,8))=3
        _OutlineColor("Outline Color",Color)=(0.025,0.035,0.045,1)
        _OutlineWidth("Outline Width",Range(0,0.1))=0.025
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        // PASS 1: vertex color and toon lighting are combined in the SAME surface calculation.
        Pass
        {
            Name "ToonVertexColor"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _SpecularColor, _OutlineColor;
                float _Ambient, _Shininess, _ToonSteps, _OutlineWidth;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float4 color:TEXCOORD2; };
            V vert(A a) { V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz); o.positionWS=TransformObjectToWorld(a.positionOS.xyz); o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.color=a.color; return o; }
            half4 frag(V i):SV_Target
            {
                float3 n=normalize(i.normalWS); Light light=GetMainLight();
                float3 v=normalize(_WorldSpaceCameraPos-i.positionWS);
                float steps=max(2,_ToonSteps);
                float diffuse=floor(saturate(dot(n,light.direction))*steps)/steps;
                float specular=pow(saturate(dot(reflect(-light.direction,n),v)),_Shininess);
                float3 surface=i.color.rgb*_BaseColor.rgb;
                return half4(surface*(_Ambient+diffuse*light.color)+_SpecularColor.rgb*specular*light.color,1);
            }
            ENDHLSL
        }
        // PASS 2: inverted hull outline. The distinct URP tag matches S20_ToonOutline_Finish.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _SpecularColor, _OutlineColor;
                float _Ambient, _Shininess, _ToonSteps, _OutlineWidth;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            float4 vert(A a):SV_POSITION { return TransformObjectToHClip(a.positionOS.xyz+a.normalOS*_OutlineWidth); }
            half4 frag():SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
