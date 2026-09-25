using Mirage.Common;

namespace Mirage.Graphics.Shaders;

/// <summary>
/// Specifies a programmable graphics pipeline stage.
/// </summary>
public enum ShaderStage
{
    /// <summary>
    /// Processes individual vertices.
    /// </summary>
    Vertex,

    /// <summary>
    /// Processes rasterized fragments.
    /// </summary>
    Fragment,

    /// <summary>
    /// Performs general-purpose GPU computation.
    /// </summary>
    Compute,
}

/// <summary>
/// Specifies the source language of a shader.
/// </summary>
public enum ShaderLanguage
{
    /// <summary>
    /// High-Level Shader Language.
    /// </summary>
    Hlsl,
}

/// <summary>
/// Represents shader source code used by the graphics API.
/// </summary>
public sealed class Shader : Resource
{
    /// <summary>
    /// Initializes a new shader.
    /// </summary>
    /// <param name="source">
    /// The shader source code.
    /// </param>
    /// <param name="stage">
    /// The programmable stage executed by the shader.
    /// </param>
    /// <param name="entryPoint">
    /// The shader function used as the entry point.
    /// </param>
    /// <param name="language">
    /// The language in which the shader is written.
    /// </param>
    public Shader(
        string source,
        ShaderStage stage,
        string entryPoint = "main",
        ShaderLanguage language = ShaderLanguage.Hlsl
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryPoint);

        Source = source;
        Stage = stage;
        EntryPoint = entryPoint;
        Language = language;
    }

    /// <summary>
    /// Gets the shader entry point.
    /// </summary>
    public string EntryPoint { get; }

    /// <summary>
    /// Gets the shader source language.
    /// </summary>
    public ShaderLanguage Language { get; }

    /// <summary>
    /// Gets the shader source code.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the programmable stage executed by the shader.
    /// </summary>
    public ShaderStage Stage { get; }
}
