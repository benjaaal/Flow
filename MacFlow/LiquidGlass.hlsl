sampler2D backdrop : register(s0);
float2 size : register(c0);
float radius : register(c1);
float4 tint : register(c2);
float strength : register(c3);
float readability : register(c4);
float2 pointer : register(c5);
float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 extent=max(size,1.0);
    float2 p=(uv-0.5)*extent;
    float r=min(radius,min(extent.x,extent.y)*0.5);
    float2 q=abs(p)-(extent*0.5-r);
    float2 corner=max(q,0.0);
    float distance=length(corner)+min(max(q.x,q.y),0.0)-r;
    float2 n;
    if(length(corner)>0.001) n=normalize(corner)*sign(p);
    else n=(q.x>q.y?float2(sign(p.x),0):float2(0,sign(p.y)));
    // Convex lens displaces sampled backdrop pixels inside a narrow rim.
    float rim=saturate(1.0+distance/17.0);
    float2 offset=n*(sin(rim*1.570796)*strength)/extent;
    float2 sampleUV=clamp(uv-offset,0.001,0.999);
    float4 color=tex2D(backdrop,sampleUV);
    color.r=tex2D(backdrop,clamp(sampleUV-n*0.32*rim/extent,0.001,0.999)).r;
    color.b=tex2D(backdrop,clamp(sampleUV+n*0.32*rim/extent,0.001,0.999)).b;
    color.rgb=lerp(color.rgb,tint.rgb,tint.a);
    float luminance=dot(color.rgb,float3(0.2126,0.7152,0.0722));
    // Preserve backdrop detail while enforcing a luminance range for text.
    // Foreground labels/icons never pass through this shader.
    if(readability>0.0) color.rgb=lerp(color.rgb,1.0,saturate((readability-luminance)/max(1.0-luminance,0.001)));
    else if(readability<0.0) color.rgb*=min(1.0,-readability/max(luminance,0.001));
    float2 lightDirection=normalize(float2(-0.5,-0.86)+(pointer-0.5)*0.35);
    float light=pow(saturate(dot(n,lightDirection)),5.0);
    float hairline=pow(saturate(1.0+distance/1.4),3.0);
    color.rgb+=hairline*(0.025+0.16*light);
    color.rgb+=rim*rim*light*0.023;
    return float4(saturate(color.rgb),1.0);
}
