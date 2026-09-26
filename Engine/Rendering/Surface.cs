using System.Numerics;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Primitives;
using Mirage.Graphics.Resources;
using Mirage.Windowing;
using SDL3;

namespace Mirage.Rendering;

/// <summary>
/// Owns the native renderer and textures for a window.
/// </summary>
internal sealed class RenderSurface(Window window)
{
    private readonly Dictionary<Texture, nint> _textures = [];
    private bool _frameActive;
    private nint _native;

    private static SDL.Vertex CreateVertex(Vector2 position, SDL.FColor color)
    {
        return new SDL.Vertex
        {
            Position = new SDL.FPoint { X = position.X, Y = position.Y },
            Color = color,
            TexCoord = default,
        };
    }

    private void EnsureFrame()
    {
        EnsureStarted();

        if (!_frameActive)
            throw new InvalidOperationException("Drawing requires an active frame.");
    }

    private void EnsureStarted()
    {
        if (_native == nint.Zero)
            throw new InvalidOperationException("The renderer is not started.");
    }

    private static InvalidOperationException Error(string operation) =>
        new($"{operation} failed: {SDL.GetError()}");

    private nint GetTexture(Texture texture)
    {
        if (_textures.TryGetValue(texture, out var existing))
            return existing;

        var image = texture.Image;

        if (image.Destroyed)
            throw new ObjectDisposedException(nameof(image));

        // On little-endian PCs, ABGR8888 reads these bytes as RGBA.
        var nativeTexture = SDL.CreateTexture(
            _native,
            SDL.PixelFormat.ABGR8888,
            SDL.TextureAccess.Static,
            checked((int)image.Width),
            checked((int)image.Height)
        );

        if (nativeTexture == nint.Zero)
            throw Error("Creating a texture");

        try
        {
            unsafe
            {
                var pixels = image.Data.Span;

                fixed (byte* pointer = pixels)
                {
                    if (
                        !SDL.UpdateTexture(
                            nativeTexture,
                            nint.Zero,
                            (nint)pointer,
                            checked((int)image.Width * 4)
                        )
                    )
                    {
                        throw Error("Uploading texture pixels");
                    }
                }
            }

            _textures.Add(texture, nativeTexture);
            return nativeTexture;
        }
        catch
        {
            SDL.DestroyTexture(nativeTexture);
            throw;
        }
    }

    private void ReleaseDestroyedTextures()
    {
        foreach (var (texture, nativeTexture) in _textures.ToArray())
        {
            if (!texture.Destroyed)
                continue;

            SDL.DestroyTexture(nativeTexture);
            _textures.Remove(texture);
        }
    }

    private void SetDrawColor(Color color)
    {
        var value = color.Clamped();

        if (!SDL.SetRenderDrawColorFloat(_native, value.R, value.G, value.B, value.A))
            throw Error("Setting the drawing color");
    }

    private static SDL.FColor ToNativeColor(Color color)
    {
        var value = color.Clamped();

        return new SDL.FColor
        {
            R = value.R,
            G = value.G,
            B = value.B,
            A = value.A,
        };
    }

    private static void ValidateRectangle(Vector2 position, Vector2 size)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
            throw new ArgumentOutOfRangeException(nameof(position));

