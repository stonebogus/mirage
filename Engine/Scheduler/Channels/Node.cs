using Mirage.Graph;
using Mirage.Scheduler;
using Mirage.Scheduler.Interfaces;

namespace Mirage.Scheduler.Channels;

/// <summary>
/// Updates loaded <see cref="IUpdatable"/> nodes in the graph's active root.
/// </summary>
public sealed class NodeUpdateChannel(
    Graph.Graph graph,
    string identifier = "nodes",
    UpdateChannelPriority priority = UpdateChannelPriority.Normal
) : UpdateChannel(identifier, priority)
{
    private readonly Graph.Graph _graph = graph ?? throw new ArgumentNullException(nameof(graph));

    private static void Visit(Node node, double deltaTime)
    {
        if (node.Destroyed)
            return;

        if (node.Loaded && node is IUpdatable updatable)
            updatable.Update(deltaTime);

        if (node.Destroyed)
            return;

        foreach (var child in node.Subnodes.ToArray())
            Visit(child, deltaTime);
    }

    /// <inheritdoc />
    protected override void OnUpdate(double deltaTime)
    {
        Visit(_graph.ActiveRoot.Get(), deltaTime);
    }
}
