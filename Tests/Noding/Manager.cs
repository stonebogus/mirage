using Mirage.Common;
using Mirage.Noding;

namespace Tests.Noding;

public class NodeManagerTest
{
    [Fact]
    public void Constructor_RegistersInitialAndAdditionalRoots()
    {
        var initial = new Node("Initial");
        var additional = new Node("Additional");

        var graph = new NodeManager(initial, [additional]);

        Assert.Equal(2, graph.Roots.Count);
        Assert.Same(initial, graph.Roots["Initial"]);
        Assert.Same(additional, graph.Roots["Additional"]);
        Assert.Same(initial, graph.ActiveRoot.Get());
    }

    [Fact]
    public void Roots_WhenRegisteringChild_Throws()
    {
        var root = new Node("Root");
        var child = new Node("Child", new NodeOptions { Parent = root });
        var graph = new NodeManager(root);

        Assert.Throws<InvalidOperationException>(() => graph.Roots.Add("Child", child));
        Assert.DoesNotContain(graph.Roots, entry => ReferenceEquals(entry.Value, child));
    }

    [Fact]
    public void Roots_WhenRemovingActiveRoot_RestoresRootAndThrows()
    {
        var root = new Node("Root");
        var graph = new NodeManager(root);

        Assert.Throws<InvalidOperationException>(() => graph.Roots.Remove("Root"));

        Assert.Same(root, graph.Roots["Root"]);
        Assert.Same(root, graph.ActiveRoot.Get());
    }

    [Fact]
    public void Start_LoadsActiveRoot()
    {
        var root = new Node("Root");
        var graph = new NodeManager(root);

        var game = new TestGame(graph);
        game.Start();

        Assert.Equal(ModuleState.Running, graph.State.Get());
        Assert.True(root.Loaded);

        game.Stop();
    }

    [Fact]
    public void Switch_WhenIdle_ChangesActiveRootWithoutLoadingIt()
    {
        var initial = new Node("Initial");
        var next = new Node("Next");
        var graph = new NodeManager(initial, [next]);

        graph.Switch("Next");

        Assert.Same(next, graph.ActiveRoot.Get());
        Assert.False(initial.Loaded);
        Assert.False(next.Loaded);
    }

    [Fact]
    public void Switch_WhenRootDoesNotExist_Throws()
    {
        var graph = new NodeManager(new Node("Initial"));

        Assert.Throws<InvalidOperationException>(() => graph.Switch("Missing"));
    }

    [Fact]
    public void Switch_WhenRunning_UnloadsCurrentRootAndLoadsNextRoot()
    {
        var initial = new Node("Initial");
        var next = new Node("Next");
        var graph = new NodeManager(initial, [next]);

        var game = new TestGame(graph);
        game.Start();
        graph.Switch("Next");

        Assert.Same(next, graph.ActiveRoot.Get());
        Assert.False(initial.Loaded);
        Assert.True(next.Loaded);

        game.Stop();
    }

    private sealed class TestGame(Module module) : Game([module]);
}
