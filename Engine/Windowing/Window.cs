using System.Diagnostics;
using System.Numerics;
using Mirage.Common.Collections;
using Mirage.Common.Events;
using Mirage.Common.Interfaces;
using Mirage.Common.Lifecycle;
using Mirage.Common.Primitives;
using Mirage.Scheduling;
using Mirage.Scheduling.Interfaces;
using SDL3;

namespace Mirage.Windowing;

/// <summary>
/// Defines the initial configuration of a <see cref="Window"/>.
/// </summary>
public class WindowOptions : IIdentifiable<string>
{
    private static readonly string[] DefaultTitleMessages =
    [
        "Reality is loading…",
        "Somewhere between pixels and possibility.",
        "If lost, check the scene tree.",
        "The window is not the whole world.",
        "Objects may appear more stable than they are.",
        "Rendering your imagination.",
        "This frame took the scenic route.",
        "No mirage is final.",
        "Is this window real, or just well rendered?",
        "The desert is rendering. Please wait.",
        "A little illusion, a lot of C#.",
        "Look twice. It may be a node.",
        "The horizon is outside the client area.",
        "Reality has entered fullscreen mode.",
        "The update loop says hello.",
        "Somewhere, a frame is being skipped.",
        "It works on this machine.",
        "The bug is probably in another module.",
        "Compiling the laws of reality.",
        "One more feature, then we ship.",
        "Have you tried restarting the scene?",
        "The window is open. The TODO list is not.",
        "There are no bugs, only unexpected features.",
        "This title was selected at runtime.",
        "Your pixels are in another window.",
        "Please wait while the developer finds the semicolon.",
        "A wild NullReferenceException appeared!",
        "This frame is brought to you by delta time.",
        "The scheduler is doing its best.",
        "Somewhere, C# is compiling.",
        "Warning: reality may contain experimental features.",
        "Drawing outside the lines since this frame.",
        "A new node has entered the scene.",
        "The scene tree has a nice view from here.",
        "The pixels are all present and accounted for.",
        "A window into another coordinate system.",
        "Presenting one frame at a time.",
        "The render target knows what you did.",
        "The viewport is looking back.",
        "Everything is relative to the parent.",
        "Your frame has been queued for rendering.",
        "This isn't the frame you're looking for.",
        "It's dangerous to render alone.",
        "A wild window appeared!",
        "The cake is probably in the assets folder.",
        "A companion cube was added to the scene.",
        "One does not simply skip the update loop.",
        "May your frame time be ever in your favor.",
        "The cake is a compile-time constant.",
        "A block of reality has been placed.",
        "Hallownest would make a very large scene graph.",
        "The window has no map. Check the scene tree.",
        "The simulation is starting to feel suspicious.",
        "There is probably a spoon somewhere.",
        "The pixels must flow.",
    ];

    /// <summary>
    /// Gets the cursors to register initially. The default is an empty sequence.
    /// </summary>
    /// <remarks>The window stores these references but does not own the cursors.</remarks>
    public IEnumerable<Cursor> Cursors = [];

    /// <summary>
    /// Gets the initial display mode. The default is <see cref="WindowMode.Normal"/>.
    /// </summary>
    public WindowMode Mode { get; init; } = WindowMode.Normal;

    /// <summary>
    /// Gets the initial client-area position in screen coordinates.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the platform chooses the initial position.
    /// Some platforms may ignore an explicitly supplied position.
    /// </remarks>
    public Vector2? Position { get; init; }

    /// <summary>
    /// Gets whether the user can resize the window. The default is <see langword="true"/>.
    /// </summary>
    public bool Resizable { get; init; } = true;

    /// <summary>
    /// Gets the initial client-area size in pixels. The default is <c>(800, 600)</c>.
    /// </summary>
    public Vector2 Size { get; init; } = new(800, 600);

    /// <summary>
    /// Gets the initial title of the window.
    /// </summary>
    /// <remarks>
    /// The default is <c>"Mirage"</c> followed by a randomly selected message.
    /// </remarks>
    public string Title { get; init; } =
        $"Mirage - {DefaultTitleMessages[Random.Shared.Next(DefaultTitleMessages.Length)]}";

    /// <summary>
    /// Gets whether vertical synchronization is initially enabled. The default is <see langword="false"/>.
    /// </summary>
    public bool VSync { get; init; }

