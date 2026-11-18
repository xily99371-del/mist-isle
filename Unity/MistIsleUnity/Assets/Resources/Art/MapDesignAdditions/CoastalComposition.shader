Shader "MistIsle/Coastal Camp Composition"
{
    Properties
    {
        _GrassTex("Approved grass",2D)="white" {} _SandTex("Approved sand",2D)="white" {}
        _SandDetailTex("Approved sparse beach stones",2D)="white" {}
        _RockTex("Approved cliff",2D)="white" {} _SurfaceData("Authored footprint data",2D)="black" {}
        _MapSize("Map size",Vector)=(64,48,0,0) _SurfaceMode("Ground,rock,lip,wash",Float)=0
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
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half2 lightingUV:TEXCOORD1; float2 local:TEXCOORD2; half4 color:COLOR; };
            TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);
            TEXTURE2D(_SandTex); SAMPLER(sampler_SandTex);
            TEXTURE2D(_SandDetailTex); SAMPLER(sampler_SandDetailTex);
            TEXTURE2D(_RockTex); SAMPLER(sampler_RockTex);
            TEXTURE2D(_SurfaceData); SAMPLER(sampler_SurfaceData);
            CBUFFER_START(UnityPerMaterial) float4 _MapSize; half _SurfaceMode; CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS);
                output.uv=input.uv; output.local=input.positionOS.xy; output.color=input.color;
                output.lightingUV=half2(ComputeScreenPos(output.positionCS/output.positionCS.w).xy);
                return output;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 cell=floor(p), blend=frac(p); blend=blend*blend*(3-2*blend);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),blend.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),blend.x),blend.y);
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 terrainPosition=floor(input.local*16)/16;
                half4 data=SAMPLE_TEXTURE2D(_SurfaceData,sampler_SurfaceData,terrainPosition/_MapSize.xy);
                half3 albedo; half alpha=1;
                if (_SurfaceMode<.5)
                {
                    float variation=Noise(terrainPosition*.3)*.65+Noise(terrainPosition*.84)*.35;
                    float2 warp=float2(sin(terrainPosition.y*.49),cos(terrainPosition.x*.43))*.17;
                    half3 grass=SAMPLE_TEXTURE2D(_GrassTex,sampler_GrassTex,frac(terrainPosition+warp)).rgb;
                    grass=lerp(half3(.40,.54,.26),grass,.34)*( .92+variation*.16 );
                    half3 sand=SAMPLE_TEXTURE2D(_SandTex,sampler_SandTex,frac(terrainPosition*.81+warp)).rgb;
                    half3 beachDetail=SAMPLE_TEXTURE2D(_SandDetailTex,sampler_SandDetailTex,frac(terrainPosition*.81+warp)).rgb;
                    sand=lerp(sand,beachDetail,step(.89,Hash(floor(terrainPosition))));
                    sand=lerp(half3(.79,.68,.47),sand,.42)*( .96+variation*.08 );
                    float edgeNoise=(variation-.5)*.3+(Noise(terrainPosition*3.2)-.5)*.10;
                    float plaza=1-smoothstep(5.6,5.9,length((terrainPosition-float2(32,24))*float2(1,.9))+edgeNoise*1.6);
                    float route=smoothstep(.37,.65,data.r+edgeNoise);
                    float beach=1-smoothstep(11.1,11.8,terrainPosition.y+edgeNoise*2.0);
                    albedo=lerp(grass,sand,max(beach,max(route,plaza)));
                    alpha=data.a*(1-smoothstep(.3,.7,data.b));
                }
                else if (_SurfaceMode<1.5)
                {
                    clip(data.g-.5);
                    float rockU=frac(terrainPosition.x*.53);
                    half3 source=SAMPLE_TEXTURE2D(_RockTex,sampler_RockTex,lerp(float2(.10,.12),float2(.92,.65),float2(rockU,input.uv.y))).rgb;
                    half luminance=dot(source,half3(.3,.5,.2));
                    half strata=lerp(.68,1.1,smoothstep(0,1,input.uv.y));
                    float facet=Hash(floor(float2(terrainPosition.x*.64,input.uv.y*2.3)));
                    albedo=lerp(half3(.43,.46,.51),source*.4+half3(.22,.25,.31),.2)*( .78+luminance*.6 )*strata*(.88+facet*.22);
                    alpha=input.color.a;
                }
                else if (_SurfaceMode<2.5)
                {
                    half3 source=SAMPLE_TEXTURE2D(_SandTex,sampler_SandTex,frac(terrainPosition)).rgb;
                    albedo=lerp(source,half3(.51,.50,.43),.60);
                    alpha=smoothstep(0,.18,input.uv.y)*(1-smoothstep(.65,1,input.uv.y))*.86;
                }
                else if (_SurfaceMode<3.5)
                {
                    clip(data.g-.5);
                    float wave=sin(terrainPosition.x*1.7+_Time.y*1.15)*.5+.5;
                    albedo=half3(.71,.88,.84);
                    alpha=(1-abs(input.uv.y*2-1))*(.19+wave*.29);
                }
                else
                {
                    half4 source=SAMPLE_TEXTURE2D(_RockTex,sampler_RockTex,input.uv);
                    half luminance=dot(source.rgb,half3(.3,.5,.2));
                    albedo=source.rgb*.16+luminance*half3(.64,.72,.82);
                    alpha=source.a;
                }
                SurfaceData2D surface; InputData2D lighting;
                InitializeSurfaceData(albedo,alpha,half4(1,1,1,1),half3(0,0,1),surface);
                InitializeInputData(input.uv,input.lightingUV,lighting);
                return CombinedShapeLightShared(surface,lighting);
            }
            ENDHLSL
        }
    }
}
