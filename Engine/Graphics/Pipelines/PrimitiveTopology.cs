namespace Mirage.Graphics.Pipelines;

/// <summary>
/// Specifies how vertices are assembled into primitives.
/// </summary>
public enum PrimitiveTopology
{
    /// <summary>
    /// Every three vertices form an independent triangle.
    /// </summary>
    TriangleList,

    /// <summary>
    /// Consecutive vertices form connected triangles.
    /// </summary>
    TriangleStrip,

    /// <summary>
    /// Every two vertices form an independent line.
    /// </summary>
    LineList,

    /// <summary>
    /// Consecutive vertices form connected lines.
    /// </summary>
    LineStrip,

    /// <summary>
    /// Every vertex forms an independent point.
    /// </summary>
    PointList,
}