    /// <summary>
    /// Gets whether the window is initially visible. The default is <see langword="true"/>.
    /// </summary>
    public bool Visible { get; init; } = true;

    /// <inheritdoc />
    /// <remarks>The default is <c>"window"</c>.</remarks>
    public string Identifier { get; init; } = "window";
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
/// Represents the application's SDL3 window.
/// </summary>
/// <remarks>
/// Writable stores describe requested window state and are also updated when the
/// underlying SDL3 window changes. <see cref="Focused"/> and <see cref="Opened"/>
/// are read-only because their values are controlled by SDL3.
/// Registered cursors are borrowed and may be shared between windows. Their caller owns
/// and destroys them after all windows have stopped using them. The window owns its stores
/// and cursor collection, but not the selected or registered cursor objects.
/// </remarks>
public partial class Window : Destroyable, IUpdatable, IIdentifiable<string>
{
    private readonly List<SDL.Event> _frameEvents = [];
    private readonly Store<bool> _opened = new(false);
    private readonly Signal<Unit> _closing = new();
    private bool _deferVisibilityUntilFirstFrame;
    private bool _firstFramePresented;

    /// <summary>
    /// Gets a value indicating whether an initial position was explicitly configured.
    /// </summary>
    protected readonly bool HasInitialPosition;

    /// <summary>
    /// Stores whether the native window currently has input focus.
    /// </summary>
    protected readonly Store<bool> _focused = new(false);

    /// <summary>
    /// Gets the cursor value selected for this window, or <see langword="null"/> when none is selected.
    /// </summary>
    public readonly Store<Cursor?> Cursor;

    /// <summary>Gets the event fired before the native window is closed.</summary>
    /// <remarks>Borrowers release window-bound native resources here while the handle is valid.</remarks>
    public IReadOnlyEvent<Unit> OnClosing => _closing;

    /// <summary>
    /// Gets the identifiable set of registered cursors.
    /// </summary>
    public readonly IdentifiableSet<string, Cursor> Cursors = [];

    /// <summary>
    /// Gets a read-only store indicating whether the window currently has input focus.
    /// </summary>
    public readonly IReadOnlyStore<bool> Focused;

    /// <summary>
    /// Gets the store that controls and reports the window's display mode.
    /// </summary>
    public readonly Store<WindowMode> Mode;

    /// <summary>
    /// Gets a read-only store indicating whether the native window is open.
    /// </summary>
    public readonly IReadOnlyStore<bool> Opened;

    /// <summary>
    /// Gets the store that controls and reports the position of the window's client area.
    /// The position is expressed in screen coordinates.
    /// </summary>
    public readonly Store<Vector2> Position;

    /// <summary>
    /// Gets the store that controls and reports whether the window is resizable.
    /// </summary>
    public readonly Store<bool> Resizable;

    /// <summary>
    /// Gets the store that controls and reports the size of the window's client area.
    /// The size is expressed in pixels.
    /// </summary>
    public readonly Store<Vector2> Size;

    /// <summary>
    /// Gets the store that controls and reports the title of the window.
    /// </summary>
    /// <remarks>
    /// Store changes are immediate. While open, native title changes are coalesced
    /// during window updates, at most once every 250 milliseconds of real elapsed time.
    /// Creation uses the current title immediately.
    /// </remarks>
    public readonly Store<string> Title;

    /// <summary>
    /// Gets the store that controls and reports whether vertical synchronization is enabled.
    /// </summary>
    public readonly Store<bool> VSync;

    /// <summary>
    /// Gets the store that controls and reports whether the window is visible.
    /// </summary>
    public readonly Store<bool> Visible;

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    /// <param name="options">
    /// The initial window configuration, or <see langword="null"/> to use the defaults.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the configured identifier is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the initial cursor collection contains duplicate identifiers.
    /// </exception>
    public Window(WindowOptions? options = null)
    {
        options ??= new WindowOptions();

        if (string.IsNullOrWhiteSpace(options.Identifier))
            throw new ArgumentException("Window identifier cannot be empty.", nameof(options));

        Identifier = options.Identifier;
        HasInitialPosition = options.Position.HasValue;

        Focused = _focused;
        Opened = _opened;

        foreach (var cursor in options.Cursors)
        {
            Cursors.Add(cursor);
        }
        Cursor = new Store<Cursor?>(Cursors.FirstOrDefault());

        Mode = new Store<WindowMode>(options.Mode);
        Position = new Store<Vector2>(options.Position ?? new Vector2());
        Resizable = new Store<bool>(options.Resizable);
        Size = new Store<Vector2>(options.Size);
        Title = new Store<string>(options.Title);
        VSync = new Store<bool>(options.VSync);
        Visible = new Store<bool>(options.Visible);

        Mode.Connect(OnModeChanged, true);
        Position.Connect(OnMove, true);
        Resizable.Connect(OnResizableChanged, true);
        Size.Connect(OnResize, true);
        Title.Connect(OnTitleChanged, true);
        VSync.Connect(OnVSyncChanged, true);
        Visible.Connect(OnVisibilityChanged, true);
    }

