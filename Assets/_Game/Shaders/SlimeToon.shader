Shader "BubbleMind/Slime Toon"
{
    Properties
    {
        [MainColor] _BaseColor("Base Tint", Color) = (1, 1, 1, 1)
        _TopColor("Top Color", Color) = (0.58, 0.82, 1, 1)
        _BottomColor("Bottom Color", Color) = (0.10, 0.24, 0.58, 1)
        _ShadowColor("Toon Shadow", Color) = (0.31, 0.35, 0.62, 1)
        _GradientOffset("Gradient Offset", Range(-1, 1)) = 0.42
        _GradientScale("Gradient Scale", Range(0.05, 4)) = 0.9
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.48
        _ShadowSoftness("Shadow Softness", Range(0.001, 0.35)) = 0.08

        _InnerColor("Inner Jelly Color", Color) = (0.32, 0.52, 1, 1)
        _InnerStrength("Inner Color Strength", Range(0, 1)) = 0.34
        _ThicknessStrength("Fake Thickness", Range(0, 1)) = 0.42
        _FresnelColor("Soft Edge Color", Color) = (0.72, 0.88, 1, 1)
        _FresnelPower("Soft Edge Power", Range(0.5, 8)) = 3.2
        _FresnelStrength("Soft Edge Strength", Range(0, 2)) = 0.46
        _HighlightColor("Jelly Highlight", Color) = (1, 1, 1, 1)
        _HighlightThreshold("Highlight Threshold", Range(0, 1)) = 0.88
        _HighlightSoftness("Highlight Softness", Range(0.001, 0.25)) = 0.045
        _HighlightStrength("Highlight Strength", Range(0, 2)) = 0.62

        _Realism("Realistic Lighting Blend", Range(0, 1)) = 0.72
        _Roughness("Surface Roughness", Range(0.02, 0.9)) = 0.22
        _CoatStrength("Gel Clear Coat", Range(0, 2)) = 0.72
        _CoatRoughness("Clear Coat Roughness", Range(0.02, 0.8)) = 0.16
        _MicroSurfaceScale("Micro Surface Scale", Range(1, 48)) = 17
        _MicroSurfaceStrength("Micro Roughness", Range(0, 0.5)) = 0.12
        _AbsorptionColor("Deep Absorption", Color) = (0.08, 0.015, 0.18, 1)
        _AbsorptionStrength("Deep Absorption Strength", Range(0, 1)) = 0.58
        _TransmissionStrength("Jelly Transmission", Range(0, 2)) = 0.72

        _NebulaColorA("Nebula Color A", Color) = (0.22, 0.035, 0.58, 1)
        _NebulaColorB("Nebula Color B", Color) = (0.68, 0.10, 0.88, 1)
        _NebulaScale("Nebula Scale", Range(0.5, 12)) = 3.8
        _NebulaStrength("Nebula Strength", Range(0, 2)) = 0.56
        _NebulaSpeed("Nebula Drift Speed", Range(0, 2)) = 0.16

        _StarColor("Internal Star Color", Color) = (0.82, 0.92, 1, 1)
        _StarDensity("Internal Star Density", Range(0, 0.45)) = 0.08
        _StarScale("Internal Star Scale", Range(2, 64)) = 18
        _StarStrength("Internal Star Strength", Range(0, 3)) = 0.7
        _StarSpeed("Internal Star Pulse Speed", Range(0, 8)) = 1.4

        _Opacity("Controlled Opacity", Range(0.55, 1)) = 1
        [HideInInspector] _VfxDarken("VFX Darken", Range(0, 1)) = 0
        [HideInInspector] _VfxDissolve("VFX Dissolve", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "SlimeToonForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero
            AlphaToMask On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SlimeVertex
            #pragma fragment SlimeFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TopColor;
                half4 _BottomColor;
                half4 _ShadowColor;
                half4 _InnerColor;
                half4 _FresnelColor;
                half4 _HighlightColor;
                half4 _AbsorptionColor;
                half4 _NebulaColorA;
                half4 _NebulaColorB;
                half4 _StarColor;
                half _GradientOffset;
                half _GradientScale;
                half _ShadowThreshold;
                half _ShadowSoftness;
                half _InnerStrength;
                half _ThicknessStrength;
                half _FresnelPower;
                half _FresnelStrength;
                half _HighlightThreshold;
                half _HighlightSoftness;
                half _HighlightStrength;
                half _Realism;
                half _Roughness;
                half _CoatStrength;
                half _CoatRoughness;
                half _MicroSurfaceScale;
                half _MicroSurfaceStrength;
                half _AbsorptionStrength;
                half _TransmissionStrength;
                half _NebulaScale;
                half _NebulaStrength;
                half _NebulaSpeed;
                half _StarDensity;
                half _StarScale;
                half _StarStrength;
                half _StarSpeed;
                half _Opacity;
                half _VfxDarken;
                half _VfxDissolve;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float2 Hash22(float2 value)
            {
                float first = Hash21(value);
                float second = Hash21(value + 19.19);
                return float2(first, second);
            }

            float InterleavedGradientNoise(float2 pixelPosition)
            {
                return frac(52.9829189 * frac(dot(floor(pixelPosition), float2(0.06711056, 0.00583715))));
            }

            half InternalStarMask(float3 positionOS)
            {
                float2 projected = positionOS.xz + positionOS.xy * 0.19 + positionOS.yz * 0.11;
                float2 gridUv = projected * max(_StarScale, 0.001);
                float2 cell = floor(gridUv);
                float2 starOffset = (Hash22(cell) - 0.5) * 0.48;
                float distanceToPoint = length(frac(gridUv) - 0.5 - starOffset);
                float randomValue = Hash21(cell + 7.31);
                float radius = lerp(0.055, 0.14, Hash21(cell + 3.73));
                float visible = step(1.0 - _StarDensity, randomValue);
                float pulse = 0.68 + 0.32 * sin(_Time.y * _StarSpeed + randomValue * TWO_PI);
                return (1.0 - smoothstep(radius, radius + 0.035, distanceToPoint)) * visible * pulse;
            }

            half MicroSurface(float3 positionOS)
            {
                float3 samplePosition = positionOS * max(_MicroSurfaceScale, 0.001h);
                half firstWave = sin(samplePosition.x * 1.07 + samplePosition.y * 0.73 + samplePosition.z * 0.41);
                half secondWave = sin(samplePosition.x * -0.37 + samplePosition.y * 1.31 + samplePosition.z * 0.89);
                half thirdWave = sin(samplePosition.x * 0.61 - samplePosition.y * 0.47 + samplePosition.z * 1.43);
                return saturate(0.5h + (firstWave + secondWave + thirdWave) / 6.0h);
            }

            half InternalNebula(float3 positionOS, out half colorBlend)
            {
                float drift = _Time.y * _NebulaSpeed;
                float3 samplePosition = positionOS * max(_NebulaScale, 0.001h);
                samplePosition += float3(drift * 0.37, -drift * 0.19, drift * 0.23);
                half warp = sin(samplePosition.y * 1.37h + sin(samplePosition.z * 0.71h));
                half primary = 0.5h + 0.5h * sin(samplePosition.x * 1.21h + samplePosition.z * 0.83h + warp * 1.7h);
                half secondary = 0.5h + 0.5h * sin(samplePosition.y * 0.91h - samplePosition.x * 0.64h + sin(samplePosition.z));
                colorBlend = saturate(primary * 0.68h + secondary * 0.42h - 0.18h);
                return smoothstep(0.50h, 0.86h, primary * secondary + primary * 0.32h);
            }

            void AccumulateJellyLight(
                Light additionalLight,
                half3 normalWS,
                half3 viewDirectionWS,
                half3 bodyColor,
                half specularPower,
                half coatPower,
                inout half3 accumulatedColor)
            {
                half3 additionalDirection = normalize(additionalLight.direction);
                half additionalAttenuation = additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
                half additionalNdotL = saturate(dot(normalWS, additionalDirection));
                half3 additionalHalf = SafeNormalize(additionalDirection + viewDirectionWS);
                half additionalNdotH = saturate(dot(normalWS, additionalHalf));
                half additionalSpecular = pow(additionalNdotH, specularPower) * (1.0h - _Roughness);
                half additionalCoat = pow(additionalNdotH, coatPower) * _CoatStrength;
                half additionalTransmission = saturate(dot(-normalWS, additionalDirection));

                accumulatedColor += bodyColor * additionalLight.color * additionalNdotL * additionalAttenuation *
                    lerp(0.32h, 0.78h, _Realism);
                accumulatedColor += _HighlightColor.rgb * additionalLight.color *
                    (additionalSpecular * _HighlightStrength + additionalCoat) * additionalAttenuation * _Realism;
                accumulatedColor += _InnerColor.rgb * additionalLight.color * additionalTransmission * additionalAttenuation *
                    _TransmissionStrength * _ThicknessStrength * 0.62h;
            }

            Varyings SlimeVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.positionOS = input.positionOS.xyz;
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 SlimeFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half effectiveOpacity = saturate(_Opacity * (1.0h - _VfxDissolve));
                clip(effectiveOpacity - InterleavedGradientNoise(input.positionCS.xy));

                half3 normalWS = normalize(input.normalWS);
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lightDirectionWS = normalize(mainLight.direction);

                half heightGradient = saturate(input.positionOS.y * _GradientScale + _GradientOffset);
                half3 bodyColor = lerp(_BottomColor.rgb, _TopColor.rgb, heightGradient) * _BaseColor.rgb;

                half nDotV = saturate(dot(normalWS, viewDirectionWS));
                half centerThickness = pow(nDotV, 0.62h);
                half absorption = saturate(centerThickness * _AbsorptionStrength);
                half3 absorbedColor = bodyColor * lerp(half3(1.0h, 1.0h, 1.0h), _AbsorptionColor.rgb * 1.8h, absorption);
                bodyColor = lerp(bodyColor, absorbedColor, _Realism);

                half nebulaBlend;
                half nebulaMask = InternalNebula(input.positionOS, nebulaBlend);
                half3 nebulaColor = lerp(_NebulaColorA.rgb, _NebulaColorB.rgb, nebulaBlend);
                bodyColor += nebulaColor * nebulaMask * _NebulaStrength * centerThickness * lerp(0.25h, 0.68h, _Realism);

                half microSurface = MicroSurface(input.positionOS);
                half roughness = saturate(_Roughness + (microSurface - 0.5h) * _MicroSurfaceStrength);

                half nDotL = saturate(dot(normalWS, lightDirectionWS));
                half mainAttenuation = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half toonRamp = smoothstep(
                    _ShadowThreshold - _ShadowSoftness,
                    _ShadowThreshold + _ShadowSoftness,
                    nDotL);
                toonRamp *= mainAttenuation;
                half realisticRamp = nDotL * mainAttenuation;
                half directRamp = lerp(toonRamp, realisticRamp, _Realism);

                half3 shadowedColor = bodyColor * _ShadowColor.rgb;
                half3 litColor = bodyColor * max(mainLight.color, half3(0.22h, 0.22h, 0.22h));
                half3 color = lerp(shadowedColor, litColor, directRamp);

                half fresnel = pow(1.0h - nDotV, _FresnelPower);
                half interiorMask = saturate((1.0h - fresnel) * _InnerStrength);
                interiorMask *= lerp(0.56h, 1.0h, 1.0h - heightGradient);
                color = lerp(color, _InnerColor.rgb, interiorMask * lerp(0.35h, 0.16h, _Realism));

                half backNdotL = saturate(dot(-normalWS, lightDirectionWS));
                half forwardScatter = pow(saturate(dot(-lightDirectionWS, viewDirectionWS)), 2.3h);
                half transmission = (backNdotL * 0.72h + forwardScatter * 0.38h);
                transmission *= mainAttenuation * _ThicknessStrength * _TransmissionStrength;
                color += _InnerColor.rgb * mainLight.color * transmission;

                half3 halfDirection = SafeNormalize(lightDirectionWS + viewDirectionWS);
                half nDotH = saturate(dot(normalWS, halfDirection));
                half toonHighlight = smoothstep(
                    _HighlightThreshold,
                    min(1.0h, _HighlightThreshold + _HighlightSoftness),
                    nDotH);
                toonHighlight *= toonRamp * _HighlightStrength;
                half specularPower = lerp(176.0h, 13.0h, roughness);
                half realisticHighlight = pow(nDotH, specularPower) * (1.0h - roughness) * mainAttenuation;
                half coatPower = lerp(72.0h, 7.0h, _CoatRoughness);
                half coatHighlight = pow(nDotH, coatPower) * _CoatStrength * mainAttenuation;
                half highlight = lerp(toonHighlight, realisticHighlight * _HighlightStrength + coatHighlight, _Realism);
                color += _HighlightColor.rgb * mainLight.color * highlight;

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.positionCS = input.positionCS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                    uint additionalLightCount = GetAdditionalLightsCount();

                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint lightIndex = 0u; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
                    {
                        Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                        AccumulateJellyLight(
                            additionalLight,
                            normalWS,
                            viewDirectionWS,
                            bodyColor,
                            specularPower,
                            coatPower,
                            color);
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(additionalLightCount)
                        Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                        AccumulateJellyLight(
                            additionalLight,
                            normalWS,
                            viewDirectionWS,
                            bodyColor,
                            specularPower,
                            coatPower,
                            color);
                    LIGHT_LOOP_END
                #endif

                color += _FresnelColor.rgb * fresnel * _FresnelStrength * lerp(0.42h, 0.72h, _Realism);
                color += _StarColor.rgb * InternalStarMask(input.positionOS) * _StarStrength *
                    (0.34h + 0.66h * max(interiorMask, centerThickness * 0.45h));

                half3 ambient = SampleSH(normalWS) * bodyColor * lerp(0.18h, 0.34h, _Realism);
                color += ambient;
                color = lerp(color, color * 0.035h, saturate(_VfxDarken));
                color = MixFog(color, input.fogFactor);
                return half4(max(color, 0.0h), 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TopColor;
                half4 _BottomColor;
                half4 _ShadowColor;
                half4 _InnerColor;
                half4 _FresnelColor;
                half4 _HighlightColor;
                half4 _AbsorptionColor;
                half4 _NebulaColorA;
                half4 _NebulaColorB;
                half4 _StarColor;
                half _GradientOffset;
                half _GradientScale;
                half _ShadowThreshold;
                half _ShadowSoftness;
                half _InnerStrength;
                half _ThicknessStrength;
                half _FresnelPower;
                half _FresnelStrength;
                half _HighlightThreshold;
                half _HighlightSoftness;
                half _HighlightStrength;
                half _Realism;
                half _Roughness;
                half _CoatStrength;
                half _CoatRoughness;
                half _MicroSurfaceScale;
                half _MicroSurfaceStrength;
                half _AbsorptionStrength;
                half _TransmissionStrength;
                half _NebulaScale;
                half _NebulaStrength;
                half _NebulaSpeed;
                half _StarDensity;
                half _StarScale;
                half _StarStrength;
                half _StarSpeed;
                half _Opacity;
                half _VfxDarken;
                half _VfxDissolve;
            CBUFFER_END

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float ShadowNoise(float2 pixelPosition)
            {
                return frac(52.9829189 * frac(dot(floor(pixelPosition), float2(0.06711056, 0.00583715))));
            }

            ShadowVaryings ShadowVertex(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                return output;
            }

            half4 ShadowFragment(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half effectiveOpacity = saturate(_Opacity * (1.0h - _VfxDissolve));
                clip(effectiveOpacity - ShadowNoise(input.positionCS.xy));
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TopColor;
                half4 _BottomColor;
                half4 _ShadowColor;
                half4 _InnerColor;
                half4 _FresnelColor;
                half4 _HighlightColor;
                half4 _AbsorptionColor;
                half4 _NebulaColorA;
                half4 _NebulaColorB;
                half4 _StarColor;
                half _GradientOffset;
                half _GradientScale;
                half _ShadowThreshold;
                half _ShadowSoftness;
                half _InnerStrength;
                half _ThicknessStrength;
                half _FresnelPower;
                half _FresnelStrength;
                half _HighlightThreshold;
                half _HighlightSoftness;
                half _HighlightStrength;
                half _Realism;
                half _Roughness;
                half _CoatStrength;
                half _CoatRoughness;
                half _MicroSurfaceScale;
                half _MicroSurfaceStrength;
                half _AbsorptionStrength;
                half _TransmissionStrength;
                half _NebulaScale;
                half _NebulaStrength;
                half _NebulaSpeed;
                half _StarDensity;
                half _StarScale;
                half _StarStrength;
                half _StarSpeed;
                half _Opacity;
                half _VfxDarken;
                half _VfxDissolve;
            CBUFFER_END

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVertex(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFragment(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float noise = frac(52.9829189 * frac(dot(floor(input.positionCS.xy), float2(0.06711056, 0.00583715))));
                half effectiveOpacity = saturate(_Opacity * (1.0h - _VfxDissolve));
                clip(effectiveOpacity - noise);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
