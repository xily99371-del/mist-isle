Shader "MistIsle/Map Soft Terrain"
{
    Properties
    {
        [PerRendererData] _MainTex("Original terrain tile",2D)="white" {}
        _MaskTex("Mask",2D)="white" {} _NormalMap("Normal",2D)="bump" {}
        _Color("Tint",Color)=(1,1,1,1)
        _DetailContrast("Local detail contrast",Range(0,1))=.44
        _PaletteBlend("Continuous ground palette blend",Range(0,.5))=.17
        _MacroAmount("Low frequency variation",Range(0,.1))=.026
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
            #pragma vertex TerrainVertex
            #pragma fragment TerrainFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE
            struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; UNITY_SKINNED_VERTEX_INPUTS };
            struct Varyings { COMMON_2D_LIT_OUTPUTS half4 color:COLOR; float2 terrainWorld:TEXCOORD6; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half _DetailContrast; half _PaletteBlend; half _MacroAmount;
            CBUFFER_END
            Varyings TerrainVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input); SetUpSpriteInstanceProperties();
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings o=CommonLitVertex(input); o.color=input.color*_Color*unity_SpriteColor;
                o.terrainWorld=TransformObjectToWorld(input.positionOS).xy; return o;
            }
            half4 TerrainFragment(Varyings input):SV_Target
            {
                half4 original=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
                // The handoff exports independent 16px cells. A five-tap point-filtered neighbourhood
                // reduces their high-contrast speckle while preserving the original outlines and accents.
                float2 stepUV=float2(.09375,0);
                half3 mean=original.rgb;
                mean+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,saturate(input.uv+stepUV)).rgb;
                mean+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,saturate(input.uv-stepUV)).rgb;
                mean+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,saturate(input.uv+stepUV.yx)).rgb;
                mean+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,saturate(input.uv-stepUV.yx)).rgb;
                mean*=.2;
                half3 detail=mean+(original.rgb-mean)*_DetailContrast;
                half grass=saturate((mean.g-max(mean.r,mean.b))*9);
                half warm=saturate((mean.r-mean.b)*4)*(1-grass);
                half3 family=lerp(mean,half3(.63,.54,.37),warm*.45);
                family=lerp(family,half3(.31,.46,.24),grass*.55);
                detail=lerp(detail,family,_PaletteBlend);
                float macro=sin(input.terrainWorld.x*.19+sin(input.terrainWorld.y*.11))*cos(input.terrainWorld.y*.16-input.terrainWorld.x*.07);
                detail*=1+macro*_MacroAmount;
                SurfaceData2D surface; InputData2D lighting;
                half4 mask=SAMPLE_TEXTURE2D(_MaskTex,sampler_MaskTex,input.uv);
                InitializeSurfaceData(detail*input.color.rgb,original.a*input.color.a,mask,half3(0,0,1),surface);
                InitializeInputData(input.uv,input.lightingUV,lighting);
                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(lighting,input.positionWS,input.positionCS,_MainTex);
                    surface.normalWS=input.normalWS;
                #endif
                return CombinedShapeLightShared(surface,lighting);
            }
            ENDHLSL
        }
    }
}
