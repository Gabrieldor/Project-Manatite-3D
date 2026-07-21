#ifndef ADDITIONAL_LIGHT_INCLUDED
#define ADDITIONAL_LIGHT_INCLUDED


void GetLights_float(in float3 WorldPos, in float3 WorldNormal, out float3 Direction, out float3 Color)
{
    #ifdef SHADERGRAPH_PREVIEW
        Direction = normalize(float3(1.0f, 1.0f, 0.0f));
        Color = 1.0f;
    #else
        //Get Shadow Map
        #if SHADOWS_SCREEN
			float4 shadowCoord = ComputeScreenPos(ClipSpacePos);
		#else
			float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
		#endif

        //Get Main Light Color and Direction
        Light mainLight = GetMainLight();
        Direction = mainLight.direction;
        Color = mainLight.color;


        //Creates the Shadow for Main Light
        ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        float shadowStrength = GetMainLightShadowStrength();
        float ShadowAtten = SampleShadowmapFiltered(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowCoord, shadowSamplingData);

        Color *= ShadowAtten;

    #endif

    #ifndef SHADERGRAPH_PREVIEW
        //Find all Additional Lights interacting with the object and iterate over then
        int lightCount = GetAdditionalLightsCount();

        for(int i = 0; i < lightCount; ++i)
        {
            Light light = GetAdditionalLight(i, WorldPos);

            //Verify if the surface normal is in the same direction that the light and join everything
            float3 color = dot(light.direction, normalize(WorldNormal));
            color = saturate(color);
            color *= light.distanceAttenuation;
            color *= light.shadowAttenuation;
            color *= light.color;


            //Creates the shadows for adittional lights (not working properly yet)
            ShadowSamplingData shadowSamplingDataAdd = GetAdditionalLightShadowSamplingData(i);
            float shadowStrengthAdd = GetAdditionalLightShadowParams(i);
            float ShadowAttenAdd = SampleShadowmapFiltered(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowCoord, shadowSamplingDataAdd);

            Color *= ShadowAttenAdd;

            Color += color;
        } 
    #endif
}

#endif