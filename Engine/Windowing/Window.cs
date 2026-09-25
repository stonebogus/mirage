using System.Numerics;
using Mirage.Common.Events;
using Mirage.Common.Lifecycle;
using SDL3;

namespace Mirage.Windowing;

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
    public Vector2? Position { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user can resize the window.
    /// </summary>
    public bool Resizable { get; init; } = true;

    /// <summary>
    /// Gets the initial size of the window's client area.
    /// </summary>
    public Vector2 Size { get; init; } = new(800, 600);

    /// <summary>
    /// Gets the initial title of the window.
    /// </summary>
    public string Title { get; init; } = "Mirage";

    /// <summary>
    /// Gets a value indicating whether vertical synchronization is initially enabled.
    /// </summary>
    public bool VSync { get; init; }

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
/// Represents the application's SDL3 window.
/// </summary>
/// <remarks>
/// Writable stores describe requested window state and are also updated when the
/// underlying SDL3 window changes. <see cref="Focused"/> and <see cref="Opened"/>
/// are read-only because their values are controlled by SDL3.
/// </remarks>
public partial class Window : Destroyable
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
    public Window(WindowOptions? options = null)
    {
        options ??= new WindowOptions();

        if (string.IsNullOrWhiteSpace(options.Identifier))
            throw new ArgumentException("Window identifier cannot be empty.", nameof(options));

        Identifier = options.Identifier;
        HasInitialPosition = options.Position.HasValue;

        Focused = _focused;
        Opened = _opened;

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
    public Store<Vector2> Position { get; }

    /// <summary>
    /// Gets the store that controls and reports whether the window is resizable.
    /// </summary>
    public Store<bool> Resizable { get; }

    /// <summary>
    /// Gets the store that controls and reports the size of the window's client area.
    /// </summary>
    public Store<Vector2> Size { get; }

    /// <summary>
    /// Gets the store that controls and reports the title of the window.
    /// </summary>
    public Store<string> Title { get; }

    /// <summary>
    /// Gets the store that controls and reports whether vertical synchronization is enabled.
    /// </summary>
    public Store<bool> VSync { get; }

    /// <summary>
    /// Gets the store that controls and reports whether the window is visible.
    /// </summary>
    public Store<bool> Visible { get; }

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
        VSync.Destroy();
        Visible.Destroy();
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

        if (!_opened.Get())
            return;

        OnClose();

        _focused.Set(false);
        _opened.Set(false);
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
    public void Process()
    {
        ThrowIfDestroyed();
        if (_opened.Get())
            OnProcess();
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
    private IntPtr _nativeWindow;
    private IntPtr _gpuDevice;

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
    /// Used by the SDL3 GPU renderer.
    /// Do not destroy or mutate the window through this handle.
    /// </remarks>
    public IntPtr Native => _nativeWindow;

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

        if (
            type != SDL.EventType.Quit
            && (type != SDL.EventType.WindowCloseRequested || !BelongsToWindow(@event, windowId))
        )
            return true;
        Close();
        return false;
    }

    private void Publish<TValue>(IntPtr window, Store<TValue> store, Func<TValue> read)
    {
        if (_nativeWindow != window || Destroyed)
            return;

        var value = read();

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

    private void SynchronizeState(IntPtr window)
    {
        var flags = SDL.GetWindowFlags(window);

        Publish(window, Mode, () => FromSdlFlags(flags));

        if (_nativeWindow != window)
            return;

        Publish(
            window,
            Position,
            () =>
            {
                Ensure(
                    SDL.GetWindowPosition(window, out var x, out var y),
                    "Getting window position"
                );

                return new Vector2(x, y);
            }
        );

        Publish(
            window,
            Size,
            () =>
            {
                Ensure(
                    SDL.GetWindowSize(window, out var width, out var height),
                    "Getting window size"
                );

                return new Vector2(width, height);
            }
        );

        Publish(window, Resizable, () => flags.HasFlag(SDL.WindowFlags.Resizable));

        Publish(window, Visible, () => !flags.HasFlag(SDL.WindowFlags.Hidden));

        Publish(window, _focused, () => flags.HasFlag(SDL.WindowFlags.InputFocus));

        Publish(window, Title, () => SDL.GetWindowTitle(window));
    }

    /// <summary>
    /// Associates an SDL GPU device with this window so its VSync store can
    /// control the GPU swapchain present mode.
    /// </summary>
    /// <param name="device">The claimed SDL GPU device.</param>
    public void AttachGPUDevice(IntPtr device)
    {
        _gpuDevice = device;
        ApplyVSync();
    }

    /// <summary>
    /// Removes the SDL GPU device associated with this window.
    /// </summary>
    /// <param name="device">The previously associated SDL GPU device.</param>
    public void DetachGPUDevice(IntPtr device)
    {
        if (_gpuDevice == device)
            _gpuDevice = IntPtr.Zero;
    }

    private void ApplyVSync()
    {
        if (_gpuDevice == IntPtr.Zero || _nativeWindow == IntPtr.Zero)
            return;

        var presentMode = VSync.Get() ? SDL.GPUPresentMode.VSync : SDL.GPUPresentMode.Immediate;

        if (!SDL.WindowSupportsGPUPresentMode(_gpuDevice, _nativeWindow, presentMode))
        {
            throw new InvalidOperationException(
                $"SDL GPU present mode '{presentMode}' is unsupported: {SDL.GetError()}"
            );
        }

        Ensure(
            SDL.SetGPUSwapchainParameters(
                _gpuDevice,
                _nativeWindow,
                SDL.GPUSwapchainComposition.SDR,
                presentMode
            ),
            "Changing GPU swapchain VSync"
        );
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

    /// <inheritdoc />
    protected virtual void OnClose()
    {
        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

        _nativeWindow = IntPtr.Zero;

        SDL.DestroyWindow(window);
        SDL3VideoRuntime.Release();
    }

    /// <inheritdoc />
    protected virtual void OnModeChanged(WindowMode mode)
    {
        if (!ShouldApply(Mode, mode))
            return;

        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

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

    /// <inheritdoc />
    protected virtual void OnMove(Vector2 position)
    {
        if (!ShouldApply(Position, position))
            return;

        var nativePosition = ToNativePosition(position);
        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

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

    /// <inheritdoc />
    protected virtual void OnOpen()
    {
        if (_nativeWindow != IntPtr.Zero)
            return;

        var requestedMode = Mode.Get();
        var requestedSize = ToNativeSize(Size.Get());

        var flags = ToSdlFlags(requestedMode, Resizable.Get(), Visible.Get());

        SDL3VideoRuntime.Acquire();

        var window = SDL.CreateWindow(
            Title.Get(),
            requestedSize.Width,
            requestedSize.Height,
            flags
        );

        if (window == IntPtr.Zero)
        {
            SDL3VideoRuntime.Release();

            throw new InvalidOperationException($"Creating SDL window failed: {SDL.GetError()}");
        }

        _nativeWindow = window;

        try
        {
            if (HasInitialPosition && requestedMode == WindowMode.Normal)
            {
                var position = ToNativePosition(Position.Get());

                Ensure(
                    SDL.SetWindowPosition(window, position.X, position.Y),
                    "Setting initial window position"
                );
            }

            SynchronizeState(window);
        }
        catch
        {
            _nativeWindow = IntPtr.Zero;

            SDL.DestroyWindow(window);
            SDL3VideoRuntime.Release();

            throw;
        }
    }

    /// <inheritdoc />
    protected virtual void OnProcess()
    {
        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

        var windowId = SDL.GetWindowID(window);

        if (windowId == 0)
        {
            throw new InvalidOperationException(
                $"Getting SDL window identifier failed: {SDL.GetError()}"
            );
        }

        while (SDL.PollEvent(out var @event))
        {
            if (!ProcessEvent(@event, windowId))
                return;
        }

        if (_nativeWindow != window || Destroyed)
            return;

        SynchronizeState(window);
    }

    /// <inheritdoc />
    protected virtual void OnResizableChanged(bool resizable)
    {
        if (!ShouldApply(Resizable, resizable))
            return;

        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

        var flags = SDL.GetWindowFlags(window);
        var current = flags.HasFlag(SDL.WindowFlags.Resizable);

        if (current == resizable)
            return;

        Ensure(SDL.SetWindowResizable(window, resizable), "Changing window resizability");
    }

    /// <inheritdoc />
    protected virtual void OnResize(Vector2 size)
    {
        if (!ShouldApply(Size, size))
            return;

        var nativeSize = ToNativeSize(size);
        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

        var flags = SDL.GetWindowFlags(window);

        if (flags.HasFlag(SDL.WindowFlags.Fullscreen))
            return;

        Ensure(
            SDL.GetWindowSize(window, out var currentWidth, out var currentHeight),
            "Getting window size"
        );

        if (currentWidth == nativeSize.Width && currentHeight == nativeSize.Height)
        {
            return;
        }

        Ensure(SDL.SetWindowSize(window, nativeSize.Width, nativeSize.Height), "Resizing window");
    }

    /// <inheritdoc />
    protected virtual void OnTitleChanged(string title)
    {
        if (!ShouldApply(Title, title))
            return;

        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

        if (SDL.GetWindowTitle(window) == title)
            return;

        Ensure(SDL.SetWindowTitle(window, title), "Changing window title");
    }

    /// <inheritdoc />
    protected virtual void OnVSyncChanged(bool vsync)
    {
        if (!ShouldApply(VSync, vsync))
            return;

        ApplyVSync();
    }

    /// <inheritdoc />
    protected virtual void OnVisibilityChanged(bool visible)
    {
        if (!ShouldApply(Visible, visible))
            return;

        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

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

/// <summary>
/// Manages shared ownership of SDL's video subsystem.
/// </summary>
internal static class SDL3VideoRuntime
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
