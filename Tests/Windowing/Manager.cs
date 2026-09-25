using Mirage.Common;
using Mirage.Scheduling;
using Mirage.Windowing;

namespace Tests.Windowing;

public class WindowManagerTest
{
    [Fact]
    public void Constructor_WithDuplicateWindowIdentifier_Throws()
    {
        var first = new TestWindow("test");
        var second = new TestWindow("test");

        Assert.Throws<InvalidOperationException>(() => new WindowManager([first, second]));
    }

    [Fact]
    public void Start_ComposesWindowsBeforeOpening_AndOnlyOnce()
    {
        var composed = new TestWindow("composed");
        var windowing = new TrackingWindowManager(composed);
        var game = new TestGame(windowing);

        game.Start();
        game.Stop();
        game.Start();

        Assert.Equal(1, windowing.ComposeCalls);
        Assert.Same(composed, windowing.Windows["composed"]);
        Assert.Equal(2, composed.OpenCalls);

        game.Stop();
    }

    [Fact]
    public void Start_OpensAllWindows_AndStopClosesThemInReverseOrder()
    {
        var closeOrder = new List<string>();
        var first = new TestWindow("first", closeOrder);
        var second = new TestWindow("second", closeOrder);
        var windowing = new WindowManager([first, second]);
        var game = new TestGame(windowing);

        game.Start();

        Assert.True(first.Opened.Get());
        Assert.True(second.Opened.Get());

        game.Stop();

        Assert.Equal(["second", "first"], closeOrder);
        Assert.False(first.Opened.Get());
        Assert.False(second.Opened.Get());
    }

    [Fact]
    public void Start_WhenComposedWindowDuplicatesRegisteredWindow_Throws()
    {
        var registered = new TestWindow("test");
        var composed = new TestWindow("test");
        var windowing = new TrackingWindowManager(composed, [registered]);
        var game = new TestGame(windowing);

        Assert.Throws<InvalidOperationException>(game.Start);
        Assert.Equal(ModuleState.Idle, windowing.State.Get());
        Assert.Same(registered, windowing.Windows["test"]);
    }

    [Fact]
    public void Start_WhenWindowFailsToOpen_RollsBackPreviouslyOpenedWindows()
    {
        var closeOrder = new List<string>();
        var first = new TestWindow("first", closeOrder);
        var failing = new TestWindow("failing", closeOrder, failOnOpen: true);
        var windowing = new WindowManager([first, failing]);
        var game = new TestGame(windowing);

        Assert.Throws<InvalidOperationException>(game.Start);

        Assert.Equal(["first"], closeOrder);
        Assert.False(first.Opened.Get());
        Assert.False(failing.Opened.Get());
        Assert.Equal(ModuleState.Idle, windowing.State.Get());
    }

    [Fact]
    public void Update_ProcessesEveryWindow()
    {
        var first = new TestWindow("first");
        var second = new TestWindow("second");
        var windowing = new WindowManager([first, second]);

        first.Open();
        second.Open();
        windowing.Update(0.016);

        Assert.Equal(1, first.ProcessCalls);
        Assert.Equal(1, second.ProcessCalls);
    }

    private sealed class TestGame(WindowManager windowing)
        : Game([new Mirage.Scheduling.Scheduler(), windowing]);

    private sealed class TestWindow(
        string identifier,
        List<string>? closeOrder = null,
        bool failOnOpen = false
    ) : Window(new WindowOptions { Identifier = identifier })
    {
        public int OpenCalls { get; private set; }
        public int ProcessCalls { get; private set; }

        protected override void OnClose() => closeOrder?.Add(Identifier);

        protected override void OnOpen()
        {
            OpenCalls++;

            if (failOnOpen)
                throw new InvalidOperationException("Open failed.");
        }

        protected override void OnProcess() => ProcessCalls++;
    }

    private sealed class TrackingWindowManager(Window composed, IEnumerable<Window>? windows = null)
        : WindowManager(windows)
    {
        public int ComposeCalls { get; private set; }

        protected override IEnumerable<Window> Compose()
        {
            ComposeCalls++;
            return [composed];
        }
    }
}
