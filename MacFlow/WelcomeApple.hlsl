// Lighting of a vector silhouette over a live compositor backdrop.
// Samples only the logo mask, not the desktop. No optical refraction claim.
sampler2D mask : register(s0);
float darkInk : register(c0);
float4 main(float2 uv : TEXCOORD) : COLOR
{
    float a=tex2D(mask,uv).a;
    float2 step=float2(1.0/180.0,1.0/180.0);
    float left=tex2D(mask,uv-float2(step.x*4,0)).a;
    float right=tex2D(mask,uv+float2(step.x*4,0)).a;
    float top=tex2D(mask,uv-float2(0,step.y*4)).a;
    float bottom=tex2D(mask,uv+float2(0,step.y*4)).a;
    float2 gradient=float2(left-right,top-bottom);
    float2 normal=gradient/max(.001,length(gradient));
    float edge=saturate(1-min(min(left,right),min(top,bottom)));
    float fine=saturate(1-min(min(tex2D(mask,uv-float2(step.x,0)).a,tex2D(mask,uv+float2(step.x,0)).a),min(tex2D(mask,uv-float2(0,step.y)).a,tex2D(mask,uv+float2(0,step.y)).a)));
    float crown=pow(saturate(dot(normal,normalize(float2(-.55,-.83)))),3);
    float foot=pow(saturate(dot(normal,normalize(float2(.55,.83)))),4);
    float reflection=exp(-pow((uv.y-(.08+uv.x*.20))/.095,2))*.075;
    float alpha=(.09+reflection+edge*(.30*crown+.09*foot)+fine*(.42*crown+.17*foot))*a;
    float3 color=lerp(float3(.66,.86,1),float3(.98,.995,1),saturate(crown+reflection*4));
    color=lerp(color,float3(.10,.20,.32),darkInk*.90);
    return float4(color*alpha,alpha);
}
