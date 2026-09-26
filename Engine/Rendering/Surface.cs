using System.Numerics;
using System.Runtime.InteropServices;
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
    private const int RenderScale = 2;

    private readonly Dictionary<Texture, nint> _textures = [];

    private bool _frameActive;

    private nint _frameTexture;
    private int _frameTextureHeight;
    private int _frameTextureWidth;
    private nint _native;
    private bool _textInitialized;

    private static SDL.Vertex CreateVertex(Vector2 position, SDL.FColor color)
    {
        return new SDL.Vertex
        {
            Position = new SDL.FPoint { X = position.X, Y = position.Y },
            Color = color,
            TexCoord = default,
        };
    }

    private void DrawNativeTexture(
        nint nativeTexture,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft,
        string operation
    )
    {
        var white = new SDL.FColor
        {
            R = 1f,
            G = 1f,
            B = 1f,
            A = 1f,
        };

        Span<SDL.Vertex> vertices = stackalloc SDL.Vertex[6];

        vertices[0] = TexturedVertex(topLeft, 0f, 0f, white);
        vertices[1] = TexturedVertex(topRight, 1f, 0f, white);
        vertices[2] = TexturedVertex(bottomRight, 1f, 1f, white);

        vertices[3] = TexturedVertex(topLeft, 0f, 0f, white);
        vertices[4] = TexturedVertex(bottomRight, 1f, 1f, white);
        vertices[5] = TexturedVertex(bottomLeft, 0f, 1f, white);

        if (!SDL.RenderGeometry(_native, nativeTexture, vertices, 6, nint.Zero, 0))
            throw Error(operation);
    }

    private void EnsureFrame()
    {
        EnsureStarted();

        if (!_frameActive)
            throw new InvalidOperationException("Drawing requires an active frame.");
    }

    private void EnsureFrameTexture(Vector2 viewportSize)
    {
        var width = checked(System.Math.Max(1, (int)MathF.Ceiling(viewportSize.X)) * RenderScale);

        var height = checked(System.Math.Max(1, (int)MathF.Ceiling(viewportSize.Y)) * RenderScale);

        if (
            _frameTexture != nint.Zero
            && width == _frameTextureWidth
            && height == _frameTextureHeight
        )
        {
            return;
        }

        var replacement = SDL.CreateTexture(
            _native,
            SDL.PixelFormat.ABGR8888,
            SDL.TextureAccess.Target,
            width,
            height
        );

        if (replacement == nint.Zero)
            throw Error("Creating the frame texture");

        try
        {
            if (!SDL.SetTextureScaleMode(replacement, SDL.ScaleMode.Linear))
                throw Error("Setting the frame texture scale mode");
        }
        catch
        {
            SDL.DestroyTexture(replacement);
            throw;
        }

        if (_frameTexture != nint.Zero)
            SDL.DestroyTexture(_frameTexture);

        _frameTexture = replacement;
        _frameTextureWidth = width;
        _frameTextureHeight = height;
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

    private static OpenedFont OpenFont(Font font, float size, FontStyle style)
    {
        // SDL_IOFromConstMem borrows this buffer. Keep it pinned until both
        // the native font and its IO stream have been closed.
        var bytes = font.Data.ToArray();
        var pin = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        var stream = NativeText.IOFromConstMem(pin.AddrOfPinnedObject(), (nuint)bytes.Length);

        if (stream == nint.Zero)
        {
            pin.Free();
            throw Error("Opening font bytes");
        }

        // This class closes the stream explicitly, including when opening
        // the font fails.
        var nativeFont = NativeText.OpenFontIO(stream, false, size);

        if (nativeFont == nint.Zero)
        {
            var error = Error("Opening a font");
            NativeText.CloseIO(stream);
            pin.Free();
            throw error;
        }

        NativeText.SetFontStyle(nativeFont, (uint)style);

        return new OpenedFont(nativeFont, stream, pin);
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

    private static SDL.Vertex TexturedVertex(Vector2 position, float u, float v, SDL.FColor color)
    {
        return new SDL.Vertex
        {
            Position = new SDL.FPoint { X = position.X, Y = position.Y },
            Color = color,
            TexCoord = new SDL.FPoint { X = u, Y = v },
        };
    }

    private static byte ToByte(float value) => (byte)MathF.Round(value * 255f);

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

    private static void ValidateText(Font font, string text, float size, FontStyle style)
    {
        if (!float.IsFinite(size) || size <= 0f)
            throw new ArgumentOutOfRangeException(nameof(size));

        const FontStyle supported =
            FontStyle.Bold | FontStyle.Italic | FontStyle.Underline | FontStyle.Strikethrough;

        if ((style & ~supported) != 0)
            throw new ArgumentOutOfRangeException(nameof(style));
    }

    /// <summary>
    /// Abandons the active frame without presenting it.
    /// </summary>
    public void AbortFrame()
    {
        if (!_frameActive)
            return;

        _frameActive = false;

        // Drawing must not remain directed at the intermediate texture.
        SDL.SetRenderTarget(_native, nint.Zero);
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

        var viewportSize = window.Size.Get();

        ReleaseDestroyedTextures();
        EnsureFrameTexture(viewportSize);

        if (!SDL.SetRenderTarget(_native, _frameTexture))
            throw Error("Selecting the frame texture");

        try
        {
            if (!SDL.SetRenderScale(_native, RenderScale, RenderScale))
                throw Error("Setting the frame render scale");

            SetDrawColor(clearColor);

            if (!SDL.RenderClear(_native))
                throw Error("Clearing the frame");
        }
        catch
        {
            SDL.SetRenderTarget(_native, nint.Zero);
            throw;
        }

        _frameActive = true;
        return new RenderContext(this, deltaTime, camera, viewportSize);
    }

    /// <summary>
    /// Reduces and presents the active frame to the window.
    /// </summary>
    public void EndFrame()
    {
        EnsureFrame();

        _frameActive = false;

        if (!SDL.SetRenderTarget(_native, nint.Zero))
            throw Error("Selecting the window render target");

        var viewportSize = window.Size.Get();

        var destination = new SDL.FRect
        {
            X = 0f,
            Y = 0f,
            W = viewportSize.X,
            H = viewportSize.Y,
        };

        if (!SDL.RenderTexture(_native, _frameTexture, nint.Zero, in destination))
            throw Error("Reducing the frame texture");

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
        {
            throw new InvalidOperationException(
                "The window must be open before the renderer starts."
            );
        }

        _native = SDL.CreateRenderer(window.Native, null);

        if (_native == nint.Zero)
            throw Error("Creating the renderer");

        try
        {
            ApplyVSync(window.VSync.Get());

            if (!NativeText.Init())
                throw Error("Initializing SDL3_ttf");

            _textInitialized = true;
        }
        catch
        {
            if (_textInitialized)
            {
                NativeText.Quit();
                _textInitialized = false;
            }

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
        SDL.SetRenderTarget(_native, nint.Zero);

        if (_frameTexture != nint.Zero)
        {
            SDL.DestroyTexture(_frameTexture);
            _frameTexture = nint.Zero;
        }

        _frameTextureWidth = 0;
        _frameTextureHeight = 0;

        foreach (var nativeTexture in _textures.Values)
            SDL.DestroyTexture(nativeTexture);

        _textures.Clear();

        if (_textInitialized)
        {
            NativeText.Quit();
            _textInitialized = false;
        }

        SDL.DestroyRenderer(_native);
        _native = nint.Zero;
    }

    /// <summary>
    /// Fills a triangle using screen coordinates.
    /// </summary>
    internal void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
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

    /// <summary>
    /// Fills a circle using screen coordinates.
    /// </summary>
    internal void FillCircle(Vector2 center, float radius, Color color)
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

    /// <summary>
    /// Enables or disables vertical synchronization.
    /// </summary>
    /// <param name="enabled">Whether vertical synchronization is enabled.</param>
    internal void ApplyVSync(bool enabled)
    {
        if (_native == nint.Zero)
            return;

        if (!SDL.SetRenderVSync(_native, enabled ? 1 : 0))
            throw Error("Changing VSync");
    }

    /// <summary>
    /// Draws a line with a specified thickness using screen coordinates.
    /// </summary>
    internal void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        EnsureFrame();

        if (!float.IsFinite(thickness) || thickness <= 0f)
            throw new ArgumentOutOfRangeException(nameof(thickness));

        var direction = end - start;
        var halfThickness = thickness / 2f;

        if (direction == Vector2.Zero)
        {
            FillRectangle(start - new Vector2(halfThickness), new Vector2(thickness), color);
            return;
        }

        var normal = Vector2.Normalize(new Vector2(-direction.Y, direction.X)) * halfThickness;

        var a = start - normal;
        var b = start + normal;
        var c = end + normal;
        var d = end - normal;

        var nativeColor = ToNativeColor(color);
        Span<SDL.Vertex> vertices = stackalloc SDL.Vertex[6];

        vertices[0] = CreateVertex(a, nativeColor);
        vertices[1] = CreateVertex(b, nativeColor);
        vertices[2] = CreateVertex(c, nativeColor);

        vertices[3] = CreateVertex(a, nativeColor);
        vertices[4] = CreateVertex(c, nativeColor);
        vertices[5] = CreateVertex(d, nativeColor);

        if (!SDL.RenderGeometry(_native, nint.Zero, vertices, 6, nint.Zero, 0))
            throw Error("Drawing a line");
    }

    /// <summary>
    /// Draws a texture across four corners in screen coordinates.
    /// </summary>
    internal void DrawTexture(
        Texture texture,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft
    )
    {
        EnsureFrame();

        var nativeTexture = GetTexture(texture);
        DrawNativeTexture(
            nativeTexture,
            topLeft,
            topRight,
            bottomRight,
            bottomLeft,
            "Drawing a texture"
        );
    }

    /// <summary>
    /// Draws a textured mesh using vertex positions in screen coordinates.
    /// </summary>
    internal void DrawMesh(GraphicMesh mesh, ReadOnlySpan<Vector2> positions)
    {
        EnsureFrame();

        var geometry = mesh.Mesh;
        var localVertices = geometry.Vertices.Span;
        var textureCoordinates = mesh.TexCoords.Span;
        var indices = geometry.Indices.Span;

        if (positions.Length != localVertices.Length)
        {
            throw new ArgumentException(
                "The position count must match the mesh vertex count.",
                nameof(positions)
            );
        }

        var white = new SDL.FColor
        {
            R = 1f,
            G = 1f,
            B = 1f,
            A = 1f,
        };

        var vertices = new SDL.Vertex[positions.Length];

        for (var index = 0; index < vertices.Length; index++)
        {
            var position = positions[index];
            var uv = textureCoordinates[index];

            vertices[index] = TexturedVertex(position, uv.X, uv.Y, white);
        }

        var nativeTexture = GetTexture(mesh.Texture);

        if (
            !SDL.RenderGeometry(
                _native,
                nativeTexture,
                vertices,
                vertices.Length,
                indices,
                indices.Length
            )
        )
        {
            throw Error("Drawing a mesh");
        }
    }

    /// <summary>
    /// Measures text before camera scaling or drawing transformations.
    /// </summary>
    internal Vector2 MeasureText(Font font, string text, float size, FontStyle style)
    {
        EnsureFrame();
        ValidateText(font, text, size, style);

        if (text.Length == 0)
            return Vector2.Zero;

        using var opened = OpenFont(font, size, style);

        if (!NativeText.GetStringSize(opened.Handle, text, 0, out var width, out var height))
        {
            throw Error("Measuring text");
        }

        return new Vector2(width, height);
    }

    /// <summary>
    /// Draws text across four corners in screen coordinates.
    /// </summary>
    internal void DrawText(
        Font font,
        string text,
        float size,
        Color color,
        FontStyle style,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft
    )
    {
        EnsureFrame();
        ValidateText(font, text, size, style);

        if (text.Length == 0)
            return;

        using var opened = OpenFont(font, size, style);

        var value = color.Clamped();
        var nativeColor = new NativeText.NativeColor(
            ToByte(value.R),
            ToByte(value.G),
            ToByte(value.B),
            ToByte(value.A)
        );

        var textSurface = NativeText.RenderTextBlended(opened.Handle, text, 0, nativeColor);

        if (textSurface == nint.Zero)
            throw Error("Rasterizing text");

        nint nativeTexture;

        try
        {
            nativeTexture = NativeText.CreateTextureFromSurface(_native, textSurface);

            if (nativeTexture == nint.Zero)
                throw Error("Creating a text texture");
        }
        finally
        {
            SDL.DestroySurface(textSurface);
        }

        try
        {
            DrawNativeTexture(
                nativeTexture,
                topLeft,
                topRight,
                bottomRight,
                bottomLeft,
                "Drawing text"
            );
        }
        finally
        {
            SDL.DestroyTexture(nativeTexture);
        }
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

    private sealed class OpenedFont(nint handle, nint stream, GCHandle pin) : IDisposable
    {
        private GCHandle _pin = pin;
        public nint Handle { get; } = handle;

        public void Dispose()
        {
            NativeText.CloseFont(Handle);
            NativeText.CloseIO(stream);
            _pin.Free();
        }
    }
}
