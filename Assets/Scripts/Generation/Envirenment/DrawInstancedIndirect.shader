Shader "Custom/DrawInstancedIndirect"
{
    Properties{
       _MainTex("Albedo (RGB)", 2D) = "white" {}
    }
        SubShader{

            Pass {

                Tags { /*"LightMode" = "ShadowCaster"*/ }
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                //#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
                #pragma target 4.5

                #include "UnityCG.cginc"
                #include "UnityLightingCommon.cginc"
                #include "AutoLight.cginc"

                #define PI 3.14159265358979323846

                sampler2D _MainTex;

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv_MainTex : TEXCOORD0;
                    float3 ambient : TEXCOORD1;
                    float3 diffuse : TEXCOORD2;
                    //TODO: Instead of having color for vertex use texture map as color lookup when having few colors
                    float3 color : TEXCOORD3;
                };


                struct MeshProperties {
                    float4x4 mat;
                    float3 color;
                };

                StructuredBuffer<MeshProperties> _Properties;

                v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
                {
                    float3 localPosition = v.vertex.xyz;
                    float3 worldPosition = localPosition;
                    float3 worldNormal = normalize(mul((_Properties[instanceID].mat), v.normal));

                    float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                    float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
                    float3 diffuse = (ndotl * _LightColor0.rgb);

                    float heightTransition = v.texcoord.y;
                    //float heightTransition = 1 - cos(PI * v.texcoord.y / 2);

                    float3 color = (1- heightTransition) * v.color + heightTransition * _Properties[instanceID].color;

                    v2f o;
                    float4 pos = mul(_Properties[instanceID].mat, v.vertex);
                    o.pos = UnityObjectToClipPos(pos);
                    //o.uv_MainTex = v.texcoord;
                    o.ambient = ambient;
                    o.diffuse = diffuse;
                    o.color = color;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    //fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
                    float3 lighting = i.diffuse + i.ambient;
                    fixed4 output = fixed4(/*albedo.rgb * */i.color * lighting, 1/*albedo.w*/);
                    return output;
                }

                ENDCG
            }
    }
}
