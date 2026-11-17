Shader "MistIsle/Map Flowing Water"
{
    Properties
    {
        [PerRendererData] _MainTex("Water tile", 2D)="white" {}
        _MaskTex("Mask",2D)="white" {} _NormalMap("Normal",2D)="bump" {}
        _Color("Tint",Color)=(1,1,1,1) _Flow("Flow speed",Vector)=(.023,-.011,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex WaterVertex
            #pragma fragment WaterFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE
            struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS };
            struct Varyings { COMMON_2D_LIT_OUTPUTS half4 color:COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Color; float4 _Flow; CBUFFER_END
            Varyings WaterVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input); SetUpSpriteInstanceProperties();
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings o=CommonLitVertex(input); o.color=input.color*_Color*unity_SpriteColor; return o;
            }
            half4 WaterFragment(Varyings input):SV_Target
            {
                input.uv=frac(input.uv+_Flow.xy*_Time.y+float2(sin(input.uv.y*6.283+_Time.y*.7)*.012,0));
                half4 color=CommonLitFragment(input,input.color);
                color.rgb+=sin(input.uv.y*37+_Time.y*.6)*sin(input.uv.x*19-_Time.y*.4)*.018*color.a;
                return color;
            }
            ENDHLSL
        }
    }
}