    /// <summary>
    /// Gets the SDL events polled during the most recent window update.
    /// </summary>
    /// <remarks>The list is replaced on each update and can contain events for other windows.</remarks>
    public IReadOnlyList<SDL.Event> FrameEvents => _frameEvents;

    /// <inheritdoc />
    public string Identifier { get; }

    /// <inheritdoc />
    public virtual void Update(UpdateContext context)
    {
        ThrowIfDestroyed();
        if (_opened.Get())
            OnProcess();
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        if (Native != IntPtr.Zero)
            OnClose();

        _focused.Destroy();
        _opened.Destroy();
        _closing.Destroy();

        Mode.Destroy();
        Position.Destroy();
        Resizable.Destroy();
        Size.Destroy();
        Title.Destroy();
        VSync.Destroy();
        Visible.Destroy();
        Cursor.Destroy();
        Cursors.Destroy();
        _frameEvents.Clear();

        base.OnDestroy();
    }

    /// <summary>
    /// Closes the window if it is open.
    /// </summary>
    /// <exception cref="DestroyedObjectException">
    /// Thrown when the window has already been destroyed.
    /// </exception>
    public void Close()
    {
        ThrowIfDestroyed();

        if (Native == IntPtr.Zero)
            return;

        OnClose();

        var generation = _nativeGeneration;
        _focused.Set(false);

        if (!Destroyed && _nativeGeneration == generation)
            _opened.Set(false);
    }

    /// <summary>
    /// Defers showing the native window until its first frame has been presented.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the window is already open.
    /// </exception>
    public void DeferVisibilityUntilFirstFrame()
    {
        ThrowIfDestroyed();

        if (_opened.Get())
            throw new InvalidOperationException(
                "Visibility must be deferred before the window is opened."
            );

        _deferVisibilityUntilFirstFrame = true;
    }

    /// <summary>
    /// Notifies the window that its renderer has presented its first frame.
    /// </summary>
    public void NotifyFirstFramePresented()
    {
        ThrowIfDestroyed();

        if (!_deferVisibilityUntilFirstFrame || _firstFramePresented)
            return;

        _firstFramePresented = true;

        if (Visible.Get())
            OnVisibilityChanged(true);
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

        if (!Destroyed && Native != IntPtr.Zero)
            _opened.Set(true);
    }
}

/// <summary>
/// Represents an SDL3-backed application window.
/// </summary>
/// <remarks>
/// Create, process, mutate and destroy this window on the main thread.
/// On Linux, X11 is selected by default; under a Wayland session this normally
/// runs through XWayland. Set <c>SDL_VIDEODRIVER</c> before SDL initialization
/// to explicitly select another video driver.
/// </remarks>
public partial class Window
{
    private static readonly TimeSpan NativeRefreshInterval = TimeSpan.FromMilliseconds(250);
    private static readonly Dictionary<uint, Window> NativeWindows = [];

    [Flags]
    private enum NativeState
    {
        None = 0,
        Flags = 1,
        Position = 2,
        Size = 4,
        All = Flags | Position | Size,
    }

    private NativeState _dirtyState;
    private uint _windowId;
    private long _nativeGeneration;
    private long _lastTitleUpdate;
    private long _lastNativePoll;
    private bool _titlePending;
    private object? _publishingStore;
    private object? _publishingValue;

    static Window()
    {
        if (OperatingSystem.IsLinux())
            SDL.SetHint("SDL_VIDEO_DRIVER", "x11");
    }

