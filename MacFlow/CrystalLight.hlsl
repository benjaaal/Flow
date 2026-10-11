// Lighting only. The live background is supplied by Windows' compositor.
// This shader does not sample or refract the desktop, nor process UI content.
float2 size : register(c0);
float radius : register(c1);
float2 pointer : register(c2);
float activity : register(c3);
float intensity : register(c4);

float4 main(float2 uv : TEXCOORD) : COLOR
{
    float2 halfSize = size * .5;
    float r = min(radius, min(halfSize.x, halfSize.y));
    float2 p = uv * size - halfSize;
    float2 q = abs(p) - (halfSize - r);
    float2 outside = max(q, 0);
    float d = length(outside) + min(max(q.x, q.y), 0) - r;
    float coverage = 1 - smoothstep(-.8, .1, d);
    float depth = max(0, -d);
    float2 normal = length(outside) > .001 ? normalize(outside) * sign(p)
        : (q.x > q.y ? float2(sign(p.x), 0) : float2(0, sign(p.y)));
    // A directional meniscus fades into the surface. No uniform white stroke.
    float bevel = exp(-depth * .72);
    float hairline = exp(-pow((depth - .65) / .62, 2));
    float upper = pow(saturate(dot(normal, normalize(float2(-.36, -.93)))), 6);
    float lower = pow(saturate(dot(normal, normalize(float2(.52, .85)))), 9);
    float cursor = exp(-length((uv-pointer) * size) / 100) * activity;
    float cornerLight = .25 + .75 * exp(-pow((abs(p.x)-(halfSize.x-r*.6))/max(8,r*1.5),2));
    float lit = (.075 * bevel + .26 * hairline * cornerLight) * upper
              + (.02 * bevel + .035 * hairline * cornerLight) * lower
              + cursor * bevel * .08;
    float shadow = 0;
    float alpha = saturate((lit + shadow) * intensity) * coverage;
    float3 color = lerp(float3(.08,.10,.14), float3(.94,.97,1), lit / max(.0001,lit+shadow));
    return float4(color * alpha, alpha);
}
