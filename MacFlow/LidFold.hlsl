sampler2D screen : register(s0);
float progress : register(c0);
float2 pixel : register(c1);
float4 main(float2 uv : TEXCOORD) : COLOR
{
    if(progress<0.001) return tex2D(screen,uv);
    float angle=saturate(progress)*1.44;
    float d=2.8;
    float fromBottom=1.0-uv.y;
    float denom=d*cos(angle)-fromBottom*sin(angle);
    float v=d*fromBottom/max(denom,0.001);
    float scale=d/(d+v*sin(angle));
    float2 src=float2(0.5+(uv.x-0.5)/scale,1.0-v);
    if(denom<=0.0 || src.x<0.0 || src.x>1.0 || src.y<0.0 || src.y>1.0) return float4(0,0,0,1);
    // A single desktop texture, projected around its bottom hinge. Variable
    // frost increases towards the far edge; no capture inside the frame loop.
    float2 r=pixel*(1.0+progress*progress*v*v*24.0);
    float3 c=tex2D(screen,src).rgb*0.24;
    c+=(tex2D(screen,src+float2(r.x,0)).rgb+tex2D(screen,src-float2(r.x,0)).rgb)*0.13;
    c+=(tex2D(screen,src+float2(0,r.y)).rgb+tex2D(screen,src-float2(0,r.y)).rgb)*0.13;
    c+=(tex2D(screen,src+r).rgb+tex2D(screen,src-r).rgb+tex2D(screen,src+float2(r.x,-r.y)).rgb+tex2D(screen,src+float2(-r.x,r.y)).rgb)*0.06;
    return float4(c*(1.0-progress*v*0.22),1);
}
