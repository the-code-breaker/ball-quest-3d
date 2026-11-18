Shader "BallQuest/Boost Flow"
{
    Properties
    {
        [HDR] _BaseColor("Flow color",Color)=(0.1,1.5,2.2,1)
        _Pulse("Activation",Range(0,1))=0
        _FlowType("Drive / launch / conveyor",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Pulse;
                half _FlowType;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half rate=_FlowType>1.5?0.8:_FlowType>0.5?1.15:1.65;
                half phase=frac(input.uv.y-_Time.y*rate);
                half sweep=exp2(-38*abs(phase-0.5));
                half fine=0.5+0.5*cos((input.uv.y-_Time.y*rate)*18.84956);
                half3 color=_BaseColor.rgb*(0.42+0.62*sweep+0.10*fine);
                // A bright traveling core keeps the lights readable from the chase
                // camera while the surrounding pearl plates retain directional shading.
                color+=half3(0.42,0.55,0.6)*sweep+half3(1.1,1.1,0.86)*_Pulse;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
