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
    /// Gets the initial normalized pivot of the image.
    /// </summary>
    /// <remarks>
    /// (0, 0) is the top-left corner, (0.5, 0.5) is the center,
    /// and (1, 1) is the bottom-right corner.
    /// </remarks>
    public Vector2 Pivot { get; init; } = new(0.5f, 0.5f);

    /// <summary>
    /// Gets the initial drawn size, or <see langword="null"/> to use
    /// the texture's natural size.
    /// </summary>
    public Vector2? Size { get; init; }
}

/// <summary>
/// Draws a texture as a node in the scene.
/// </summary>
/// <remarks>
/// The spatial origin controls the node's transformation and its children.
/// <see cref="Pivot"/> controls how the image is placed around that origin.
/// </remarks>
public class Sprite : SpatialNode, IDrawable
{
    /// <summary>
    /// Gets the normalized point of the image placed at this node's origin.
    /// </summary>
    public readonly Store<Vector2> Pivot;

    /// <summary>
    /// Gets the size at which the sprite is drawn.
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
    /// <param name="options">The initial sprite and spatial values.</param>
    public Sprite(string name, Texture texture, SpriteOptions? options = null)
        : base(name, options)
    {
        ArgumentNullException.ThrowIfNull(texture);

        options ??= new SpriteOptions();

        Texture = new Store<Texture>(texture);
        Size = new Store<Vector2>(options.Size ?? new Vector2(texture.Width, texture.Height));
        Pivot = new Store<Vector2>(options.Pivot);
    }

    /// <summary>
    /// Draws the texture with its pivot placed at the node's global position.
    /// </summary>
    /// <param name="context">The active drawing context.</param>
    public void Draw(IDrawContext context)
    {
        var transform = GlobalTransform;
        var size = Size.Get();

        var localTopLeft = Origin.Get() - size * Pivot.Get();
        var globalTopLeft = Vector2.Transform(localTopLeft, transform);

        var scaleX = new Vector2(transform.M11, transform.M12).Length();
        var scaleY = new Vector2(transform.M21, transform.M22).Length();

        context.DrawTexture(
            Texture.Get(),
            globalTopLeft,
            new Vector2(size.X * scaleX, size.Y * scaleY)
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
