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
    float alpha=(.30+reflection+edge*(.36*crown+.13*foot)+fine*(.58*crown+.23*foot))*a;
    float3 color=lerp(float3(.88,.88,.88),float3(1,1,1),saturate(crown+reflection*4));
    color=lerp(color,float3(.13,.13,.13),darkInk*.90);
    float2 sourceUV=saturate(offset+uv*extent+normal*edge*extent*.018);
    float3 photo=tex2D(wallpaper,sourceUV).rgb;
    // Use the photo's light only, never paint its blue/green hue into the
    // lettering. The actual wallpaper remains visible through the alpha.
    float light=dot(photo,float3(.2126,.7152,.0722));
    float3 imageGlass=saturate(color*lerp(.86,1.05,light));
    float clearAlpha=saturate(.30+frost*.30+reflection+edge*(.30*crown+.12*foot)+fine*(.48*crown+.20*foot))*a;
    // An opposing neutral reflection gives the transparent body a curved lip.
    imageGlass=lerp(imageGlass,float3(.18,.18,.18),fine*foot*.36);
    return lerp(float4(color*alpha,alpha),float4(imageGlass*clearAlpha,clearAlpha),hasWallpaper);
}
