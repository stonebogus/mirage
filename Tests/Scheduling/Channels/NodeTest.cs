using Mirage.Noding;
using Mirage.Scheduling.Channels;
using Mirage.Scheduling.Interfaces;

namespace Tests.Scheduling.Channels;

public class NodeUpdateChannelTest
{
    [Fact]
    public void Constructor_CreatesChannelWithDefaultValues()
    {
        var graph = new NodeManager(new Node("Root"));
        var channel = new NodeUpdateChannel(graph);

        Assert.Equal("nodes", channel.Identifier);
        Assert.Equal(UpdateChannelPriority.Normal, channel.Priority);
        Assert.Empty(channel.Entries);
    }

    [Fact]
    public void Constructor_WithPriority_SetsPriority()
    {
        var graph = new NodeManager(new Node("Root"));
        var channel = new NodeUpdateChannel(graph, "gameplay", UpdateChannelPriority.High);

        Assert.Equal("gameplay", channel.Identifier);
        Assert.Equal(UpdateChannelPriority.High, channel.Priority);
    }

    [Fact]
    public void Update_DoesNotUpdateUnloadedNodes()
    {
        var child = new TestNode("Child");
        var root = new TestNode("Root", new NodeOptions { Subnodes = [child] });
        var channel = new NodeUpdateChannel(new NodeManager(root));

        channel.Update(0.25);

        Assert.Equal(0, root.UpdateCount);
        Assert.Equal(0, child.UpdateCount);
    }

    [Fact]
    public void Update_ReachesLoadedPersistentNodeUnderUnloadedParent()
    {
        var persistent = new TestNode("Persistent", new NodeOptions { Persistent = true });
        var regular = new TestNode("Regular");
        var branch = new TestNode("Branch", new NodeOptions { Subnodes = [persistent, regular] });
        var root = new Node("Root", new NodeOptions { Subnodes = [branch] });
        var channel = new NodeUpdateChannel(new NodeManager(root));

        root.Load();
        branch.Unload();
        channel.Update(0.25);

        Assert.False(branch.Loaded);
        Assert.True(persistent.Loaded);
        Assert.False(regular.Loaded);
        Assert.Equal(0, branch.UpdateCount);
        Assert.Equal(1, persistent.UpdateCount);
        Assert.Equal(0, regular.UpdateCount);
    }

    [Fact]
    public void Update_UpdatesLoadedNodesThroughNonUpdatableParents()
    {
        var grandchild = new TestNode("Grandchild");
        var branch = new Node("Branch", new NodeOptions { Subnodes = [grandchild] });
        var root = new TestNode("Root", new NodeOptions { Subnodes = [branch] });
        var channel = new NodeUpdateChannel(new NodeManager(root));

        root.Load();
        channel.Update(0.25);

        Assert.Equal(1, root.UpdateCount);
        Assert.Equal(1, grandchild.UpdateCount);
        Assert.Equal(0.25, root.LastDeltaTime);
        Assert.Equal(0.25, grandchild.LastDeltaTime);
    }

    [Fact]
    public void Update_WhenNodeDestroysItself_DoesNotVisitItsSubnodes()
    {
        var child = new TestNode("Child");
        var parent = new SelfDestroyingNode("Parent", new NodeOptions { Subnodes = [child] });
        var root = new Node("Root", new NodeOptions { Subnodes = [parent] });
        var channel = new NodeUpdateChannel(new NodeManager(root));

        root.Load();
        channel.Update(0.25);

        Assert.True(parent.Destroyed);
        Assert.True(child.Destroyed);
        Assert.Equal(1, parent.UpdateCount);
        Assert.Equal(0, child.UpdateCount);
    }

    private sealed class SelfDestroyingNode(string name, NodeOptions? options = null)
        : Node(name, options),
            IUpdatable
    {
        public int UpdateCount { get; private set; }

        public void Update(double deltaTime)
        {
            UpdateCount++;
            Destroy();
        }
    }

    private sealed class TestNode(string name, NodeOptions? options = null)
        : Node(name, options),
            IUpdatable
    {
        public double LastDeltaTime { get; private set; }
        public int UpdateCount { get; private set; }

        public void Update(double deltaTime)
        {
            UpdateCount++;
            LastDeltaTime = deltaTime;
        }
    }
}
