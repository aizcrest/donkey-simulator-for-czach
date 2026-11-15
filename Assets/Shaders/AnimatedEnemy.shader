Shader "Forest Rider/Animated Enemy"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _FrameBlend ("Pose interpolation", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert noforwardadd
        #pragma target 3.0
        #pragma multi_compile_instancing
        fixed4 _Color;
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _FrameBlend)
        UNITY_INSTANCING_BUFFER_END(Props)
        struct Input { fixed4 color : COLOR; };
        void vert(inout appdata_full v)
        {
            UNITY_SETUP_INSTANCE_ID(v);
            float blend=UNITY_ACCESS_INSTANCED_PROP(Props,_FrameBlend);
            v.vertex.xyz+=v.texcoord1.xyz*blend;
            float3 normal=v.normal+v.texcoord2.xyz*blend;
            v.normal=dot(normal,normal)<.0001?v.normal:normalize(normal);
        }
        void surf(Input IN,inout SurfaceOutput o)
        {
            o.Albedo=IN.color.rgb*_Color.rgb;
            #ifndef UNITY_COLORSPACE_GAMMA
            o.Albedo=GammaToLinearSpace(IN.color.rgb)*_Color.rgb;
            #endif
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Forest Rider/Model Vertex Colors"
}
