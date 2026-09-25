using Mirage.Graphics.Interfaces;
using Mirage.Noding;

namespace Mirage.Rendering.Layers;

/// <summary>
/// Draws loaded renderable nodes from the active root.
/// </summary>
public sealed class NodeRenderLayer(
    NodeManager manager,
    string identifier = "nodes",
    RenderLayerPriority priority = RenderLayerPriority.Normal
) : RenderLayer(identifier, priority)
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
    protected override void OnRender(RenderContext context)
    {
        RenderNode(manager.ActiveRoot.Get(), context);
    }
}
