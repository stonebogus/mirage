using Mirage.Common.Events;
using Mirage.Common.Lifecycle;
using Mirage.Math.Vectors;

namespace Mirage.Windowing.Windows;

/// <summary>
/// Defines the initial configuration of a <see cref="Window"/>.
/// </summary>
public sealed class WindowOptions
{
    /// <summary>
    /// Gets the identifier used to distinguish the window within the windowing module.
    /// </summary>
    public string Identifier { get; init; } = "window";

    /// <summary>
    /// Gets the initial display mode of the window.
    /// </summary>
    public WindowMode Mode { get; init; } = WindowMode.Normal;

    /// <summary>
    /// Gets the initial position of the window's client area, in screen coordinates.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the platform chooses the initial position.
    /// Some platforms may ignore an explicitly supplied position.
    /// </remarks>
    public Vector2D? Position { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user can resize the window.
    /// </summary>
    public bool Resizable { get; init; } = true;

    /// <summary>
    /// Gets the initial size of the window's client area.
    /// </summary>
    public Vector2D Size { get; init; } = new(800, 600);

    /// <summary>
    /// Gets the initial title of the window.
    /// </summary>
    public string Title { get; init; } = "Mirage";

    /// <summary>
    /// Gets a value indicating whether the window is initially visible.
    /// </summary>
    public bool Visible { get; init; } = true;
}

/// <summary>
/// Specifies the display mode of a window.
/// </summary>
public enum WindowMode
{
    /// <summary>
    /// The window is displayed normally.
    /// </summary>
    Normal,

    /// <summary>
    /// The window is minimized.
    /// </summary>
    Minimized,

    /// <summary>
    /// The window is maximized within the available desktop area.
    /// </summary>
    Maximized,

    /// <summary>
    /// The window occupies an entire display.
    /// </summary>
    Fullscreen,
}

/// <summary>
/// Represents a platform-agnostic application window.
/// </summary>
/// <remarks>
/// Writable stores describe requested window state and are also updated when the
/// underlying native window changes. <see cref="Focused"/> and <see cref="Opened"/>
/// are read-only because their values are controlled by the windowing backend.
/// </remarks>
public abstract class Window : Destroyable
{
    /// <summary>
    /// Stores whether the native window currently has input focus.
    /// </summary>
    protected readonly Store<bool> _focused = new(false);

    /// <summary>
    /// Stores whether the native window is currently open.
    /// </summary>
    protected readonly Store<bool> _opened = new(false);

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    /// <param name="options">
    /// The initial window configuration, or <see langword="null"/> to use the defaults.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the configured identifier is empty or consists only of whitespace.
    /// </exception>
    protected Window(WindowOptions? options = null)
    {
        options ??= new WindowOptions();

        if (string.IsNullOrWhiteSpace(options.Identifier))
            throw new ArgumentException("Window identifier cannot be empty.", nameof(options));

        Identifier = options.Identifier;
        HasInitialPosition = options.Position.HasValue;

        Focused = _focused;
        Opened = _opened;

        Mode = new Store<WindowMode>(options.Mode);
        Position = new Store<Vector2D>(options.Position ?? new Vector2D());
        Resizable = new Store<bool>(options.Resizable);
        Size = new Store<Vector2D>(options.Size);
        Title = new Store<string>(options.Title);
        Visible = new Store<bool>(options.Visible);

        Mode.Connect(OnModeChanged, true);
        Position.Connect(OnMove, true);
        Resizable.Connect(OnResizableChanged, true);
        Size.Connect(OnResize, true);
        Title.Connect(OnTitleChanged, true);
        Visible.Connect(OnVisibilityChanged, true);
    }

    /// <summary>
    /// Gets a value indicating whether an initial position was explicitly configured.
    /// </summary>
    protected bool HasInitialPosition { get; }

    /// <summary>
    /// Gets a read-only store indicating whether the window currently has input focus.
    /// </summary>
    public IReadOnlyStore<bool> Focused { get; }

