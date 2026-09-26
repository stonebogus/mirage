using System.Numerics;
using System.Runtime.InteropServices;
using Mirage.Common.Events;
using Mirage.Common.Lifecycle;
using Mirage.Graphics.Resources;
using SDL3;
using Image = Mirage.Graphics.Resources.Image;

namespace Mirage.Windowing;

/// <summary>
/// Provides the initial values for a cursor.
/// </summary>
public sealed class CursorOptions
{
    /// <summary>
    /// Gets the click position within a custom image, in pixels.
    /// </summary>
    public Vector2 Hotspot { get; init; } = Vector2.Zero;

    /// <summary>
    /// Gets the custom image. When null, the system cursor is used.
    /// </summary>
    public Image? Icon { get; init; }

    /// <summary>
    /// Gets the system style used when <see cref="Icon"/> is null.
    /// </summary>
    public SDL.SystemCursor SystemIcon { get; init; } = SDL.SystemCursor.Default;

    /// <summary>
    /// Gets whether the cursor is visible when applied.
    /// </summary>
    public bool Visible { get; init; } = true;
}

/// <summary>
/// Owns a native SDL cursor backed by either a system style or an image.
/// </summary>
/// <remarks>
/// SDL cursor selection and visibility are global. Create, apply, change, and
/// destroy cursors on the main thread while SDL's video subsystem is initialized.
/// The cursor does not own its <see cref="Image"/>.
/// </remarks>
public sealed class Cursor : Destroyable
{
    private IntPtr _native;

    /// <summary>
    /// Gets the click position within a custom image, measured in pixels.
    /// Changing it recreates the native cursor when a custom image is set.
    /// </summary>
    public readonly Store<Vector2> Hotspot;

    /// <summary>
    /// Gets the custom cursor image. Set this to null to use <see cref="SystemIcon"/>.
    /// Changing it recreates the native cursor.
    /// </summary>
    public readonly Store<Image?> Icon;

    /// <summary>
    /// Gets the system cursor style used when <see cref="Icon"/> is null.
    /// Changing it recreates the native cursor when no custom image is set.
    /// </summary>
    public readonly Store<SDL.SystemCursor> SystemIcon;

    /// <summary>
    /// Gets whether this cursor should be visible while active.
    /// </summary>
    public readonly Store<bool> Visible;

    /// <summary>
    /// Creates a cursor with the supplied identifier and options.
    /// </summary>
    /// <param name="identifier">The cursor identifier.</param>
    /// <param name="options">The initial cursor settings.</param>
    public Cursor(string identifier, CursorOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        options ??= new CursorOptions();

        _native = CreateNative(options.Icon, options.SystemIcon, options.Hotspot);

        Identifier = identifier;
        Icon = new Store<Image?>(options.Icon);
        SystemIcon = new Store<SDL.SystemCursor>(options.SystemIcon);
        Hotspot = new Store<Vector2>(options.Hotspot);
        Visible = new Store<bool>(options.Visible);

        Icon.Connect(_ => Recreate());
        SystemIcon.Connect(_ =>
        {
            if (Icon.Get() is null)
                Recreate();
        });
        Hotspot.Connect(_ =>
        {
            if (Icon.Get() is not null)
                Recreate();
        });
        Visible.Connect(_ =>
        {
            if (SDL.GetCursor() == _native)
                ApplyVisibility();
        });
    }

    /// <summary>
    /// Gets this cursor's identifier.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the native SDL cursor handle.
    /// </summary>
    public IntPtr Native
    {
        get
        {
            ThrowIfDestroyed();
            return _native;
        }
    }

    private void ApplyVisibility()
    {
        var success = Visible.Get() ? SDL.ShowCursor() : SDL.HideCursor();

        if (!success)
            throw new InvalidOperationException(
                $"Changing SDL cursor visibility failed: {SDL.GetError()}"
            );
    }

    private static IntPtr CreateNative(Image? icon, SDL.SystemCursor systemIcon, Vector2 hotspot)
    {
        if (icon is null)
        {
            var systemCursor = SDL.CreateSystemCursor(systemIcon);

            if (systemCursor == IntPtr.Zero)
                throw new InvalidOperationException(
                    $"Creating SDL system cursor failed: {SDL.GetError()}"
                );

            return systemCursor;
        }

        if (icon.Destroyed)
            throw new ObjectDisposedException(nameof(icon));

        if (icon.Format != RasterImageFormat.Rgba8)
            throw new NotSupportedException($"Unsupported cursor image format: {icon.Format}.");

        if (icon.Width > int.MaxValue || icon.Height > int.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(icon),
                "Cursor image dimensions exceed SDL's limits."
            );

        ValidateHotspot(hotspot, icon);

        var width = checked((int)icon.Width);
        var height = checked((int)icon.Height);
        var pitch = checked(width * 4);

        // SDL_CreateSurfaceFrom borrows these pixels. Keep the array pinned
        // until both the SDL surface and cursor creation are finished.
        var pixels = icon.Data.ToArray();
        var pinnedPixels = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        IntPtr surface = IntPtr.Zero;

        try
        {
            surface = SDL.CreateSurfaceFrom(
                width,
                height,
                BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888,
                pinnedPixels.AddrOfPinnedObject(),
                pitch
            );

            if (surface == IntPtr.Zero)
                throw new InvalidOperationException(
                    $"Creating SDL cursor surface failed: {SDL.GetError()}"
                );

            var cursor = SDL.CreateColorCursor(surface, (int)hotspot.X, (int)hotspot.Y);

            if (cursor == IntPtr.Zero)
                throw new InvalidOperationException(
                    $"Creating SDL color cursor failed: {SDL.GetError()}"
                );

            return cursor;
        }
        finally
        {
            if (surface != IntPtr.Zero)
                SDL.DestroySurface(surface);

            pinnedPixels.Free();
        }
    }

    private void Recreate()
    {
        var replacement = CreateNative(Icon.Get(), SystemIcon.Get(), Hotspot.Get());

        var previous = _native;
        var wasActive = SDL.GetCursor() == previous;

        if (wasActive && !SDL.SetCursor(replacement))
        {
            SDL.DestroyCursor(replacement);
            throw new InvalidOperationException($"Setting SDL cursor failed: {SDL.GetError()}");
        }

        _native = replacement;
        SDL.DestroyCursor(previous);

        if (wasActive)
            ApplyVisibility();
    }

    private static void ValidateHotspot(Vector2 hotspot, Image icon)
    {
        if (
            !float.IsFinite(hotspot.X)
            || !float.IsFinite(hotspot.Y)
            || hotspot.X < 0
            || hotspot.Y < 0
            || hotspot.X >= icon.Width
            || hotspot.Y >= icon.Height
            || hotspot.X != MathF.Truncate(hotspot.X)
            || hotspot.Y != MathF.Truncate(hotspot.Y)
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(hotspot),
                "Hotspot must be a whole pixel coordinate inside the image."
            );
        }
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        if (SDL.GetCursor() == _native)
            SDL.SetCursor(SDL.GetDefaultCursor());

        SDL.DestroyCursor(_native);
        _native = IntPtr.Zero;

        Icon.Destroy();
        SystemIcon.Destroy();
        Hotspot.Destroy();
        Visible.Destroy();

        base.OnDestroy();
    }

    /// <summary>
    /// Makes this cursor active in SDL and applies its visibility.
    /// </summary>
    public void Apply()
    {
        ThrowIfDestroyed();

        if (!SDL.SetCursor(_native))
            throw new InvalidOperationException($"Setting SDL cursor failed: {SDL.GetError()}");

        ApplyVisibility();
    }
}
