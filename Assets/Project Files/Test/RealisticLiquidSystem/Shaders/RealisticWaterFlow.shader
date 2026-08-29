Shader "RealisticLiquidSystem/MeshFluidSimulation"
{
    Properties
    {
        // =========================================================
        // FLUID TYPE
        // =========================================================

        [Enum(Water,0,Oil,1,Chemical,2)]
        _FluidType ("Fluid Type", Float) = 0


        // =========================================================
        // COLORS
        // =========================================================

        _BaseColor ("Fluid Color", Color) =
            (0.02, 0.25, 0.75, 0.92)

        _DeepColor ("Deep Color", Color) =
            (0.005, 0.025, 0.12, 1)

        _SurfaceColor ("Surface Color", Color) =
            (0.15, 0.55, 1.0, 1)

        _Alpha ("Opacity", Range(0.1,1)) = 0.92

        // Water is naturally more transparent than oil/chemical
        _WaterOpacity ("Water Opacity", Range(0.15,0.8)) = 0.45


        // =========================================================
        // FILL
        // =========================================================

        _FillLevel ("Fill Level", Range(0,1)) = 0.7

        _FillSoftness ("Fill Softness", Range(0.001,0.2)) = 0.025

        _ContainerHeight ("Container Height", Float) = 1.0


        // =========================================================
        // WAVE
        // =========================================================

        _WaveDirection ("Wave Direction", Vector) =
            (1,0,0,0)

        _WaveHeight ("Wave Height", Range(0,0.25)) = 0.035

        _WaveLength ("Wave Length", Range(0.1,5)) = 1.2

        _WaveSpeed ("Wave Speed", Range(0,5)) = 1.0

        _WaveFrequency ("Wave Frequency", Range(0.1,10)) = 2.0

        _SecondaryWave ("Secondary Wave", Range(0,0.2)) = 0.015


        // =========================================================
        // WOBBLE
        // =========================================================

        _WobbleAmount ("Wobble Amount", Range(0,0.2)) = 0.035

        _WobbleSpeed ("Wobble Speed", Range(0,8)) = 2.0

        _WobbleFrequency ("Wobble Frequency", Range(0.1,10)) = 2.5


        // =========================================================
        // FLUID THICKNESS
        // =========================================================

        _Thickness ("Fluid Thickness", Range(0,2)) = 1.0

        _Viscosity ("Viscosity", Range(0,1)) = 0.2

        _RefractionStrength ("Refraction Strength", Range(0,1)) = 0.15


        // =========================================================
        // REFLECTION / SPECULAR
        // =========================================================

        _Smoothness ("Smoothness", Range(0,1)) = 0.95

        _SpecularStrength ("Specular Strength", Range(0,5)) = 2.5

        _FresnelPower ("Fresnel Power", Range(0.5,8)) = 3.5

        _FresnelStrength ("Fresnel Strength", Range(0,3)) = 1.5


        // =========================================================
        // FLOW
        // =========================================================

        _FlowDirection ("Flow Direction", Vector) =
            (1,0,0,0)

        _FlowSpeed ("Flow Speed", Range(0,5)) = 0.8

        _FlowScale ("Flow Scale", Range(0.2,15)) = 3.0


        // =========================================================
        // BUBBLES
        // =========================================================

        _BubbleAmount ("Bubble Amount", Range(0,1)) = 0.35

        _BubbleSize ("Bubble Size", Range(0.005,0.15)) = 0.035

        _BubbleSpeed ("Bubble Speed", Range(0,2)) = 0.35

        _BubbleBrightness ("Bubble Brightness", Range(0,3)) = 1.0


        // =========================================================
        // CHEMICAL DOTS
        // =========================================================

        _ChemicalDotAmount ("Chemical Dot Amount", Range(0,1)) = 0.5

        _ChemicalDotSize ("Chemical Dot Size", Range(0.002,0.1)) = 0.025

        _ChemicalDotSpeed ("Chemical Dot Speed", Range(0,3)) = 0.5


        // =========================================================
        // SURFACE FOAM
        // =========================================================

        _FoamAmount ("Surface Foam", Range(0,1)) = 0.2

        _FoamScale ("Foam Scale", Range(1,20)) = 8

        _FoamSpeed ("Foam Speed", Range(0,3)) = 0.6
    }


    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        LOD 400

        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off

        Cull Back


        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"


            // =====================================================
            // STRUCTURES
            // =====================================================

            struct appdata
            {
                float4 vertex : POSITION;

                float3 normal : NORMAL;

                float2 uv : TEXCOORD0;
            };


            struct v2f
            {
                float4 pos : SV_POSITION;

                float3 worldPos : TEXCOORD0;

                float3 normal : TEXCOORD1;

                float3 viewDir : TEXCOORD2;

                float2 uv : TEXCOORD3;
            };


            // =====================================================
            // VARIABLES
            // =====================================================

            float _FluidType;


            fixed4 _BaseColor;

            fixed4 _DeepColor;

            fixed4 _SurfaceColor;

            float _Alpha;

            float _WaterOpacity;


            float _FillLevel;

            float _FillSoftness;

            float _ContainerHeight;


            float4 _WaveDirection;

            float _WaveHeight;

            float _WaveLength;

            float _WaveSpeed;

            float _WaveFrequency;

            float _SecondaryWave;


            float _WobbleAmount;

            float _WobbleSpeed;

            float _WobbleFrequency;


            float _Thickness;

            float _Viscosity;

            float _RefractionStrength;


            float _Smoothness;

            float _SpecularStrength;

            float _FresnelPower;

            float _FresnelStrength;


            float4 _FlowDirection;

            float _FlowSpeed;

            float _FlowScale;


            float _BubbleAmount;

            float _BubbleSize;

            float _BubbleSpeed;

            float _BubbleBrightness;


            float _ChemicalDotAmount;

            float _ChemicalDotSize;

            float _ChemicalDotSpeed;


            float _FoamAmount;

            float _FoamScale;

            float _FoamSpeed;


            // =====================================================
            // HASH
            // =====================================================

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));

                p += dot(p, p + 45.32);

                return frac(p.x * p.y);
            }


            // =====================================================
            // NOISE
            // =====================================================

            float noise(float2 p)
            {
                float2 i = floor(p);

                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);


                float a = hash21(i);

                float b = hash21(i + float2(1,0));

                float c = hash21(i + float2(0,1));

                float d = hash21(i + float2(1,1));


                return lerp(
                    lerp(a,b,f.x),
                    lerp(c,d,f.x),
                    f.y
                );
            }


            // =====================================================
            // WAVE
            // =====================================================

            float CalculateWave(float3 worldPos)
            {
                float2 direction =
                    normalize(
                        _WaveDirection.xy +
                        float2(0.0001,0.0001)
                    );


                float t =
                    _Time.y *
                    _WaveSpeed;


                float2 position =
                    worldPos.xz;


                float primary =
                    sin(
                        dot(position,direction)
                        *
                        (6.28318 / max(_WaveLength,0.01))
                        *
                        _WaveFrequency
                        +
                        t
                    );


                float2 secondaryDirection =
                    float2(
                        -direction.y,
                        direction.x
                    );


                float secondary =
                    sin(
                        dot(
                            position,
                            secondaryDirection
                        )
                        *
                        3.5
                        -
                        t * 0.7
                    );


                float noiseWave =
                    noise(
                        position *
                        2.5
                        +
                        t * 0.15
                    );


                return
                    primary * 0.65 +
                    secondary * 0.2 +
                    (noiseWave * 2.0 - 1.0) * 0.15;
            }


            // =====================================================
            // WOBBLE
            // =====================================================

            float CalculateWobble(float3 worldPos)
            {
                float t =
                    _Time.y *
                    _WobbleSpeed;


                float w1 =
                    sin(
                        worldPos.x *
                        _WobbleFrequency
                        +
                        t
                    );


                float w2 =
                    sin(
                        worldPos.z *
                        (_WobbleFrequency * 1.37)
                        -
                        t * 0.8
                    );


                return
                    (w1 + w2) *
                    0.5;
            }


            // =====================================================
            // VERTEX
            // =====================================================

            v2f vert(appdata v)
            {
                v2f o;


                float3 localPosition =
                    v.vertex.xyz;


                float3 worldPosition =
                    mul(
                        unity_ObjectToWorld,
                        v.vertex
                    ).xyz;


                float3 worldNormal =
                    normalize(
                        UnityObjectToWorldNormal(
                            v.normal
                        )
                    );


                // -------------------------------------------------
                // WAVE
                // -------------------------------------------------

                float wave =
                    CalculateWave(
                        worldPosition
                    );


                // -------------------------------------------------
                // WOBBLE
                // -------------------------------------------------

                float wobble =
                    CalculateWobble(
                        worldPosition
                    );


                // -------------------------------------------------
                // APPLY SURFACE MOVEMENT
                // -------------------------------------------------

                float surfaceFactor =
                    smoothstep(
                        0.0,
                        1.0,
                        saturate(
                            _FillLevel
                        )
                    );


                float displacement =
                    wave *
                    _WaveHeight *
                    surfaceFactor;


                displacement +=
                    wobble *
                    _WobbleAmount *
                    surfaceFactor;


                // -------------------------------------------------
                // MESH DEFORMATION
                // -------------------------------------------------

                worldPosition +=
                    worldNormal *
                    displacement;


                // -------------------------------------------------
                // OUTPUT
                // -------------------------------------------------

                o.pos =
                    UnityWorldToClipPos(
                        worldPosition
                    );


                o.worldPos =
                    worldPosition;


                o.normal =
                    worldNormal;


                o.viewDir =
                    normalize(
                        _WorldSpaceCameraPos -
                        worldPosition
                    );


                o.uv =
                    v.uv;


                return o;
            }


            // =====================================================
            // BUBBLE FUNCTION
            // =====================================================

            float BubblePattern(
                float2 uv,
                float time
            )
            {
                float2 grid =
                    uv * 12.0;


                float2 cell =
                    floor(grid);


                float2 local =
                    frac(grid) - 0.5;


                float random =
                    hash21(cell);


                float2 offset =
                    float2(
                        hash21(cell + 12.4),
                        hash21(cell + 45.7)
                    )
                    - 0.5;


                float rise =
                    frac(
                        time *
                        _BubbleSpeed
                        +
                        random
                    );


                offset.y =
                    lerp(
                        -0.45,
                        0.45,
                        rise
                    );


                float distanceToBubble =
                    length(
                        local -
                        offset *
                        0.35
                    );


                float bubble =
                    1.0 -
                    smoothstep(
                        _BubbleSize,
                        _BubbleSize * 1.8,
                        distanceToBubble
                    );


                bubble *=
                    step(
                        random,
                        _BubbleAmount
                    );


                return bubble;
            }


            // =====================================================
            // CHEMICAL DOT FUNCTION
            // =====================================================

            float ChemicalDots(
                float2 uv,
                float time
            )
            {
                float2 grid =
                    uv * 20.0;


                float2 cell =
                    floor(grid);


                float2 local =
                    frac(grid) - 0.5;


                float random =
                    hash21(cell);


                float2 movement =
                    float2(
                        sin(time * _ChemicalDotSpeed + random * 6.28),
                        cos(time * _ChemicalDotSpeed * 0.7 + random * 5.0)
                    )
                    * 0.15;


                float distanceToDot =
                    length(
                        local -
                        movement
                    );


                float dot =
                    1.0 -
                    smoothstep(
                        _ChemicalDotSize,
                        _ChemicalDotSize * 1.8,
                        distanceToDot
                    );


                dot *=
                    step(
                        random,
                        _ChemicalDotAmount
                    );


                return dot;
            }


            // =====================================================
            // FLOW
            // =====================================================

            float CalculateFlow(
                float3 worldPos
            )
            {
                float2 direction =
                    normalize(
                        _FlowDirection.xy +
                        float2(
                            0.0001,
                            0.0001
                        )
                    );


                float2 flowUV =
                    worldPos.xz;


                float time =
                    _Time.y *
                    _FlowSpeed;


                float2 offset =
                    direction *
                    time;


                float n1 =
                    noise(
                        flowUV *
                        _FlowScale
                        +
                        offset
                    );


                float n2 =
                    noise(
                        flowUV *
                        (_FlowScale * 1.7)
                        -
                        offset * 0.6
                    );


                return
                    n1 * 0.65 +
                    n2 * 0.35;
            }


            // =====================================================
            // FRAGMENT
            // =====================================================

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal =
                    normalize(i.normal);


                float3 viewDirection =
                    normalize(i.viewDir);


                // =================================================
                // FILL
                // =================================================

                float fillHeight =
                    _FillLevel;


                float normalizedHeight =
                    saturate(
                        i.worldPos.y /
                        max(
                            _ContainerHeight,
                            0.001
                        )
                    );


                float fillMask =
                    smoothstep(
                        fillHeight -
                        _FillSoftness,

                        fillHeight +
                        _FillSoftness,

                        normalizedHeight
                    );


                // =================================================
                // FLOW
                // =================================================

                float flow =
                    CalculateFlow(
                        i.worldPos
                    );


                // =================================================
                // DEPTH COLOR
                // =================================================

                float depthFactor =
                    saturate(
                        normalizedHeight
                    );


                float3 fluidColor =
                    lerp(
                        _DeepColor.rgb,
                        _BaseColor.rgb,
                        depthFactor
                    );


                fluidColor =
                    lerp(
                        fluidColor,
                        _SurfaceColor.rgb,
                        flow * 0.18
                    );


                // =================================================
                // FRESNEL
                // =================================================

                float fresnel =
                    pow(
                        1.0 -
                        saturate(
                            dot(
                                normal,
                                viewDirection
                            )
                        ),
                        _FresnelPower
                    );


                fresnel *=
                    _FresnelStrength;


                // =================================================
                // SPECULAR
                // =================================================

                float3 lightDirection =
                    normalize(
                        _WorldSpaceLightPos0.xyz
                    );


                float3 halfDirection =
                    normalize(
                        lightDirection +
                        viewDirection
                    );


                float specular =
                    pow(
                        saturate(
                            dot(
                                normal,
                                halfDirection
                            )
                        ),
                        lerp(
                            32.0,
                            180.0,
                            _Smoothness
                        )
                    );


                specular *=
                    _SpecularStrength;


                // =================================================
                // SURFACE FOAM
                // =================================================

                float foam =
                    noise(
                        i.worldPos.xz *
                        _FoamScale
                        +
                        _Time.y *
                        _FoamSpeed
                    );


                foam =
                    smoothstep(
                        0.62,
                        0.85,
                        foam
                    );


                foam *=
                    _FoamAmount;


                // =================================================
                // BUBBLES
                // =================================================

                float bubbles =
                    BubblePattern(
                        i.worldPos.xz,
                        _Time.y
                    );


                // =================================================
                // CHEMICAL DOTS
                // =================================================

                float chemicalDots =
                    ChemicalDots(
                        i.worldPos.xz,
                        _Time.y
                    );


                // =================================================
                // FLUID TYPE
                // =================================================

                float waterMask =
                    1.0 -
                    step(
                        0.5,
                        _FluidType
                    );


                float oilMask =
                    step(
                        0.5,
                        _FluidType
                    )
                    *
                    (1.0 -
                    step(
                        1.5,
                        _FluidType
                    ));


                float chemicalMask =
                    step(
                        1.5,
                        _FluidType
                    );


                // =================================================
                // WATER
                // =================================================

                float waterBubbles =
                    bubbles *
                    waterMask *
                    1.2;


                // =================================================
                // OIL
                // =================================================

                float oilBubbles =
                    bubbles *
                    oilMask *
                    0.45;


                // =================================================
                // CHEMICAL
                // =================================================

                float chemical =
                    chemicalDots *
                    chemicalMask;


                // =================================================
                // FLUID APPEARANCE
                // =================================================

                float3 color =
                    fluidColor;


                // Water bubbles
                color +=
                    waterBubbles *
                    _BubbleBrightness *
                    float3(
                        0.35,
                        0.75,
                        1.0
                    );


                // Oil bubbles
                color +=
                    oilBubbles *
                    _BubbleBrightness *
                    float3(
                        1.0,
                        0.55,
                        0.08
                    );


                // Chemical dots
                color +=
                    chemical *
                    float3(
                        0.2,
                        1.0,
                        0.45
                    );


                // =================================================
                // OIL GLOSS
                // =================================================

                float oilGloss =
                    oilMask *
                    flow *
                    0.35;


                color +=
                    oilGloss *
                    float3(
                        0.8,
                        0.45,
                        0.08
                    );


                // =================================================
                // LIGHTING
                // =================================================

                color +=
                    specular *
                    0.35;


                color +=
                    fresnel *
                    0.18;


                color +=
                    foam *
                    0.15;


                // =================================================
                // THICKNESS
                // =================================================

                float thickness =
                    lerp(
                        0.55,
                        1.0,
                        _Thickness
                    );


                float alpha =
                    _Alpha *
                    thickness;

                // Water should look light/transparent instead of
                // dense and jelly-like. Oil and chemical keep the
                // original opacity behavior.
                alpha *= lerp(
                    1.0,
                    _WaterOpacity,
                    waterMask
                );


                // =================================================
                // SURFACE EDGE
                // =================================================

                float surfaceEdge =
                    1.0 -
                    abs(
                        normalizedHeight -
                        fillHeight
                    );


                surfaceEdge =
                    smoothstep(
                        0.0,
                        0.08,
                        surfaceEdge
                    );


                color +=
                    surfaceEdge *
                    _SurfaceColor.rgb *
                    0.12;


                // =================================================
                // FINAL
                // =================================================

                return fixed4(
                    saturate(color),
                    saturate(alpha)
                );
            }

            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}