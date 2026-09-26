using Mirage.Noding;
using Mirage.Scheduling.Interfaces;

namespace Mirage.Scheduling.Channels;

/// <summary>
/// Updates loaded <see cref="IUpdatable"/> nodes in the graph's active root.
/// </summary>
/// <param name="manager">The node manager providing the active root.</param>
/// <param name="identifier">The channel identifier. The default is <c>"nodes"</c>.</param>
/// <param name="priority">The update priority. The default is <see cref="UpdateChannelPriority.Normal"/>.</param>
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
