Shader "Blockout/Facility Flashlight Volume"
{
    Properties { _BeamColor("Beam color",Color)=(1,.95,.85,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; };
            float4x4 _BeamWorldToLocal;
            float4 _BeamParams, _BeamColor;
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.positionCS.xy/_ScaledScreenParams.xy;
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 scene=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float3 camera=GetCameraPositionWS();
                float3 delta=i.positionWS-camera;
                float distanceToBack=length(delta);
                float3 ray=delta/max(distanceToBack,.0001);
                float maxDistance=min(distanceToBack,length(scene-camera));
                // Skip the empty space before the light's bounding volume.
                float3 localCamera=mul(_BeamWorldToLocal,float4(camera,1)).xyz;
                float3 localRay=mul((float3x3)_BeamWorldToLocal,ray);
                float3 safeRay=localRay+float3(1e-6,1e-6,1e-6);
                float radius=_BeamParams.x*_BeamParams.y;
                float3 t0=(float3(-radius,-radius,0)-localCamera)/safeRay;
                float3 t1=(float3(radius,radius,_BeamParams.x)-localCamera)/safeRay;
                float3 nearPlane=min(t0,t1),farPlane=max(t0,t1);
                float begin=max(0,max(nearPlane.x,max(nearPlane.y,nearPlane.z)));
                float end=min(maxDistance,min(farPlane.x,min(farPlane.y,farPlane.z)));
                if(end<=begin)discard;
                float stepSize=(end-begin)/40;
                float sum=0;
                [loop] for(int n=0;n<40;n++)
                {
                    float3 p=localCamera+localRay*(begin+(n+.5)*stepSize);
                    float radial=length(p.xy)/max(.02,p.z*_BeamParams.y);
                    float edge=1-smoothstep(.6,1,radial);
                    float fade=smoothstep(.05,.3,p.z)*(1-smoothstep(_BeamParams.x*.65,_BeamParams.x,p.z));
                    float dust=.85+.15*sin(p.x*19+p.y*13+p.z*5+_Time.y*.2);
                    sum+=edge*fade*dust*stepSize/(1+p.z*.12);
                }
                float opacity=1-exp(-sum*_BeamParams.z*_BeamParams.w);
                return half4(_BeamColor.rgb,min(opacity,.48));
            }
            ENDHLSL
        }
    }
}
