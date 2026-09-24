using Mirage.Graphics.Interfaces;
using Mirage.Renderer;

namespace Mirage.Graph.Rendering;

/// <summary>
/// Collects loaded <see cref="IRenderable"/> nodes from the graph's active root.
/// </summary>
public sealed class NodeRenderLayer(
    Graph graph,
    string identifier = "nodes",
    RenderLayerPriority priority = RenderLayerPriority.Normal
) : RenderLayer(identifier, priority)
{
    private readonly Graph _graph = graph;

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
        Visit(_graph.ActiveRoot.Get());
    }
}
