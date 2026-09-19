using Mirage.Common;
using Mirage.Windowing.Windows;
using SchedulerModule = Mirage.Scheduler.Scheduler;
using WindowingModule = Mirage.Windowing.Windowing;

namespace Tests.Windowing;

public class WindowingTest
{
    [Fact]
    public void Start_ComposesWindowsBeforeOpening_AndOnlyOnce()
    {
        var composed = new TestWindow("composed");
        var windowing = new TrackingWindowing(composed);
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
    public void Start_WhenComposedWindowDuplicatesRegisteredWindow_Throws()
    {
        var registered = new TestWindow("test");
        var composed = new TestWindow("test");
        var windowing = new TrackingWindowing(composed, [registered]);
        var game = new TestGame(windowing);

        Assert.Throws<InvalidOperationException>(game.Start);
        Assert.Equal(ModuleState.Idle, windowing.State.Get());
        Assert.Same(registered, windowing.Windows["test"]);
    }

    [Fact]
    public void Constructor_WithDuplicateWindowIdentifier_Throws()
    {
        var first = new TestWindow("test");
        var second = new TestWindow("test");

        Assert.Throws<InvalidOperationException>(() => new WindowingModule([first, second]));
    }

    [Fact]
    public void Start_OpensAllWindows_AndStopClosesThemInReverseOrder()
    {
        var closeOrder = new List<string>();
        var first = new TestWindow("first", closeOrder);
        var second = new TestWindow("second", closeOrder);
        var windowing = new WindowingModule([first, second]);
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
    public void Start_WhenWindowFailsToOpen_RollsBackPreviouslyOpenedWindows()
    {
        var closeOrder = new List<string>();
        var first = new TestWindow("first", closeOrder);
        var failing = new TestWindow("failing", closeOrder, failOnOpen: true);
        var windowing = new WindowingModule([first, failing]);
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
        var windowing = new WindowingModule([first, second]);

        windowing.Update(0.016);

        Assert.Equal(1, first.ProcessCalls);
        Assert.Equal(1, second.ProcessCalls);
    }

    private sealed class TrackingWindowing(
        Window composed,
        IEnumerable<Window>? windows = null
    ) : WindowingModule(windows)
    {
        public int ComposeCalls { get; private set; }

        protected override IEnumerable<Window> Compose()
        {
            ComposeCalls++;
            return [composed];
        }
    }

    private sealed class TestGame(WindowingModule windowing)
        : Game([new SchedulerModule(), windowing]);

    private sealed class TestWindow(
        string identifier,
        List<string>? closeOrder = null,
        bool failOnOpen = false
    ) : Window(
        new WindowOptions { Identifier = identifier }
    )
    {
        public int OpenCalls { get; private set; }
        public int ProcessCalls { get; private set; }

        protected override void OnOpen()
        {
            OpenCalls++;

            if (failOnOpen)
                throw new InvalidOperationException("Open failed.");
        }

        protected override void OnClose() => closeOrder?.Add(Identifier);

        internal override void Process() => ProcessCalls++;
    }
}
