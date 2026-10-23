// Transparent clock: local wallpaper, shallow convex glyphs, directional rim.
sampler2D mask : register(s0);
sampler2D backdrop : register(s1);
float2 offset : register(c1);
float2 extent : register(c2);
float2 size : register(c4);
float4 tint : register(c6);
float4 main(float2 uv : TEXCOORD) : COLOR
{
    float a=tex2D(mask,uv).a;
    float2 px=1/max(size,float2(1,1));
    float l=tex2D(mask,uv-float2(px.x*3,0)).a;
    float r=tex2D(mask,uv+float2(px.x*3,0)).a;
    float t=tex2D(mask,uv-float2(0,px.y*3)).a;
    float b=tex2D(mask,uv+float2(0,px.y*3)).a;
    float2 g=float2(l-r,t-b);float2 n=g/max(.001,length(g));
    float rim=saturate(1-min(min(l,r),min(t,b)));
    float light=pow(saturate(dot(n,normalize(float2(-.4,-.9)))),2);
    float opposite=pow(saturate(dot(n,normalize(float2(.4,.9)))),2);
    float2 src=saturate(offset+uv*extent-n*rim*extent*px*5);
    float3 photo=tex2D(backdrop,src).rgb;
    float alpha=(.52+rim*.08*light)*a;
    float3 body=lerp(photo,tint.rgb,.74);
    body=lerp(body,tint.rgb,rim*light*.20);
    body=lerp(body,float3(.08,.10,.13),rim*opposite*.10);
    return float4(saturate(body)*alpha,alpha);
}