    /// <summary>
    /// Gets the native SDL window while it is open.
    /// </summary>
    /// <remarks>
    /// The handle is owned by this window and is <see cref="IntPtr.Zero"/> while closed.
    /// Do not destroy or mutate the window through this borrowed handle.
    /// </remarks>
    public IntPtr Native { get; private set; }

    private bool BelongsToWindow(in SDL.Event @event, uint windowId)
    {
        return @event.Window.WindowID == windowId;
    }

    private static void Ensure(bool result, string operation)
    {
        if (!result)
            throw new InvalidOperationException($"{operation} failed: {SDL.GetError()}");
    }

    private static WindowMode FromSdlFlags(SDL.WindowFlags flags)
    {
        if (flags.HasFlag(SDL.WindowFlags.Fullscreen))
            return WindowMode.Fullscreen;

        if (flags.HasFlag(SDL.WindowFlags.Minimized))
            return WindowMode.Minimized;

        if (flags.HasFlag(SDL.WindowFlags.Maximized))
            return WindowMode.Maximized;

        return WindowMode.Normal;
    }

    private bool ProcessEvent(in SDL.Event @event, uint windowId)
    {
        var type = (SDL.EventType)@event.Type;

        var dirty = type switch
        {
            SDL.EventType.WindowMoved => NativeState.Position,
            SDL.EventType.WindowResized or SDL.EventType.WindowPixelSizeChanged => NativeState.Size,
            SDL.EventType.WindowShown
            or SDL.EventType.WindowHidden
            or SDL.EventType.WindowFocusGained
            or SDL.EventType.WindowFocusLost
            or SDL.EventType.WindowMinimized
            or SDL.EventType.WindowMaximized
            or SDL.EventType.WindowRestored
            or SDL.EventType.WindowEnterFullscreen
            or SDL.EventType.WindowLeaveFullscreen => NativeState.Flags,
            _ => NativeState.None,
        };

        // SDL's queue is shared: another Window may consume our state events.
        if (
            dirty != NativeState.None
            && NativeWindows.TryGetValue(@event.Window.WindowID, out var target)
        )
            target._dirtyState |= dirty;

        if (
            type != SDL.EventType.Quit
            && (type != SDL.EventType.WindowCloseRequested || !BelongsToWindow(@event, windowId))
        )
            return true;

        Close();
        return false;
    }

    private bool IsCurrentWindow(IntPtr window, long generation)
    {
        return window != IntPtr.Zero
            && Native == window
            && _nativeGeneration == generation
            && !Destroyed;
    }

    private void Publish<TValue>(IntPtr window, long generation, Store<TValue> store, TValue value)
    {
        if (!IsCurrentWindow(window, generation))
            return;

        if (EqualityComparer<TValue>.Default.Equals(store.Get(), value))
            return;

        var previousStore = _publishingStore;
        var previousValue = _publishingValue;

        _publishingStore = store;
        _publishingValue = value;

        try
        {
            store.Set(value);
        }
        finally
        {
            _publishingStore = previousStore;
            _publishingValue = previousValue;
        }
    }

    private bool ShouldApply<TValue>(Store<TValue> store, TValue value)
    {
        if (!EqualityComparer<TValue>.Default.Equals(store.Get(), value))
            return false;

        return !(
            ReferenceEquals(_publishingStore, store)
            && EqualityComparer<TValue>.Default.Equals((TValue)_publishingValue!, value)
        );
    }

    private void SynchronizeState(IntPtr window, long generation, bool poll)
    {
        if (!IsCurrentWindow(window, generation))
            return;

        var dirty = _dirtyState;
        _dirtyState = NativeState.None;

        if ((dirty & NativeState.Flags) != 0 || poll)
        {
            var flags = SDL.GetWindowFlags(window);

            if ((dirty & NativeState.Flags) != 0)
            {
                Publish(window, generation, Mode, FromSdlFlags(flags));

                if (!_deferVisibilityUntilFirstFrame || _firstFramePresented)
                    Publish(window, generation, Visible, !flags.HasFlag(SDL.WindowFlags.Hidden));

                Publish(window, generation, _focused, flags.HasFlag(SDL.WindowFlags.InputFocus));
            }

            Publish(window, generation, Resizable, flags.HasFlag(SDL.WindowFlags.Resizable));
        }

        if (!IsCurrentWindow(window, generation))
            return;

        if ((dirty & NativeState.Position) != 0)
        {
            Ensure(SDL.GetWindowPosition(window, out var x, out var y), "Getting window position");
            Publish(window, generation, Position, new Vector2(x, y));
        }

        if (!IsCurrentWindow(window, generation))
            return;

        if ((dirty & NativeState.Size) != 0)
        {
            Ensure(SDL.GetWindowSize(window, out var width, out var height), "Getting window size");
            Publish(window, generation, Size, new Vector2(width, height));
        }

        // SDL has no title or resizability event. Never overwrite a pending title request.
        if (poll && IsCurrentWindow(window, generation) && !_titlePending)
            Publish(window, generation, Title, SDL.GetWindowTitle(window));
    }

