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
    /// <param name="options">The additional options for the node</param>>
    public Sprite(string name, Texture texture, SpatialNodeOptions? options = null)
        : base(name, options)
    {
        Texture = new Store<Texture>(texture);
        Size = new Store<Vector2>(new Vector2(texture.Width, texture.Height));
    }

    /// <summary>
    /// Draws the current texture at the sprite's current position and size.
    /// </summary>
    /// <param name="context">The active drawing context.</param>
    public void Draw(IDrawContext context)
    {
        var transform = GlobalTransform;
        var globalSize = new Vector2(Size.Get().X * transform.M11, Size.Get().Y * transform.M22);

        context.DrawTexture(Texture.Get(), GlobalPosition, globalSize);
    }
}
