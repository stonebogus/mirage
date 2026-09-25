struct FragmentInput
{
    float4 Position : SV_Position;
    float3 Color : TEXCOORD0;
};

float4 main(FragmentInput input) : SV_Target0
{
    return float4(input.Color, 1.0);
}