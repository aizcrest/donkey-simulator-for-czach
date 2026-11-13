Shader "Forest Rider/Day Night Sky"
{
 Properties { _Lookup("Clouds and stars",2D)="black"{} _Top("Zenith",Color)=(.1,.4,.8,1) _Horizon("Horizon",Color)=(.6,.8,1,1) _SunDir("Sun direction",Vector)=(0,1,0,0) _Day("Daylight",Range(0,1))=1 _Clock("Cloud drift",Float)=0 }
 SubShader
 {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _Lookup;float4 _Top,_Horizon,_SunDir;float _Day,_Clock;
   struct v2f { float4 pos:SV_POSITION;float3 direction:TEXCOORD0; };
   v2f vert(float4 vertex:POSITION){v2f o;o.pos=UnityObjectToClipPos(vertex);o.direction=vertex.xyz;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float3 d=normalize(i.direction);float up=saturate(d.y);
    float2 uv=float2(atan2(d.z,d.x)*.15915494+.5,asin(clamp(d.y,-1,1))*.31830989+.5);
    float3 color=lerp(_Horizon.rgb,_Top.rgb,sqrt(up));
    float clouds=smoothstep(.48,.72,tex2D(_Lookup,uv+float2(_Clock,0)).r)*smoothstep(0,.18,d.y)*.55;
    float stars=pow(tex2D(_Lookup,uv).g,2)*(1-_Day)*smoothstep(0,.15,d.y);
    color+=stars*(1-clouds)*.9;
    float sun=smoothstep(.9983,.9994,dot(d,_SunDir.xyz));
    float moon=smoothstep(.9990,.9996,dot(d,-_SunDir.xyz));
    color+=sun*float3(1,.78,.40)*_Day+moon*float3(.6,.73,.9)*(1-_Day);
    color=lerp(color,lerp(float3(.10,.14,.23),float3(.9,.94,.92),_Day),clouds);
    return fixed4(color,1);
   }
   ENDCG
  }
 }
}