        if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X < 0 || size.Y < 0)
            throw new ArgumentOutOfRangeException(nameof(size));
    }

    /// <summary>
    /// Abandons the active frame without presenting it.
    /// </summary>
    public void AbortFrame()
    {
        // The next BeginFrame clears the backbuffer before drawing again.
        _frameActive = false;
    }

    /// <summary>
    /// Clears the window and creates a context for a new frame.
    /// </summary>
    /// <param name="deltaTime">
    /// The elapsed time since the previous frame, in seconds.
    /// </param>
    /// <param name="clearColor">The color used to clear the frame.</param>
    /// <param name="camera">
    /// The active camera, or <see langword="null"/> to use screen coordinates.
    /// </param>
    /// <returns>The context used to draw the frame.</returns>
    public RenderContext BeginFrame(double deltaTime, Color clearColor, ICamera? camera)
    {
        EnsureStarted();

        if (_frameActive)
            throw new InvalidOperationException("A frame is already active.");

        var context = new RenderContext(this, deltaTime, camera, window.Size.Get());

        ReleaseDestroyedTextures();
        SetDrawColor(clearColor);

        if (!SDL.RenderClear(_native))
            throw Error("Clearing the frame");

        _frameActive = true;
        return context;
    }

    /// <summary>
    /// Presents the active frame to the window.
    /// </summary>
    public void EndFrame()
    {
        EnsureFrame();

        _frameActive = false;

        if (!SDL.RenderPresent(_native))
            throw Error("Presenting the frame");
    }

    /// <summary>
    /// Creates the native renderer for the open window.
    /// </summary>
    public void Start()
    {
        if (_native != nint.Zero)
            return;

        if (!window.Opened.Get() || window.Native == nint.Zero)
            throw new InvalidOperationException(
                "The window must be open before the renderer starts."
            );

        _native = SDL.CreateRenderer(window.Native, null);

        if (_native == nint.Zero)
            throw Error("Creating the renderer");

        try
        {
            ApplyVSync(window.VSync.Get());
        }
        catch
        {
            SDL.DestroyRenderer(_native);
            _native = nint.Zero;
            throw;
        }
    }

    /// <summary>
    /// Releases native textures and stops the native renderer.
    /// </summary>
    public void Stop()
    {
        if (_native == nint.Zero)
            return;

        _frameActive = false;

        foreach (var nativeTexture in _textures.Values)
            SDL.DestroyTexture(nativeTexture);

        _textures.Clear();

        SDL.DestroyRenderer(_native);
        _native = nint.Zero;
    }

    /// <summary>
    /// Fills a triangle using screen coordinates.
    /// </summary>
    internal unsafe void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        EnsureFrame();

        var nativeColor = ToNativeColor(color);

        Span<SDL.Vertex> vertices = stackalloc SDL.Vertex[3];

        vertices[0] = CreateVertex(a, nativeColor);
        vertices[1] = CreateVertex(b, nativeColor);
        vertices[2] = CreateVertex(c, nativeColor);

        if (!SDL.RenderGeometry(_native, nint.Zero, vertices, 3, nint.Zero, 0))
            throw Error("Filling a triangle");
    }

    internal unsafe void FillCircle(Vector2 center, float radius, Color color)
    {
        EnsureFrame();

        if (!float.IsFinite(radius) || radius < 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));

        if (radius == 0f)
            return;

        var segments = global::System.Math.Clamp(
            (int)MathF.Ceiling(MathF.Min(radius * 0.5f, 128f)),
            16,
            128
        );

        var vertexCount = segments * 3;
        Span<SDL.Vertex> vertices = stackalloc SDL.Vertex[vertexCount];

        var nativeColor = ToNativeColor(color);

        for (var index = 0; index < segments; index++)
        {
            var startAngle = MathF.Tau * index / segments;
            var endAngle = MathF.Tau * (index + 1) / segments;

            var start = center + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * radius;

            var end = center + new Vector2(MathF.Cos(endAngle), MathF.Sin(endAngle)) * radius;

            var firstVertex = index * 3;

            vertices[firstVertex] = CreateVertex(center, nativeColor);
            vertices[firstVertex + 1] = CreateVertex(start, nativeColor);
            vertices[firstVertex + 2] = CreateVertex(end, nativeColor);
        }

        if (!SDL.RenderGeometry(_native, nint.Zero, vertices, vertexCount, nint.Zero, 0))
            throw Error("Filling a circle");
    }

    internal void ApplyVSync(bool enabled)
    {
        if (_native == nint.Zero)
            return;

        if (!SDL.SetRenderVSync(_native, enabled ? 1 : 0))
            throw Error("Changing VSync");
    }

    internal void DrawTexture(Texture texture, Vector2 position, Vector2 size)
    {
        EnsureFrame();
        ArgumentNullException.ThrowIfNull(texture);

        if (texture.Destroyed)
            throw new ObjectDisposedException(nameof(texture));

        ValidateRectangle(position, size);

        var nativeTexture = GetTexture(texture);

        var destination = new SDL.FRect
        {
            X = position.X,
            Y = position.Y,
            W = size.X,
            H = size.Y,
        };

        if (!SDL.RenderTexture(_native, nativeTexture, nint.Zero, in destination))
            throw Error("Drawing a texture");
    }

    /// <summary>
    /// Fills a rectangle using screen coordinates.
    /// </summary>
    internal void FillRectangle(Vector2 position, Vector2 size, Color color)
    {
        EnsureFrame();
        ValidateRectangle(position, size);
        SetDrawColor(color);

        var rectangle = new SDL.FRect
        {
            X = position.X,
            Y = position.Y,
            W = size.X,
            H = size.Y,
        };

        if (!SDL.RenderFillRect(_native, in rectangle))
            throw Error("Filling a rectangle");
    }
}
