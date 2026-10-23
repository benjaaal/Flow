// Original procedural glass and spectral ribbons. No video/bitmap animation.
float2 size : register(c0);
float time : register(c1);
float energy : register(c2);
float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 extent=max(size,float2(1,1));float2 half=extent*.5-float2(5,5);float r=min(half.x,half.y);
    float2 p=(uv-.5)*extent;float2 q=abs(p)-(half-r);float2 corner=max(q,0);
    float d=length(corner)+min(max(q.x,q.y),0)-r;float cover=1-smoothstep(-.6,.4,d);
    float sphere=1-saturate((extent.x/extent.y-1)/2.3);float x=p.x/max(1,half.x),y=p.y/max(1,r);
    float2 n=length(corner)>.001?normalize(corner)*sign(p):(q.x>q.y?float2(sign(p.x),0):float2(0,sign(p.y)));
    float crown=pow(saturate(dot(n,normalize(float2(-.45,-.9)))),3);
    float lip=exp(-max(0,-d)*1.2);float alpha=lerp(.92,.14,smoothstep(-.04,.42,y))*sphere+(1-sphere)*.94;
    float3 base=float3(.009,.012,.019);float lower=exp(-pow((y-.63)/.20,2))*exp(-pow(x/.7,2));
    base+=lower*float3(.09,.13,.19)*sphere;
    float a=.12+.18*energy;float speed=time*(.65+energy*.8);
    float envelope=pow(saturate(1-x*x),.65);
    float center=sin(x*3.6+speed)*a*envelope+sin(x*6.2-speed*.8)*.045*envelope;
    center-=.24*(1-sphere);
    float red=exp(-pow((y-center-.13*sin(x*2.8-speed))/.15,2))*envelope;
    float blue=exp(-pow((y-center+.15*sin(x*3.2+speed*1.1))/.13,2))*envelope;
    float mint=exp(-pow((y-center-.10*cos(x*3.4+speed*.8))/.09,2))*envelope;
    float3 wave=red*float3(1,.16,.045)+blue*float3(.04,.17,1)+mint*float3(.14,.95,.80);
    float crest=max(red,max(blue,mint));float bright=pow(saturate((red+blue+mint)*.43),3);
    float3 color=base+wave*(.55+.34*energy)+bright*float3(.9,.88,.75);
    float haze=exp(-pow((y-center)/.45,2))*envelope*.13*energy;color+=haze*float3(.17,.22,.45);
    color+=lip*(.035+.16*crown)*float3(.72,.83,1);
    float highlight=exp(-pow((y+.68)/.08,2))*exp(-pow((x+.2)/.47,2))*.05*sphere;color+=highlight;
    alpha=saturate(alpha+crest*.8+lip*.15);return float4(saturate(color)*alpha,alpha*cover)*float4(cover,cover,cover,1);
}
