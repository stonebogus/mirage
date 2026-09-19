using Mirage.Common.Events;
using Mirage.Math.Vectors;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTkWindowMode = OpenTK.Windowing.Common.WindowState;

namespace Mirage.Windowing.Windows;

/// <summary>
/// Represents an OpenTK-backed application window.
/// </summary>
/// <remarks>
/// Create, update, mutate and destroy this window on the main thread.
/// Linux uses X11, through XWayland when running a Wayland session.
/// </remarks>
/// <param name="options">The initial window configuration.</param>
public sealed class OpenTkWindow(WindowOptions? options = null) : Window(options)
{
    private NativeWindow? _nativeWindow;

    private object? _publishingStore;
    private object? _publishingValue;

    static OpenTkWindow()
    {
        if (OperatingSystem.IsLinux())
            Environment.SetEnvironmentVariable("OPENTK_4_USE_WAYLAND", "0");
    }

    /// <summary>
    /// Gets the native window while it is open.
    /// </summary>
    /// <remarks>
    /// Intended for backend-specific rendering integration.
    /// Do not dispose it directly or change window properties through it;
    /// use the Mirage stores instead.
    /// </remarks>
    public NativeWindow? Native => _nativeWindow;

    private static WindowMode FromOpenTkMode(OpenTkWindowMode mode)
    {
        return mode switch
        {
            OpenTkWindowMode.Normal => WindowMode.Normal,
            OpenTkWindowMode.Minimized => WindowMode.Minimized,
            OpenTkWindowMode.Maximized => WindowMode.Maximized,
            OpenTkWindowMode.Fullscreen => WindowMode.Fullscreen,

            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unknown OpenTK window mode."
            ),
        };
    }

    private void Publish<TValue>(NativeWindow window, Store<TValue> store, Func<TValue> read)
    {
        if (!ReferenceEquals(_nativeWindow, window) || Destroyed)
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

    private void SynchronizeState(NativeWindow window)
    {
        Publish(window, Mode, () => FromOpenTkMode(window.WindowState));

        Publish(
            window,
            Position,
            () =>
            {
                var position = window.ClientLocation;
                return new Vector2D(position.X, position.Y);
            }
        );

        Publish(
            window,
            Size,
            () =>
            {
                var size = window.ClientSize;
                return new Vector2D(size.X, size.Y);
            }
        );

        Publish(window, Visible, () => window.IsVisible);
        Publish(window, _focused, () => window.IsFocused);
    }

    private static Vector2i ToNativePosition(Vector2D position)
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

        return new Vector2i((int)position.X, (int)position.Y);
    }

    private static Vector2i ToNativeSize(Vector2D size)
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

        return new Vector2i((int)size.X, (int)size.Y);
    }

    private static OpenTkWindowMode ToOpenTkMode(WindowMode mode)
    {
        return mode switch
        {
            WindowMode.Normal => OpenTkWindowMode.Normal,
            WindowMode.Minimized => OpenTkWindowMode.Minimized,
            WindowMode.Maximized => OpenTkWindowMode.Maximized,
            WindowMode.Fullscreen => OpenTkWindowMode.Fullscreen,

            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown window mode."),
        };
    }

    /// <inheritdoc />
    protected override void OnClose()
    {
        var window = _nativeWindow;

        if (window is null)
            return;

        _nativeWindow = null;
        window.Dispose();

        _focused.Set(false);
        Visible.Set(false);

        _opened.Set(false);
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        OnClose();
        base.OnDestroy();
    }

    /// <inheritdoc />
    protected override void OnModeChanged(WindowMode mode)
    {
        if (!ShouldApply(Mode, mode))
            return;

        var nativeMode = ToOpenTkMode(mode);

        if (_nativeWindow is not { } window)
            return;

        if (window.WindowState != nativeMode)
            window.WindowState = nativeMode;
    }

    /// <inheritdoc />
    protected override void OnMove(Vector2D position)
    {
        if (!ShouldApply(Position, position))
            return;

        var nativePosition = ToNativePosition(position);

        if (_nativeWindow is not { } window)
            return;

        if (window.WindowState == OpenTkWindowMode.Fullscreen)
            return;

        if (window.ClientLocation != nativePosition)
            window.ClientLocation = nativePosition;
    }

    /// <inheritdoc />
    protected override void OnOpen()
    {
        if (_nativeWindow is not null)
            return;

        var requestedMode = ToOpenTkMode(Mode.Get());
        var requestedSize = ToNativeSize(Size.Get());

        Vector2i? requestedPosition = HasInitialPosition ? ToNativePosition(Position.Get()) : null;

        NativeWindowSettings settings = new()
        {
            Title = Title.Get(),
            ClientSize = requestedSize,

            Location = null,

            StartVisible = Visible.Get(),
            StartFocused = Visible.Get(),

            WindowBorder = Resizable.Get() ? WindowBorder.Resizable : WindowBorder.Fixed,

            WindowState = requestedMode,
            IsEventDriven = false,

            API = ContextAPI.OpenGL,
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible,

            Vsync = VSyncMode.Off,
        };

        var window = new NativeWindow(settings);
        _nativeWindow = window;

        try
        {
            if (requestedPosition is { } position && requestedMode == OpenTkWindowMode.Normal)
            {
                window.ClientLocation = position;
            }
        }
        catch
        {
            _nativeWindow = null;
            window.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    protected override void OnResizableChanged(bool resizable)
    {
        if (!ShouldApply(Resizable, resizable) || _nativeWindow is not { } window)
        {
            return;
        }

        var border = resizable ? WindowBorder.Resizable : WindowBorder.Fixed;

        if (window.WindowBorder != border)
            window.WindowBorder = border;
    }

    /// <inheritdoc />
    protected override void OnResize(Vector2D size)
    {
        if (!ShouldApply(Size, size))
            return;

        var nativeSize = ToNativeSize(size);

        if (_nativeWindow is not { } window)
            return;

        if (window.WindowState == OpenTkWindowMode.Fullscreen)
            return;

        if (window.ClientSize != nativeSize)
            window.ClientSize = nativeSize;
    }

    /// <inheritdoc />
    protected override void OnTitleChanged(string title)
    {
        if (!ShouldApply(Title, title) || _nativeWindow is not { } window)
        {
            return;
        }

        if (window.Title != title)
            window.Title = title;
    }

    /// <inheritdoc />
    protected override void OnVisibilityChanged(bool visible)
    {
        if (!ShouldApply(Visible, visible) || _nativeWindow is not { } window)
        {
            return;
        }

        if (window.IsVisible != visible)
            window.IsVisible = visible;
    }

    /// <inheritdoc />
    internal override void Process()
    {
        ThrowIfDestroyed();

        if (_nativeWindow is not { } window)
            return;

        window.NewInputFrame();

        NativeWindow.ProcessWindowEvents(waitForEvents: false);

        if (!ReferenceEquals(_nativeWindow, window) || Destroyed)
            return;

        if (window.IsExiting)
        {
            Close();
            return;
        }

        SynchronizeState(window);
    }
}
