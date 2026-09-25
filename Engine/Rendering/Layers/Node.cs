using Mirage.Graphics.Interfaces;
using Mirage.Noding;

namespace Mirage.Rendering.Layers;

/// <summary>
/// Draws loaded drawable nodes from the active root.
/// </summary>
public sealed class NodeDrawLayer(
    NodeManager manager,
    string identifier = "nodes",
    DrawLayerPriority priority = DrawLayerPriority.Normal
) : DrawLayer(identifier, priority)
{
    private static void RenderNode(Node node, RenderContext context)
    {
        if (node.Destroyed)
            return;

        if (node.Loaded && node is IDrawable drawable)
            drawable.Draw(context);

        foreach (var child in node.Subnodes)
            RenderNode(child, context);
    }

    /// <summary>
    /// Draws the renderable nodes under the active root.
    /// </summary>
    /// <param name="context">The active rendering context.</param>
    protected override void OnDraw(RenderContext context)
    {
        RenderNode(manager.ActiveRoot.Get(), context);
    }
}
