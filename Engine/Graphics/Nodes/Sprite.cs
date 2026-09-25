using System.Numerics;
using Mirage.Common.Events;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Resources;
using Mirage.Spatial;
using Mirage.Spatial.Nodes;

namespace Mirage.Graphics.Nodes;

/// <summary>
/// Draws a texture as a node in the scene.
/// </summary>
public class Sprite : SpatialNode, IDrawable
{
    /// <summary>
    /// Gets the size at which the sprite is drawn.
    /// </summary>
    public readonly Store<Vector2> Size;

    /// <summary>
    /// Gets the texture displayed by this sprite.
    /// </summary>
    public readonly Store<Texture> Texture;

    /// <summary>
    /// Initializes a sprite using the texture's natural size.
    /// </summary>
    /// <param name="name">The node name.</param>
    /// <param name="texture">The initial texture.</param>
    public Sprite(string name, Texture texture)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(texture);

        Texture = new Store<Texture>(texture);
        Size = new Store<Vector2>(new Vector2(texture.Width, texture.Height));
    }

    /// <summary>
    /// Draws the current texture at the sprite's current position and size.
    /// </summary>
    /// <param name="context">The active drawing context.</param>
    public void Draw(IDrawContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.DrawTexture(Texture.Get(), Position.Get(), Size.Get());
    }
}