    private void FlushTitle(IntPtr window, long now)
    {
        if (
            !_titlePending
            || Stopwatch.GetElapsedTime(_lastTitleUpdate, now) < NativeRefreshInterval
        )
            return;

        var title = Title.Get();

        if (SDL.GetWindowTitle(window) != title)
            Ensure(SDL.SetWindowTitle(window, title), "Changing window title");

        _titlePending = false;
        _lastTitleUpdate = Stopwatch.GetTimestamp();
    }

    private static (int X, int Y) ToNativePosition(Vector2 position)
    {
        if (
            !double.IsFinite(position.X)
            || !double.IsFinite(position.Y)
            || position.X < int.MinValue
            || position.X > int.MaxValue
            || position.Y < int.MinValue
            || position.Y > int.MaxValue
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Window coordinates must be finite and fit in a 32-bit integer."
            );
        }

        return ((int)position.X, (int)position.Y);
    }

    private static (int Width, int Height) ToNativeSize(Vector2 size)
    {
        if (
            !double.IsFinite(size.X)
            || !double.IsFinite(size.Y)
            || size.X < 1
            || size.Y < 1
            || size.X > int.MaxValue
            || size.Y > int.MaxValue
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Window dimensions must be finite and between 1 and int.MaxValue."
            );
        }

