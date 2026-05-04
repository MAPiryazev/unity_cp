Shader "Custom/Dirt"
{
    Properties
    {
        // Основные цвета
        _DeepColor ("Deep Color", Color) = (0.05, 0.1, 0.15, 1)
        _ShallowColor ("Shallow Color", Color) = (0.3, 0.5, 0.7, 1)
        
        // Параметры пены
        _FoamColor ("Foam Color", Color) = (0.95, 0.98, 1, 1)
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.6
        _FoamIntensity ("Foam Intensity", Range(0, 5)) = 1.5
        
        // Параметры волн
        _WaveHeight ("Wave Height", Range(0, 5)) = 0.5
        _WaveFrequency ("Wave Frequency", Range(0.01, 20)) = 1.0
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 0.8
        
        // Текстуры
        _MainTex ("Main Texture", 2D) = "white" {}
        _FoamTex ("Foam Texture", 2D) = "white" {}
        
        // Параметры освещения
        _Smoothness ("Smoothness", Range(0, 1)) = 0.3
        _SpecularIntensity ("Specular Intensity", Range(0, 0.5)) = 0.1
        
        // Параметры прозрачности
        _Alpha ("Alpha", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }
        
        LOD 300
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float waveHeight : TEXCOORD3;
            };
            
            // Свойства
            float4 _DeepColor;
            float4 _ShallowColor;
            float4 _FoamColor;
            float _FoamThreshold;
            float _FoamIntensity;
            float _WaveHeight;
            float _WaveFrequency;
            float _WaveSpeed;
            float _Smoothness;
            float _SpecularIntensity;
            float _Alpha;
            
            sampler2D _MainTex;
            sampler2D _FoamTex;
            float4 _MainTex_ST;
            float4 _FoamTex_ST;
            
            // Функция волн
            float getWaveValue(float3 worldPos, float time)
            {
                float3 pos = worldPos * _WaveFrequency;
                
                float wave1 = sin(pos.x * 1.5 + time * 2.0) * cos(pos.z * 1.3 + time * 1.8);
                float wave2 = sin(pos.y * 2.2 + time * 2.5) * cos(pos.x * 1.7 + time * 1.5) * 0.6;
                float wave3 = cos(pos.z * 1.8 + time * 2.2) * sin(pos.y * 1.9 + time * 1.2) * 0.5;
                float wave4 = sin(pos.x * 2.0 + pos.y * 1.5 + pos.z * 1.8 + time * 3.0) * 0.4;
                
                return (wave1 + wave2 + wave3 + wave4) * 0.4;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                
                float time = _Time.y * _WaveSpeed;
                
                // Мировые координаты
                float4 worldPos = mul(UNITY_MATRIX_M, v.vertex);
                float3 worldNormal = normalize(mul((float3x3)UNITY_MATRIX_M, v.normal));
                
                // Смещение вершин волнами
                float waveValue = getWaveValue(worldPos.xyz, time);
                float3 waveOffset = worldNormal * waveValue * _WaveHeight;
                worldPos.xyz += waveOffset;
                
                // Пересчет нормали после смещения
                float eps = 0.01;
                float3 worldPosX = worldPos.xyz + float3(eps, 0, 0);
                float3 worldPosZ = worldPos.xyz + float3(0, 0, eps);
                float h = waveValue * _WaveHeight;
                float hx = getWaveValue(worldPosX, time) * _WaveHeight;
                float hz = getWaveValue(worldPosZ, time) * _WaveHeight;
                
                float3 tangent = normalize(float3(1, (hx - h) / eps, 0));
                float3 bitangent = normalize(float3(0, (hz - h) / eps, 1));
                worldNormal = normalize(cross(tangent, bitangent));
                
                float waveHeight = waveValue * 0.5 + 0.5;
                
                o.vertex = TransformWorldToHClip(worldPos);
                o.worldPos = worldPos.xyz;
                o.worldNormal = worldNormal;
                o.uv = v.uv;
                o.waveHeight = waveHeight;
                
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                float heightFactor = i.waveHeight;
                
                // Шум для вариативности
                float2 noiseUV = i.worldPos.xz * 0.2;
                float noise = tex2D(_MainTex, noiseUV).r;
                
                heightFactor = heightFactor * 0.9 + noise * 0.1;
                
                // Цвет воды на основе высоты
                float shallowStart = 0.4;
                float shallowEnd = 0.7;
                float shallowFactor = smoothstep(shallowStart, shallowEnd, heightFactor);
                
                float4 waterColor = lerp(_DeepColor, _ShallowColor, shallowFactor);
                
                // Затемнение в низинах
                float darkening = lerp(0.7, 1.0, heightFactor);
                waterColor.rgb *= darkening;
                
                // Пена
                float foamFactor = 0;
                
                if (heightFactor > _FoamThreshold)
                {
                    foamFactor = (heightFactor - _FoamThreshold) / (1.0 - _FoamThreshold);
                    foamFactor = pow(foamFactor * _FoamIntensity, 1.5);
                    
                    float2 foamUV = i.worldPos.xz * 0.8 + _Time.x * 0.5;
                    float foamDetail = tex2D(_FoamTex, foamUV).r;
                    float foamNoise = tex2D(_MainTex, i.worldPos.xz * 0.5 + _Time.x * 0.3).g;
                    
                    foamFactor = foamFactor * (0.5 + foamDetail * 0.5) * (0.7 + foamNoise * 0.3);
                    foamFactor = saturate(foamFactor);
                }
                
                // === РАСЧЕТ ОСВЕЩЕНИЯ (С ПРИГЛУШЕННЫМИ БЛИКАМИ) ===
                
                // Получаем данные основного источника света
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;
                
                // Расчет теней
                float shadowAtten = 1.0;
                #if defined(_MAIN_LIGHT_SHADOWS)
                    shadowAtten = MainLightRealtimeShadow(TransformWorldToShadowCoord(i.worldPos));
                #endif
                
                // Нормализуем нормаль
                float3 normal = normalize(i.worldNormal);
                
                // Диффузное освещение (основное)
                float NdotL = saturate(dot(normal, lightDir));
                float3 diffuse = waterColor.rgb * lightColor * NdotL * shadowAtten;
                
                // Ambient освещение (приглушенное)
                float3 ambient = SampleSH(normal) * waterColor.rgb * 0.7;
                
                // Спекулярное освещение (сильно приглушенное)
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfVec = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfVec));
                float specularPower = 2.0; // Фиксированное низкое значение
                float specular = pow(NdotH, specularPower) * _SpecularIntensity * 0.3;
                float3 specularColor = lightColor * specular * shadowAtten;
                
                // Смешиваем диффузный, спекулярный и ambient свет
                float3 litColor = diffuse + specularColor + ambient;
                
                // Добавляем дополнительное освещение (приглушенное)
                #ifdef _ADDITIONAL_LIGHTS
                    int additionalLightsCount = GetAdditionalLightsCount();
                    for (int idx = 0; idx < additionalLightsCount; idx++)
                    {
                        Light additionalLight = GetAdditionalLight(idx, i.worldPos);
                        float3 additionalLightDir = additionalLight.direction;
                        float NdotLAdd = saturate(dot(normal, additionalLightDir));
                        float3 additionalDiffuse = waterColor.rgb * additionalLight.color * NdotLAdd * additionalLight.distanceAttenuation * 0.5;
                        litColor += additionalDiffuse;
                    }
                #endif
                
                // Финальный цвет с пеной
                half4 finalColor;
                finalColor.rgb = lerp(litColor, _FoamColor.rgb, foamFactor);
                finalColor.a = lerp(waterColor.a * _Alpha, _FoamColor.a, foamFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.5
            
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            
            float _WaveHeight;
            float _WaveFrequency;
            float _WaveSpeed;
            
            float getWaveValue(float3 worldPos, float time)
            {
                float3 pos = worldPos * _WaveFrequency;
                float wave1 = sin(pos.x * 1.5 + time * 2.0) * cos(pos.z * 1.3 + time * 1.8);
                float wave2 = sin(pos.y * 2.2 + time * 2.5) * cos(pos.x * 1.7 + time * 1.5) * 0.6;
                return (wave1 + wave2) * 0.5;
            }
            
            v2f vertShadow(appdata v)
            {
                v2f o;
                float time = _Time.y * _WaveSpeed;
                float4 worldPos = mul(UNITY_MATRIX_M, v.vertex);
                float3 worldNormal = normalize(mul((float3x3)UNITY_MATRIX_M, v.normal));
                
                float waveValue = getWaveValue(worldPos.xyz, time);
                float3 waveOffset = worldNormal * waveValue * _WaveHeight;
                worldPos.xyz += waveOffset;
                
                o.vertex = TransformWorldToHClip(ApplyShadowBias(worldPos.xyz, worldNormal, _MainLightPosition.xyz));
                return o;
            }
            
            half4 fragShadow(v2f i) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}