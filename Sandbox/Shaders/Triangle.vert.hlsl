cbuffer Transform : register(b0, space1)
{
    float2 ObjectPosition;
    float ObjectRotation;
    float Padding0;

    float2 ObjectScale;
    float2 Padding1;
};

struct VertexInput
{
    float2 Position : TEXCOORD0;
    float3 Color : TEXCOORD1;
};

struct VertexOutput
{
    float4 Position : SV_Position;
    float3 Color : TEXCOORD0;
};

VertexOutput main(VertexInput input)
{
    float sine = sin(ObjectRotation);
    float cosine = cos(ObjectRotation);

    float2 position = input.Position * ObjectScale;

    float2 rotated = float2(
        position.x * cosine - position.y * sine,
        position.x * sine + position.y * cosine
    );

    VertexOutput output;

    output.Position = float4(
        rotated + ObjectPosition,
        0.0f,
        1.0f
    );

    output.Color = input.Color;

    return output;
}