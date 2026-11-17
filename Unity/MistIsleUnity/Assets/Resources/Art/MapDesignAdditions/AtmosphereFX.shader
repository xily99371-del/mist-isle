Shader "MistIsle/Map Atmosphere FX"
{
    Properties { [PerRendererData] _MainTex("Texture", 2D) = "white" {} _Color("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Output { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial) half4 _Color; CBUFFER_END
            Output Vert(Input input) { Output o; o.positionCS=TransformObjectToHClip(input.positionOS); o.uv=input.uv; o.color=input.color*_Color; return o; }
            half4 Frag(Output input):SV_Target { return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)*input.color; }
            ENDHLSL
        }
    }
}
