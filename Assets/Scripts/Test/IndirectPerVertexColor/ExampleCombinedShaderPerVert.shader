// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Instanced/ExampleCombinedShaderPerVert" {
    Properties{
        _MainTex("Albedo (RGB)", 2D) = "white" {}
    }
        SubShader{

            Pass {

                Tags {}

                CGPROGRAM

                #pragma vertex vert addshadow
                #pragma fragment frag addshadow
                //#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
                #pragma target 4.5

                #include "UnityCG.cginc"
                #include "UnityLightingCommon.cginc"
                #include "AutoLight.cginc"

                sampler2D _MainTex;

            #if SHADER_TARGET >= 45
                StructuredBuffer<float4> positionBuffer;
            #endif

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv_MainTex : TEXCOORD0;
                    float3 ambient : TEXCOORD1;
                    float3 diffuse : TEXCOORD2;
                    float3 color : TEXCOORD3;
                };

                void rotate2D(inout float2 v, float r)
                {
                    float s, c;
                    sincos(r, s, c);
                    v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
                }

                struct MeshProperties {
                    float4x4 mat;
                };

                float3x3 To3x3(float4x4 mat)
                {
                    float3x3 result;

                    for (int x = 0; x < 3; x++)
                    {
                        for (int y = 0; y < 3; y++)
                        {
                            result[x][y] = mat[x][y];
                        }
                    }

                    return result;
                }

                StructuredBuffer<MeshProperties> _Properties;

                v2f vert(appdata_full v, uint instanceID : SV_InstanceID)
                {
                    float3 localPosition = v.vertex.xyz;
                    float3 worldPosition = localPosition;
                    //float3 worldNormal = v.normal;
                    //float3 rotation = float3(_Properties[instanceID].mat[0][3], _Properties[instanceID].mat[1][3],)
                    //float3 worldNormal = mul(_Properties[instanceID].mat, v.normal);
                    float3 worldNormal = normalize(mul((_Properties[instanceID].mat), v.normal));

                    float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                    float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
                    float3 diffuse = (ndotl * _LightColor0.rgb);
                    float3 color = v.color;

                    v2f o;
                    float4 pos = mul(_Properties[instanceID].mat, v.vertex);
                    o.pos = UnityObjectToClipPos(pos);
                    o.uv_MainTex = v.texcoord;
                    o.ambient = ambient;
                    o.diffuse = diffuse;
                    o.color = color;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
                    float3 lighting = i.diffuse + i.ambient;
                    fixed4 output = fixed4(albedo.rgb * i.color * lighting, albedo.w);
                    return output;
                }

                ENDCG
            }
    }
}