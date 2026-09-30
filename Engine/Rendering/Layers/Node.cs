using Mirage.Graphics.Interfaces;
using Mirage.Noding;

namespace Mirage.Rendering.Layers;

/// <summary>
/// Initializes a new instance of the <see cref="NodeRenderLayer"/> class.
/// </summary>
/// <remarks>
/// Renders loaded renderable nodes from the active root.
/// The node manager and its nodes are borrowed and are never destroyed by this object.
/// </remarks>
/// <param name="manager">The node manager providing the active root.</param>
/// <param name="identifier">The layer identifier. The default is <c>"nodes"</c>.</param>
/// <param name="priority">The rendering priority. The default is <see cref="RenderLayerPriority.Normal"/>.</param>
public class NodeRenderLayer(
    NodeManager manager,
    string identifier = "nodes",
    RenderLayerPriority priority = RenderLayerPriority.Normal
) : RenderLayer(identifier, priority)
{
    private static void RenderNode(Node node, IRenderContext context)
    {
        if (node.Destroyed)
            return;

        if (node.Loaded && node is IRenderable renderable)
            renderable.Render(context);

        foreach (var child in node.Subnodes)
            RenderNode(child, context);
    }

    /// <summary>
    /// Renders the renderable nodes under the active root.
    /// </summary>
    /// <param name="context">The active rendering context.</param>
    protected override void OnRender(IRenderContext context)
    {
        RenderNode(manager.ActiveRoot.Get(), context);
    }
}
