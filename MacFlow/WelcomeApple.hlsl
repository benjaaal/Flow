// Mask lighting and subtle displacement of the welcome's own wallpaper image.
// Never samples or refracts other applications or the live Windows desktop.
sampler2D mask : register(s0);
sampler2D wallpaper : register(s1);
float darkInk : register(c0);
float2 offset : register(c1);
float2 extent : register(c2);
float hasWallpaper : register(c3);
float2 shapeSize : register(c4);
float frost : register(c5);
float4 main(float2 uv : TEXCOORD) : COLOR
{
    float a=tex2D(mask,uv).a;
    float2 step=1.0/max(float2(1,1),shapeSize);
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
    float reflection=exp(-pow((uv.y-(.08+uv.x*.20))/.095,2))*.13;
    float alpha=(.18+reflection+edge*(.36*crown+.13*foot)+fine*(.58*crown+.23*foot))*a;
    float3 color=lerp(float3(.66,.86,1),float3(.98,.995,1),saturate(crown+reflection*4));
    color=lerp(color,float3(.10,.20,.32),darkInk*.90);
    float2 sourceUV=saturate(offset+uv*extent+normal*edge*extent*.018);
    float3 photo=tex2D(wallpaper,sourceUV).rgb;
    float3 imageGlass=lerp(photo,color,saturate(frost+reflection+edge*.12+fine*.30));
    return lerp(float4(color*alpha,alpha),float4(imageGlass*a,a),hasWallpaper);
}
