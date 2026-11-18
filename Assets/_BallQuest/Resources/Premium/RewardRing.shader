Shader "BallQuest/Reward Ring"
{
    Properties { [HDR] _BaseColor("Ring color",Color)=(1,0.75,0.2,1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; };
            Varyings Vert(Attributes input) { Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);return output; }
            half4 Frag(Varyings input):SV_Target {return _BaseColor;}
            ENDHLSL
        }
    }
}
