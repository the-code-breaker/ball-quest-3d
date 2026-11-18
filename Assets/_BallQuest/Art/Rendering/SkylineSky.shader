Shader "BallQuest/Skyline Sky"
{
    Properties
    {
        _Top("Zenith", Color)=(.50,.46,.72,1)
        _Horizon("Horizon", Color)=(1,.80,.67,1)
        _Bottom("Cloud sea", Color)=(.80,.74,.87,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Top, _Horizon, _Bottom;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
            Varyings vert(Attributes input)
            {Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);output.direction=input.positionOS.xyz;return output;}
            half4 frag(Varyings input):SV_Target
            {
                float3 direction=normalize(input.direction);
                half3 sky=direction.y>0?lerp(_Horizon.rgb,_Top.rgb,pow(saturate(direction.y),.6)):lerp(_Horizon.rgb,_Bottom.rgb,pow(saturate(-direction.y),.35));
                float sun=pow(saturate(dot(direction,normalize(float3(-.5,.28,.85)))),90);
                sky=lerp(sky,half3(1,.94,.78),sun*.65);
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
