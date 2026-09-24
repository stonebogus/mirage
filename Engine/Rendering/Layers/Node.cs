using Mirage.Graphics.Interfaces;
using Mirage.Noding;

namespace Mirage.Rendering.Layers;

/// <summary>
/// Collects loaded <see cref="IRenderable"/> nodes from the graph's active root.
/// </summary>
public sealed class NodeRenderLayer(
    NodeManager manager,
    string identifier = "nodes",
    RenderLayerPriority priority = RenderLayerPriority.Normal
) : RenderLayer(identifier, priority)
{
    private void Visit(Node node)
    {
        if (node.Destroyed)
            return;

        if (node.Loaded && node is IRenderable renderable)
            Entries.Add(renderable);

        foreach (var child in node.Subnodes)
            Visit(child);
    }

    /// <inheritdoc />
    protected override void OnCollect(IReadOnlyList<IRenderable> entries)
    {
        Entries.Clear();
        Visit(manager.ActiveRoot.Get());
    }
}
