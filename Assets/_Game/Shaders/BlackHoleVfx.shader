Shader "BubbleMind/Black Hole VFX"
{
    Properties
    {
        [Enum(EventHorizon,0,AccretionDisk,1,GravityWave,2,CollapseFlash,3,LineBreak,4)] _Mode("Effect Mode", Float) = 0
        [HDR] _CoreColor("Core Color", Color) = (0, 0, 0, 1)
        [HDR] _InnerColor("Inner Color", Color) = (0.48, 0.12, 1.4, 1)
        [HDR] _OuterColor("Outer Color", Color) = (0.12, 0.72, 1.6, 1)
        [HDR] _AccentColor("Accent Color", Color) = (1.4, 0.72, 1.8, 1)
        _Radius("Event Radius", Range(0.05, 1.2)) = 0.34
        _RingWidth("Ring Width", Range(0.005, 0.35)) = 0.075
        _DiskFlatten("Disk Flatten", Range(0.25, 8)) = 3.2
        _Twist("Spiral Arms", Range(1, 12)) = 5
        _NoiseScale("Noise Scale", Range(0.5, 16)) = 5
        _Speed("Animation Speed", Range(-8, 8)) = 1.4
        _Intensity("Intensity", Range(0, 8)) = 1
        _Progress("Sequence Progress", Range(0, 1)) = 0
        _Phase("Phase", Float) = 0
        _Opacity("Opacity", Range(0, 1)) = 1
        _Softness("Edge Softness", Range(0.001, 0.2)) = 0.035
        _Aspect("UV Aspect", Range(0.1, 8)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+40"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BlackHoleVfx"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VfxVertex
            #pragma fragment VfxFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _InnerColor;
                half4 _OuterColor;
                half4 _AccentColor;
                half _Mode;
                half _Radius;
                half _RingWidth;
                half _DiskFlatten;
                half _Twist;
                half _NoiseScale;
                half _Speed;
                half _Intensity;
                half _Progress;
                half _Phase;
                half _Opacity;
                half _Softness;
                half _Aspect;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            float ValueNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 fraction = frac(value);
                fraction = fraction * fraction * (3.0 - 2.0 * fraction);
                float bottom = lerp(Hash21(cell), Hash21(cell + float2(1, 0)), fraction.x);
                float top = lerp(Hash21(cell + float2(0, 1)), Hash21(cell + 1.0), fraction.x);
                return lerp(bottom, top, fraction.y);
            }

            half SoftBand(float value, float center, float width, float softness)
            {
                return 1.0h - smoothstep(width, width + softness, abs(value - center));
            }

            Varyings VfxVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 RenderEventHorizon(float2 samplePosition, float radius, float angle, float noiseValue)
            {
                half core = 1.0h - smoothstep(_Radius, _Radius + _Softness, radius);
                half photonRing = SoftBand(radius, _Radius + _RingWidth * 0.36h, _RingWidth, _Softness);
                half lensRing = SoftBand(radius, _Radius + _RingWidth * 2.45h, _RingWidth * 0.38h, _Softness * 1.5h);
                half unevenRim = saturate(0.7h + 0.3h * sin(angle * 7.0h - _Time.y * _Speed * 2.0h + noiseValue * 5.0h));
                photonRing *= unevenRim;

                half3 color = _CoreColor.rgb * core;
                color += _InnerColor.rgb * photonRing * _Intensity;
                color += _OuterColor.rgb * lensRing * _Intensity * 0.55h;
                half alpha = max(core * _CoreColor.a, max(photonRing, lensRing * 0.7h)) * _Opacity;
                return half4(color, saturate(alpha));
            }

            half4 RenderAccretionDisk(float2 samplePosition, float radius, float angle, float noiseValue)
            {
                float2 diskPoint = float2(samplePosition.x, samplePosition.y * _DiskFlatten);
                float diskRadius = length(diskPoint);
                float diskAngle = atan2(diskPoint.y, diskPoint.x);
                half innerCut = smoothstep(_Radius * 0.78h, _Radius + _Softness, diskRadius);
                half outerCut = 1.0h - smoothstep(0.97h, 1.0h, diskRadius);
                half diskMask = innerCut * outerCut;

                half spiral = 0.5h + 0.5h * sin(
                    diskAngle * _Twist - diskRadius * 23.0h - _Time.y * _Speed * 3.0h + _Phase + noiseValue * 4.0h);
                half fineBand = 0.5h + 0.5h * sin(diskRadius * 74.0h + _Time.y * _Speed * 1.7h - noiseValue * 5.0h);
                half hotBand = smoothstep(0.58h, 0.94h, spiral) * lerp(0.62h, 1.0h, fineBand);
                half radialFade = saturate(1.0h - diskRadius);

                half3 diskColor = lerp(_OuterColor.rgb, _InnerColor.rgb, radialFade);
                diskColor = lerp(diskColor, _AccentColor.rgb, hotBand * 0.72h);
                diskColor *= (0.52h + hotBand * 0.9h) * _Intensity;

                half core = 1.0h - smoothstep(_Radius * 0.72h, _Radius * 0.86h, radius);
                half3 color = diskColor * diskMask + _CoreColor.rgb * core;
                half alpha = max(diskMask * (0.48h + hotBand * 0.52h), core * _CoreColor.a) * _Opacity;
                return half4(color, saturate(alpha));
            }

            half4 RenderGravityWave(float radius, float angle, float noiseValue)
            {
                half progress = saturate(_Progress);
                half ringRadius = lerp(0.08h, 1.08h, progress);
                half primary = SoftBand(radius, ringRadius, _RingWidth, _Softness);
                half echo = SoftBand(radius, max(0.0h, ringRadius - 0.16h), _RingWidth * 0.42h, _Softness);
                half angularRipple = 0.72h + 0.28h * sin(angle * 10.0h + _Time.y * _Speed * 2.0h + noiseValue * 4.0h);
                half fade = 1.0h - progress * 0.78h;
                half energy = (primary * angularRipple + echo * 0.48h) * fade;
                half3 color = lerp(_InnerColor.rgb, _OuterColor.rgb, progress) * energy * _Intensity;
                return half4(color, saturate(energy * _Opacity));
            }

            half4 RenderCollapse(float radius, float angle, float noiseValue)
            {
                half progress = saturate(_Progress);
                half inverseProgress = 1.0h - progress;
                half coreRadius = lerp(0.68h, 0.025h, progress);
                half core = 1.0h - smoothstep(coreRadius, coreRadius + _Softness * 2.0h, radius);
                half blastRadius = lerp(0.05h, 1.12h, progress);
                half blastRing = SoftBand(radius, blastRadius, _RingWidth * lerp(0.35h, 1.3h, progress), _Softness);
                half rays = pow(abs(sin(angle * 9.0h + noiseValue * 3.0h)), 16.0h);
                rays *= saturate(1.0h - radius) * sin(progress * PI);
                half flash = core * (0.55h + inverseProgress * 0.45h) + blastRing + rays * 0.8h;
                half3 color = lerp(_AccentColor.rgb, _OuterColor.rgb, progress) * flash * _Intensity;
                color += _CoreColor.rgb * core;
                half alpha = saturate((core + blastRing + rays * 0.55h) * _Opacity * (1.0h - progress * 0.28h));
                return half4(color, alpha);
            }

            half4 RenderLineBreak(float2 uv, float noiseValue)
            {
                float across = abs(uv.x - 0.5) * 2.0;
                float along = uv.y;
                half endFade = smoothstep(0.0h, 0.075h, along) * smoothstep(0.0h, 0.12h, 1.0h - along);
                half core = 1.0h - smoothstep(0.06h, 0.20h, across);
                half edge = SoftBand(across, 0.28h, 0.11h, _Softness * 2.0h);
                half traveling = 0.5h + 0.5h * sin(along * 68.0h - _Time.y * _Speed * 8.0h + _Phase + noiseValue * 5.0h);
                half fracture = smoothstep(0.70h, 0.96h, traveling) * (1.0h - across);
                half energy = (core + edge * 0.62h + fracture * 0.55h) * endFade;
                half3 color = _AccentColor.rgb * core;
                color += _InnerColor.rgb * edge + _OuterColor.rgb * fracture;
                color *= _Intensity;
                return half4(color, saturate(energy * _Opacity));
            }

            half4 VfxFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 centered = (input.uv - 0.5) * 2.0;
                centered.x *= _Aspect;
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x);
                float animatedTime = _Time.y * _Speed + _Phase;
                float noiseValue = ValueNoise(centered * _NoiseScale + float2(animatedTime * 0.17, -animatedTime * 0.13));

                half4 result;
                if (_Mode < 0.5h)
                {
                    result = RenderEventHorizon(centered, radius, angle, noiseValue);
                }
                else if (_Mode < 1.5h)
                {
                    result = RenderAccretionDisk(centered, radius, angle, noiseValue);
                }
                else if (_Mode < 2.5h)
                {
                    result = RenderGravityWave(radius, angle, noiseValue);
                }
                else if (_Mode < 3.5h)
                {
                    result = RenderCollapse(radius, angle, noiseValue);
                }
                else
                {
                    result = RenderLineBreak(input.uv, noiseValue);
                }

                clip(result.a - 0.002h);
                result.rgb = MixFog(result.rgb, input.fogFactor);
                return result;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
