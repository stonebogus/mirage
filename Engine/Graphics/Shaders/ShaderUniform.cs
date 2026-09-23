namespace Mirage.Graphics.Shaders;

/// <summary>
/// Contains uniform data supplied to a programmable shader stage.
/// </summary>
/// <param name="Slot">
/// The shader uniform slot receiving the data.
/// </param>
/// <param name="Stage">
/// The programmable shader stage that consumes the data.
/// </param>
/// <param name="Data">
/// The raw uniform bytes in the memory layout expected by the shader.
/// </param>
public readonly record struct ShaderUniform(
    uint Slot,
    ShaderStage Stage,
    ReadOnlyMemory<byte> Data
);
