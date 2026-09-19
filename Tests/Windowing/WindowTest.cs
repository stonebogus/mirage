using Mirage.Common.Lifecycle;
using Mirage.Math.Vectors;
using Mirage.Windowing.Windows;

namespace Tests.Windowing;

public class WindowTest
{
    [Fact]
    public void Close_IsIdempotentAndResetsState()
    {
        var window = new TestWindow();

        window.OpenForTest();
        window.FocusedForTest = true;
        window.CloseForTest();
        window.CloseForTest();

        Assert.False(window.Opened.Get());
        Assert.False(window.Focused.Get());
        Assert.False(window.Visible.Get());
        Assert.Equal(1, window.CloseCalls);
    }

    [Fact]
    public void Constructor_UsesDefaultOptions()
    {
        var window = new TestWindow();

        Assert.Equal("window", window.Identifier);
        Assert.Equal(WindowMode.Normal, window.Mode.Get());
        Assert.Equal(new Vector2D(), window.Position.Get());
        Assert.True(window.Resizable.Get());
        Assert.Equal(new Vector2D(800, 600), window.Size.Get());
        Assert.Equal("Mirage", window.Title.Get());
        Assert.True(window.Visible.Get());
        Assert.False(window.Focused.Get());
        Assert.False(window.Opened.Get());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyIdentifier_Throws(string identifier)
    {
        Assert.Throws<ArgumentException>(() =>
            new TestWindow(new WindowOptions { Identifier = identifier })
        );
    }

    [Fact]
    public void Constructor_WithOptions_StoresConfiguredValues()
    {
        var window = new TestWindow(
            new WindowOptions
            {
                Identifier = "main",
                Mode = WindowMode.Maximized,
                Position = new Vector2D(100, 200),
                Resizable = false,
                Size = new Vector2D(1280, 720),
                Title = "Main",
                Visible = false,
            }
        );

        Assert.Equal("main", window.Identifier);
        Assert.Equal(WindowMode.Maximized, window.Mode.Get());
        Assert.Equal(new Vector2D(100, 200), window.Position.Get());
        Assert.False(window.Resizable.Get());
        Assert.Equal(new Vector2D(1280, 720), window.Size.Get());
        Assert.Equal("Main", window.Title.Get());
        Assert.False(window.Visible.Get());
    }

    [Fact]
    public void Destroy_WhenOpen_ClosesWindowAndDestroysStores()
    {
        var window = new TestWindow();
        window.OpenForTest();

        window.Destroy();

        Assert.Equal(1, window.CloseCalls);
        Assert.Throws<DestroyedObjectException>(() => window.OpenForTest());
    }

    [Fact]
    public void Open_IsIdempotentAndSetsOpenedState()
    {
        var window = new TestWindow();

        window.OpenForTest();
        window.OpenForTest();

        Assert.True(window.Opened.Get());
        Assert.Equal(1, window.OpenCalls);
    }

    [Fact]
    public void Process_WhenDestroyed_Throws()
    {
        var window = new TestWindow();
        window.Destroy();

        Assert.Throws<DestroyedObjectException>(window.ProcessForTest);
    }

    [Fact]
    public void Stores_InvokePlatformCallbacksWhenChanged()
    {
        var window = new TestWindow();

        window.Mode.Set(WindowMode.Fullscreen);
        window.Position.Set(new Vector2D(10, 20));
        window.Resizable.Set(false);
        window.Size.Set(new Vector2D(1024, 768));
        window.Title.Set("Updated");
        window.Visible.Set(false);

        Assert.Equal(WindowMode.Fullscreen, window.LastMode);
        Assert.Equal(new Vector2D(10, 20), window.LastPosition);
        Assert.False(window.LastResizable);
        Assert.Equal(new Vector2D(1024, 768), window.LastSize);
        Assert.Equal("Updated", window.LastTitle);
        Assert.False(window.LastVisible);
    }

    private sealed class TestWindow(WindowOptions? options = null) : Window(options)
    {
        public int CloseCalls { get; private set; }

        public bool FocusedForTest
        {
            set => _focused.Set(value);
        }

        public WindowMode LastMode { get; private set; }
        public Vector2D LastPosition { get; private set; }
        public bool LastResizable { get; private set; }
        public Vector2D LastSize { get; private set; }
        public string LastTitle { get; private set; } = string.Empty;
        public bool LastVisible { get; private set; }
        public int OpenCalls { get; private set; }

        protected override void OnClose() => CloseCalls++;

        protected override void OnModeChanged(WindowMode mode) => LastMode = mode;

        protected override void OnMove(Vector2D position) => LastPosition = position;

        protected override void OnOpen() => OpenCalls++;

        protected override void OnProcess()
        {
            ThrowIfDestroyed();
        }

        protected override void OnResizableChanged(bool resizable) => LastResizable = resizable;

        protected override void OnResize(Vector2D size) => LastSize = size;

        protected override void OnTitleChanged(string title) => LastTitle = title;

        protected override void OnVisibilityChanged(bool visible) => LastVisible = visible;

        public void CloseForTest() => Close();

        public void OpenForTest() => Open();

        public void ProcessForTest() => Process();
    }
}
