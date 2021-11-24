Shader "Custom/PerVertexColor"
{
    Properties
    {
        [HideInInspector] [NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue" = "AlphaTest"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                //#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
                #pragma target 4.5

                #include "UnityCG.cginc"
                #include "UnityLightingCommon.cginc"
                #include "AutoLight.cginc"

                sampler2D _MainTex;

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float3 ambient : TEXCOORD1;
                    float3 diffuse : TEXCOORD2;
                    //TODO: Instead of having color for vertex use texture map as color lookup when having few colors
                    float3 color : TEXCOORD3;
                    SHADOW_COORDS(4)
                };


                struct MeshProperties {
                    float4x4 mat;
                };

                v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
                {
                    float3 localPosition = v.vertex.xyz;
                    float3 worldPosition = localPosition;
                    float3 worldNormal = v.normal;

                    float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                    float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
                    float3 diffuse = (ndotl * _LightColor0.rgb);
                    float3 color = v.color;

                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.ambient = ambient;
                    o.diffuse = diffuse;
                    o.color = color;
                    TRANSFER_SHADOW(o)
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed shadow = SHADOW_ATTENUATION(i);
                    float3 lighting = i.diffuse * shadow + i.ambient;
                    fixed4 output = fixed4(i.color * lighting,1);
                    UNITY_APPLY_FOG(i.fogCoord, output);
                    return output;
                }

                ENDCG
            }
    }
}
