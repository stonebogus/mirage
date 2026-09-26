using System.Numerics;
using Mirage.Common.Events;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Resources;
using Mirage.Spatial.Nodes;

namespace Mirage.Graphics.Nodes;

/// <summary>
/// Provides optional values used to initialize a <see cref="Sprite"/>.
/// </summary>
public class SpriteOptions : SpatialNodeOptions
{
    /// <summary>
    /// Gets the initial normalized pivot. The default is <c>(0.5, 0.5)</c>.
    /// </summary>
    /// <remarks>
    /// (0, 0) is the top-left corner, (0.5, 0.5) is the center,
    /// and (1, 1) is the bottom-right corner.
    /// </remarks>
    public Vector2 Pivot { get; init; } = new(0.5f, 0.5f);

    /// <summary>
    /// Gets the initial drawn size in texture pixel units, or <see langword="null"/>
    /// to use the texture's natural dimensions. The default is <see langword="null"/>.
    /// </summary>
    public Vector2? Size { get; init; }
}

/// <summary>
/// Draws a texture as a node in the scene.
/// </summary>
/// <remarks>
/// The spatial origin controls the node's transformation and its children.
/// <see cref="Pivot"/> controls how the image is placed around that origin.
/// The sprite does not own its <see cref="Texture"/> or the texture's image.
/// </remarks>
public class Sprite : SpatialNode, IDrawable
{
    /// <summary>
    /// Gets the normalized point of the image placed at this node's origin.
    /// </summary>
    public readonly Store<Vector2> Pivot;

    /// <summary>
    /// Gets the drawn size in texture pixel units.
    /// </summary>
    public readonly Store<Vector2> Size;

    /// <summary>
    /// Gets the texture displayed by this sprite.
    /// </summary>
    public readonly Store<Texture> Texture;

    /// <summary>
    /// Initializes a sprite using the supplied options.
    /// </summary>
    /// <param name="name">The node name.</param>
    /// <param name="texture">The initial texture.</param>
    /// <param name="options">The initial sprite and spatial values, or <see langword="null"/> for defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="texture"/> is <see langword="null"/>.
    /// </exception>
    public Sprite(string name, Texture texture, SpriteOptions? options = null)
        : base(name, options)
    {
        options ??= new SpriteOptions();

        Texture = new Store<Texture>(texture);
        Size = new Store<Vector2>(options.Size ?? new Vector2(texture.Width, texture.Height));
        Pivot = new Store<Vector2>(options.Pivot);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Draws the texture with its pivot placed at the node's global position.
    /// </remarks>
    public void Draw(IDrawContext context)
    {
        var size = Size.Get();
        var topLeft = Origin.Get() - size * Pivot.Get();
        var transform = GlobalTransform;

        context.DrawTexture(
            Texture.Get(),
            Vector2.Transform(topLeft, transform),
            Vector2.Transform(topLeft + new Vector2(size.X, 0f), transform),
            Vector2.Transform(topLeft + size, transform),
            Vector2.Transform(topLeft + new Vector2(0f, size.Y), transform)
        );
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Pivot.Destroy();
        Size.Destroy();
        Texture.Destroy();

        base.OnDestroy();
    }
}