        return ((int)size.X, (int)size.Y);
    }

    private static SDL.WindowFlags ToSdlFlags(WindowMode mode, bool resizable, bool visible)
    {
        var flags = (SDL.WindowFlags)0;

        if (resizable)
            flags |= SDL.WindowFlags.Resizable;

        if (!visible)
            flags |= SDL.WindowFlags.Hidden;

        flags |= mode switch
        {
            WindowMode.Normal => 0,
            WindowMode.Minimized => SDL.WindowFlags.Minimized,
            WindowMode.Maximized => SDL.WindowFlags.Maximized,
            WindowMode.Fullscreen => SDL.WindowFlags.Fullscreen,

            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown window mode."),
        };

        return flags;
    }

    /// <summary>
    /// Destroys the native window and releases the SDL video subsystem lease.
    /// </summary>
    protected virtual void OnClose()
    {
        var window = Native;

        if (window == IntPtr.Zero)
            return;

        _closing.Fire(Unit.Value);
        NativeWindows.Remove(_windowId);

        Native = IntPtr.Zero;
        _windowId = 0;
        _nativeGeneration++;
        _titlePending = false;
        _dirtyState = NativeState.None;

        SDL.DestroyWindow(window);
        VideoRuntime.Release();
    }

    /// <summary>
    /// Applies the requested display mode to the native window.
    /// </summary>
    /// <param name="mode">The requested display mode.</param>
    protected virtual void OnModeChanged(WindowMode mode)
    {
        if (!ShouldApply(Mode, mode))
            return;

        var window = Native;

        if (window == IntPtr.Zero)
            return;

        _dirtyState |= NativeState.Flags;

        var flags = SDL.GetWindowFlags(window);

        switch (mode)
        {
            case WindowMode.Normal:
                if (flags.HasFlag(SDL.WindowFlags.Fullscreen))
                {
                    Ensure(SDL.SetWindowFullscreen(window, false), "Leaving fullscreen");
                }

                Ensure(SDL.RestoreWindow(window), "Restoring window");
                break;

            case WindowMode.Minimized:
                if (flags.HasFlag(SDL.WindowFlags.Fullscreen))
                {
                    Ensure(SDL.SetWindowFullscreen(window, false), "Leaving fullscreen");
                }

                Ensure(SDL.MinimizeWindow(window), "Minimizing window");
                break;

            case WindowMode.Maximized:
                if (flags.HasFlag(SDL.WindowFlags.Fullscreen))
                {
                    Ensure(SDL.SetWindowFullscreen(window, false), "Leaving fullscreen");
                }

                Ensure(SDL.MaximizeWindow(window), "Maximizing window");
                break;

            case WindowMode.Fullscreen:
                if (!flags.HasFlag(SDL.WindowFlags.Fullscreen))
                {
                    Ensure(SDL.SetWindowFullscreen(window, true), "Entering fullscreen");
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown window mode.");
        }
    }

    /// <summary>
    /// Moves the native window to the requested screen position.
    /// </summary>
    /// <param name="position">The requested position in screen coordinates.</param>
    protected virtual void OnMove(Vector2 position)
    {
        if (!ShouldApply(Position, position))
            return;

        var nativePosition = ToNativePosition(position);
        var window = Native;

        if (window == IntPtr.Zero)
            return;

        _dirtyState |= NativeState.Position;

        var flags = SDL.GetWindowFlags(window);

        if (flags.HasFlag(SDL.WindowFlags.Fullscreen))
            return;

        Ensure(
            SDL.GetWindowPosition(window, out var currentX, out var currentY),
            "Getting window position"
        );

        if (currentX == nativePosition.X && currentY == nativePosition.Y)
            return;

        Ensure(SDL.SetWindowPosition(window, nativePosition.X, nativePosition.Y), "Moving window");
    }

    /// <summary>
    /// Creates the native window and applies its initial configuration.
    /// </summary>
    protected virtual void OnOpen()
    {
        if (Native != IntPtr.Zero)
            return;

        _firstFramePresented = false;

        var requestedMode = Mode.Get();
        var requestedSize = ToNativeSize(Size.Get());
        var visible = Visible.Get() && !_deferVisibilityUntilFirstFrame;

        var flags = ToSdlFlags(requestedMode, Resizable.Get(), visible);

        VideoRuntime.Acquire();

        var window = SDL.CreateWindow(
            Title.Get(),
            requestedSize.Width,
            requestedSize.Height,
            flags
        );

        if (window == IntPtr.Zero)
        {
            VideoRuntime.Release();

            throw new InvalidOperationException($"Creating SDL window failed: {SDL.GetError()}");
        }

        Native = window;
        var generation = ++_nativeGeneration;
        _titlePending = false;
        _dirtyState = NativeState.All;
        _lastTitleUpdate = _lastNativePoll = Stopwatch.GetTimestamp();

        try
        {
            _windowId = SDL.GetWindowID(window);
            NativeWindows.Add(_windowId, this);

            if (HasInitialPosition && requestedMode == WindowMode.Normal)
            {
                var position = ToNativePosition(Position.Get());

                Ensure(
                    SDL.SetWindowPosition(window, position.X, position.Y),
                    "Setting initial window position"
                );
            }

            SynchronizeState(window, generation, true);
        }
        catch
        {
            if (IsCurrentWindow(window, generation))
                OnClose();

            throw;
        }
    }

    /// <summary>
    /// Pumps SDL events, synchronizes changed native state, and flushes pending title changes.
    /// Title and resizability are also polled every 250 milliseconds because SDL has no
    /// dedicated change events for them.
    /// </summary>
    protected virtual void OnProcess()
    {
        _frameEvents.Clear();

        var window = Native;
        if (window == IntPtr.Zero)
            return;

        var windowId = _windowId;
        var generation = _nativeGeneration;

        while (SDL.PollEvent(out var @event))
        {
            _frameEvents.Add(@event);

            if (!ProcessEvent(@event, windowId) || !IsCurrentWindow(window, generation))
                break;
        }

        if (!IsCurrentWindow(window, generation))
            return;

        var now = Stopwatch.GetTimestamp();
        FlushTitle(window, now);

        var poll = Stopwatch.GetElapsedTime(_lastNativePoll, now) >= NativeRefreshInterval;
        if (poll)
            _lastNativePoll = now;

        SynchronizeState(window, generation, poll);
    }

    /// <summary>
    /// Applies whether the native window can be resized.
    /// </summary>
    /// <param name="resizable">Whether resizing is allowed.</param>
    protected virtual void OnResizableChanged(bool resizable)
    {
        if (!ShouldApply(Resizable, resizable))
            return;

        var window = Native;

        if (window == IntPtr.Zero)
            return;

        _dirtyState |= NativeState.Flags;

        var flags = SDL.GetWindowFlags(window);
        var current = flags.HasFlag(SDL.WindowFlags.Resizable);

        if (current == resizable)
            return;

        Ensure(SDL.SetWindowResizable(window, resizable), "Changing window resizability");
    }

    /// <summary>
    /// Applies the requested size to the native window.
    /// </summary>
    /// <param name="size">The requested client-area size.</param>
    protected virtual void OnResize(Vector2 size)
    {
        if (!ShouldApply(Size, size))
            return;

        var (width, height) = ToNativeSize(size);
        var window = Native;

        if (window == IntPtr.Zero)
            return;

        _dirtyState |= NativeState.Size;

        var flags = SDL.GetWindowFlags(window);

        if (flags.HasFlag(SDL.WindowFlags.Fullscreen))
            return;

        Ensure(
            SDL.GetWindowSize(window, out var currentWidth, out var currentHeight),
            "Getting window size"
        );

        if (currentWidth == width && currentHeight == height)
        {
            return;
        }

        Ensure(SDL.SetWindowSize(window, width, height), "Resizing window");
    }

    /// <summary>
    /// Coalesces the requested title for a subsequent window update.
    /// </summary>
    /// <param name="title">The requested window title.</param>
    /// <remarks>Native changes occur at most once every 250 milliseconds while open.</remarks>
    protected virtual void OnTitleChanged(string title)
    {
        if (!ShouldApply(Title, title) || Native == IntPtr.Zero)
            return;

        _titlePending = true;
    }

    /// <summary>
    /// Applies a VSync change to the renderer attached to this window.
    /// </summary>
    /// <param name="vsync">Whether to synchronize frame presentation with the display.</param>
    /// <remarks>
    /// If the renderer has not started, it reads the current value of
    /// <see cref="VSync"/> when it starts.
    /// </remarks>
    protected virtual void OnVSyncChanged(bool vsync)
    {
        if (!ShouldApply(VSync, vsync))
            return;

        var window = Native;

        if (window == IntPtr.Zero)
            return;

        var renderer = SDL.GetRenderer(window);

        if (renderer == IntPtr.Zero)
            return;

        Ensure(SDL.SetRenderVSync(renderer, vsync ? 1 : 0), "Changing renderer VSync");
    }

    /// <summary>
    /// Shows or hides the native window.
    /// </summary>
    /// <param name="visible">Whether the window should be visible.</param>
    protected virtual void OnVisibilityChanged(bool visible)
    {
        if (!ShouldApply(Visible, visible))
            return;

        if (visible && _deferVisibilityUntilFirstFrame && !_firstFramePresented)
            return;

        var window = Native;

        if (window == IntPtr.Zero)
            return;

        _dirtyState |= NativeState.Flags;

        var flags = SDL.GetWindowFlags(window);
        var current = !flags.HasFlag(SDL.WindowFlags.Hidden);

        if (current == visible)
            return;

        if (visible)
            Ensure(SDL.ShowWindow(window), "Showing window");
        else
            Ensure(SDL.HideWindow(window), "Hiding window");
    }
}

internal static class VideoRuntime
{
    private static readonly Lock Gate = new();

    private static int _leases;
    private static bool _ownsVideoSubsystem;

    public static void Acquire()
    {
        lock (Gate)
        {
            if (_leases > 0)
            {
                _leases++;
                return;
            }

            var initialized = SDL.WasInit(SDL.InitFlags.Video);

            _ownsVideoSubsystem = !initialized.HasFlag(SDL.InitFlags.Video);

            if (_ownsVideoSubsystem && !SDL.InitSubSystem(SDL.InitFlags.Video))
            {
                _ownsVideoSubsystem = false;

                throw new InvalidOperationException(
                    $"Initializing SDL video failed: {SDL.GetError()}"
                );
            }

            _leases = 1;
        }
    }

    public static void Release()
    {
        lock (Gate)
        {
            if (_leases == 0)
                return;

            _leases--;

            if (_leases != 0)
                return;

            if (_ownsVideoSubsystem)
                SDL.QuitSubSystem(SDL.InitFlags.Video);

            _ownsVideoSubsystem = false;
        }
    }
}