    /// <summary>
    /// Gets the identifier of the window.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the store that controls and reports the window's display mode.
    /// </summary>
    public Store<WindowMode> Mode { get; }

    /// <summary>
    /// Gets a read-only store indicating whether the native window is open.
    /// </summary>
    public IReadOnlyStore<bool> Opened { get; }

    /// <summary>
    /// Gets the store that controls and reports the position of the window's client area.
    /// </summary>
    public Store<Vector2D> Position { get; }

    /// <summary>
    /// Gets the store that controls and reports whether the window is resizable.
    /// </summary>
    public Store<bool> Resizable { get; }

    /// <summary>
    /// Gets the store that controls and reports the size of the window's client area.
    /// </summary>
    public Store<Vector2D> Size { get; }

    /// <summary>
    /// Gets the store that controls and reports the title of the window.
    /// </summary>
    public Store<string> Title { get; }

    /// <summary>
    /// Gets the store that controls and reports whether the window is visible.
    /// </summary>
    public Store<bool> Visible { get; }

    /// <summary>
    /// Closes and releases the platform-specific window.
    /// </summary>
    /// <remarks>
    /// Implementations should make repeated calls safe.
    /// </remarks>
    protected virtual void OnClose() { }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        if (_opened.Get())
            OnClose();

        _focused.Destroy();
        _opened.Destroy();

        Mode.Destroy();
        Position.Destroy();
        Resizable.Destroy();
        Size.Destroy();
        Title.Destroy();
        Visible.Destroy();
    }

    /// <summary>
    /// Applies a display mode change to the platform-specific window.
    /// </summary>
    /// <param name="mode">The requested display mode.</param>
    protected virtual void OnModeChanged(WindowMode mode) { }

    /// <summary>
    /// Applies a position change to the platform-specific window.
    /// </summary>
    /// <param name="position">The requested client-area position.</param>
    protected virtual void OnMove(Vector2D position) { }

    /// <summary>
    /// Creates and opens the platform-specific window.
    /// </summary>
    protected virtual void OnOpen() { }

    /// <summary>
    /// Applies a resizability change to the platform-specific window.
    /// </summary>
    /// <param name="resizable">
    /// <see langword="true"/> to allow user resizing; otherwise, <see langword="false"/>.
    /// </param>
    protected virtual void OnResizableChanged(bool resizable) { }

    /// <summary>
    /// Applies a client-area size change to the platform-specific window.
    /// </summary>
    /// <param name="size">The requested client-area size.</param>
    protected virtual void OnResize(Vector2D size) { }

    /// <summary>
    /// Applies a title change to the platform-specific window.
    /// </summary>
    /// <param name="title">The requested window title.</param>
    protected virtual void OnTitleChanged(string title) { }

    /// <summary>
    /// Applies a visibility change to the platform-specific window.
    /// </summary>
    /// <param name="visible">
    /// <see langword="true"/> to show the window; otherwise, <see langword="false"/>.
    /// </param>
    protected virtual void OnVisibilityChanged(bool visible) { }

    /// <summary>
    /// Closes the window if it is open.
    /// </summary>
    /// <exception cref="DestroyedObjectException">
    /// Thrown when the window has already been destroyed.
    /// </exception>
    public void Close()
    {
        ThrowIfDestroyed();

        if (!_opened.Get())
            return;

        OnClose();

        _focused.Set(false);
        _opened.Set(false);
        Visible.Set(false);
    }

    /// <summary>
    /// Opens the window if it is not already open.
    /// </summary>
    /// <exception cref="DestroyedObjectException">
    /// Thrown when the window has already been destroyed.
    /// </exception>
    public void Open()
    {
        ThrowIfDestroyed();

        if (_opened.Get())
            return;

        OnOpen();

        _opened.Set(true);
    }

    /// <summary>
    /// Processes pending platform events and synchronizes native window state.
    /// </summary>
    /// <exception cref="DestroyedObjectException">
    /// Thrown when the window has already been destroyed.
    /// </exception>
    public abstract void Process();
}
