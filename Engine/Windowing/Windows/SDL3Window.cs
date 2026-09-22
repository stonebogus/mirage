using Mirage.Common.Events;
using Mirage.Math.Vectors;
using SDL3;

namespace Mirage.Windowing.Windows;

/// <summary>
/// Represents an SDL3-backed application window.
/// </summary>
/// <remarks>
/// Create, process, mutate and destroy this window on the main thread.
/// On Linux, X11 is selected by default; under a Wayland session this normally
/// runs through XWayland. Set <c>SDL_VIDEODRIVER</c> before SDL initialization
/// to explicitly select another video driver.
/// </remarks>
/// <param name="options">The initial window configuration.</param>
public sealed class SDL3Window(WindowOptions? options = null) : Window(options)
{
    private IntPtr _nativeWindow;

    private object? _publishingStore;
    private object? _publishingValue;

    static SDL3Window()
    {
        if (OperatingSystem.IsLinux())
            SDL.SetHint("SDL_VIDEO_DRIVER", "x11");
    }

    /// <summary>
    /// Gets the native SDL window while it is open.
    /// </summary>
    /// <remarks>
    /// Intended for backend-specific integration such as SDL GPU.
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

                return new Vector2D(x, y);
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

                return new Vector2D(width, height);
            }
        );

        Publish(window, Resizable, () => flags.HasFlag(SDL.WindowFlags.Resizable));

        Publish(window, Visible, () => !flags.HasFlag(SDL.WindowFlags.Hidden));

        Publish(window, _focused, () => flags.HasFlag(SDL.WindowFlags.InputFocus));

        Publish(window, Title, () => SDL.GetWindowTitle(window));
    }

    private static (int X, int Y) ToNativePosition(Vector2D position)
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

    private static (int Width, int Height) ToNativeSize(Vector2D size)
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
    protected override void OnClose()
    {
        var window = _nativeWindow;

        if (window == IntPtr.Zero)
            return;

        _nativeWindow = IntPtr.Zero;

        SDL.DestroyWindow(window);
        SDL3VideoRuntime.Release();
    }

    /// <inheritdoc />
    protected override void OnModeChanged(WindowMode mode)
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
    protected override void OnMove(Vector2D position)
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
    protected override void OnOpen()
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
    protected override void OnProcess()
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
    protected override void OnResizableChanged(bool resizable)
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
    protected override void OnResize(Vector2D size)
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
    protected override void OnTitleChanged(string title)
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
    protected override void OnVisibilityChanged(bool visible)
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
