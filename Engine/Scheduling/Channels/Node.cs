using Mirage.Noding;
using Mirage.Scheduling.Interfaces;

namespace Mirage.Scheduling.Channels;

/// <summary>
/// Updates loaded <see cref="IUpdatable"/> nodes in the graph's active root.
/// </summary>
public sealed class NodeUpdateChannel(
    NodeManager manager,
    string identifier = "nodes",
    UpdateChannelPriority priority = UpdateChannelPriority.Normal
) : UpdateChannel(identifier, priority)
{
    private readonly NodeManager _graph = manager;

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
