Shader "Custom/Main_Shader"
{
   Properties
    {
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.7
        _AdditionalLightIntensity ("Additional Light Intensity", Range(0, 2)) = 1.0
        [Toggle] _DebugMode ("Debug Mode", Float) = 0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Main light shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            // Additional light shadows
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };
            
            float _ShadowIntensity;
            float _AdditionalLightIntensity;
            float _DebugMode;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Get main light shadow
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float mainShadow = mainLight.shadowAttenuation;
                float mainShadowAlpha = (1.0 - mainShadow) * _ShadowIntensity;
                
                // For additional lights
                float additionalShadowsAlpha = 0;
                float3 debugColor = float3(0, 0, 0);
                
                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    
                    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1,1,1,1));
                        
                        // Check if this is a directional light
                        // Directional lights have constant attenuation of 1.0
                        bool isDirectional = light.distanceAttenuation > 0.99;
                        
                        // For directional lights, we don't need to check NdotL
                        // For spot/point lights, check if they affect this pixel
                        float effectiveness = isDirectional ? 1.0 : 
                                             saturate(dot(input.normalWS, light.direction)) * light.distanceAttenuation;
                        
                        if (effectiveness > 0.01)
                        {
                            // Get this light's shadow
                            float shadowAtten = light.shadowAttenuation;
                            float shadowAmount = (1.0 - shadowAtten) * _AdditionalLightIntensity;
                            
                            if (isDirectional) {
                                // For directional lights, make shadows more pronounced
                                shadowAmount *= 1.5;
                            }
                            
                            // Accumulate additional shadows
                            // If debugging, store the last directional light color
                            if (shadowAmount > additionalShadowsAlpha) {
                                additionalShadowsAlpha = shadowAmount;
                                if (_DebugMode > 0.5 && isDirectional) {
                                    debugColor = light.color.rgb;
                                }
                            }
                        }
                    }
                #endif
                
                // Combine shadows from all lights
                float finalAlpha = max(mainShadowAlpha, additionalShadowsAlpha);
                
                // Use debug color if debugging
                float3 finalColor = _DebugMode > 0.5 ? debugColor : float3(0, 0, 0);
                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
