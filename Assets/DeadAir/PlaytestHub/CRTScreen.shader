Shader "Dead Air/CRT Screen"
{
    Properties
    {
        _BaseMap("Desktop image",2D)="white"{}
        _ScreenU("Screen horizontal projection",Vector)=(1,0,0,0)
        _ScreenV("Screen vertical projection",Vector)=(0,1,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _ScreenU;
                float4 _ScreenV;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=float2(dot(input.positionOS,_ScreenU),dot(input.positionOS,_ScreenV));return output;
            }
            half4 Frag(Varyings input):SV_Target { return half4(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,saturate(input.uv)).rgb,1); }
            ENDHLSL
        }
    }
}
