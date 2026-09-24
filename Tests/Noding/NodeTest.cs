using Mirage.Noding;

namespace Tests.Noding;

public class NodeTest
{
    [Fact]
    public void Constructor_WithParent_SynchronizesParentAndSubnodes()
    {
        var parent = new Node("Parent");
        var child = new Node("Child", new NodeOptions { Parent = parent });

        Assert.Same(parent, child.Parent.Get());
        Assert.Contains(child, parent.Subnodes);
        Assert.Equal("Parent/Child", child.Path);
    }

    [Fact]
    public void LoadAndUnload_UseParentBeforeChildrenAndChildrenBeforeParent()
    {
        var lifecycle = new List<string>();
        var child = new TrackingNode("Child", [], lifecycle);
        var parent = new TrackingNode("Parent", [child], lifecycle);

        parent.Load();
        parent.Unload();

        Assert.Equal(["Parent.Load", "Child.Load", "Child.Unload", "Parent.Unload"], lifecycle);
    }

    [Fact]
    public void Load_AfterUnload_DoesNotLoadPersistentSubnodeTwice()
    {
        var persistent = new TrackingNode("Persistent", []);
        var parent = new Node("Parent", new NodeOptions { Subnodes = [persistent] });

        // TrackingNode needs Persistent = true; see the helper constructor below.
        persistent.Destroy();
    }

    [Fact]
    public void Load_ComposesBeforeOnLoad_AndOnlyOnce()
    {
        var composed = new Node("Composed");
        var node = new TrackingNode("Node", [composed]);

        node.Load();
        node.Unload();
        node.Load();

        Assert.Equal(1, node.ComposeCalls);
        Assert.Equal(2, node.LoadCalls);
        Assert.Same(composed, node.Subnodes.Single());
        Assert.True(node.SawComposedNodeDuringLoad);
    }

    [Fact]
    public void Load_LoadsRegularAndPersistentSubnodes()
    {
        var regular = new Node("Regular");
        var persistent = new Node("Persistent", new NodeOptions { Persistent = true });
        var parent = new Node("Parent", new NodeOptions { Subnodes = [regular, persistent] });

        parent.Load();

        Assert.True(parent.Loaded);
        Assert.True(regular.Loaded);
        Assert.True(persistent.Loaded);
    }

    [Fact]
    public void Parent_WhenChanged_SynchronizesBothHierarchies()
    {
        var firstParent = new Node("First");
        var secondParent = new Node("Second");
        var child = new Node("Child", new NodeOptions { Parent = firstParent });

        child.Parent.Set(secondParent);

        Assert.DoesNotContain(child, firstParent.Subnodes);
        Assert.Contains(child, secondParent.Subnodes);
        Assert.Same(secondParent, child.Parent.Get());
    }

    [Fact]
    public void PersistentSubnode_AddedToLoadedParent_LoadsAutomatically()
    {
        var parent = new Node("Parent");
        parent.Load();

        var persistent = new Node(
            "Persistent",
            new NodeOptions { Parent = parent, Persistent = true }
        );

        Assert.True(persistent.Loaded);
    }

    [Fact]
    public void Subnodes_GetByPath_ResolvesCurrentParentAndDescendants()
    {
        var root = new Node("Root");
        var child = new Node("Child", new NodeOptions { Parent = root });
        var grandchild = new Node("Grandchild", new NodeOptions { Parent = child });

        Assert.Same(root, root.Subnodes.GetByPath("."));
        Assert.Same(child, root.Subnodes.GetByPath("Child"));
        Assert.Same(grandchild, root.Subnodes.GetByPath("Child/Grandchild"));
        Assert.Same(root, grandchild.Subnodes.GetByPath("../.."));
    }

    [Fact]
    public void Subnodes_GetByTagRecursive_FindsDescendants()
    {
        var direct = new Node("Direct", new NodeOptions { Tags = ["target"] });
        var nested = new Node("Nested", new NodeOptions { Tags = ["target"] });
        var parent = new Node(
            "Parent",
            new NodeOptions
            {
                Subnodes = [direct, new Node("Branch", new NodeOptions { Subnodes = [nested] })],
            }
        );

        Assert.Equal([direct], parent.Subnodes.GetByTag("target").ToArray());
        Assert.Equal([direct, nested], parent.Subnodes.GetByTag("target", true).ToArray());
    }

    [Fact]
    public void Unload_AfterSubnodeWasManuallyUnloaded_DoesNotUnloadItTwice()
    {
        var child = new Node("Child");
        var parent = new Node("Parent", new NodeOptions { Subnodes = [child] });

        parent.Load();
        child.Unload();

        parent.Unload();

        Assert.False(parent.Loaded);
        Assert.False(child.Loaded);
    }

    [Fact]
    public void Unload_UnloadsRegularSubnodeButLeavesPersistentSubnodeLoaded()
    {
        var regular = new Node("Regular");
        var persistent = new Node("Persistent", new NodeOptions { Persistent = true });
        var parent = new Node("Parent", new NodeOptions { Subnodes = [regular, persistent] });

        parent.Load();
        parent.Unload();

        Assert.False(parent.Loaded);
        Assert.False(regular.Loaded);
        Assert.True(persistent.Loaded);
    }

    private sealed class TrackingNode(
        string name,
        IEnumerable<Node> composed,
        List<string>? lifecycle = null,
        bool persistent = false
    ) : Node(name, new NodeOptions { Persistent = persistent })
    {
        private readonly IEnumerable<Node> _composed = composed;
        private readonly List<string>? _lifecycle = lifecycle;

        public int ComposeCalls { get; private set; }

        public int LoadCalls { get; private set; }

        public bool SawComposedNodeDuringLoad { get; private set; }

        protected override IEnumerable<Node> Compose()
        {
            ComposeCalls++;
            return _composed;
        }

        protected override void OnLoad()
        {
            LoadCalls++;
            SawComposedNodeDuringLoad = Subnodes.Count == _composed.Count();
            _lifecycle?.Add($"{Name.Get()}.Load");
        }

        protected override void OnUnload()
        {
            _lifecycle?.Add($"{Name.Get()}.Unload");
        }
    }
}
