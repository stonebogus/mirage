using System.Numerics;
using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Common.Events;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Primitives;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing;

namespace Mirage.Rendering;

/// <summary>
/// Draws rendering layers to a window.
/// </summary>
public class Renderer : Module, IUpdatable
{
    private readonly RenderSurface _surface;
    private readonly Window _window;
    private bool _composed;

    /// <summary>
    /// Gets the camera used to draw world coordinates.
    /// Set to null to draw directly in screen coordinates.
    /// </summary>
    public readonly Store<ICamera?> Camera;

    /// <summary>
    /// Gets the color used to clear each frame.
    /// </summary>
    public readonly Store<Color> ClearColor;

    /// <summary>
    /// Gets the rendering layers.
    /// </summary>
    public readonly ReactiveDictionary<string, DrawLayer> Layers = [];

    /// <summary>
    /// Initializes a renderer for the specified window.
    /// </summary>
    public Renderer(
        Window window,
        Color? clearColor = null,
        IEnumerable<DrawLayer>? layers = null,
        ICamera? camera = null
    )
        : base("Renderer")
    {
        _window = window;
        _surface = new RenderSurface(window);
        ClearColor = new Store<Color>(clearColor ?? Color.Black);
        Camera = new Store<ICamera?>(camera);

        foreach (var layer in layers ?? [])
            Layers.Add(layer.Identifier, layer);
    }

    /// <inheritdoc />
    public void Update(double deltaTime) => Render(deltaTime);

    private void EnsureComposed()
    {
        if (_composed)
            return;

        foreach (var layer in Compose().ToArray())
            Layers.Add(layer.Identifier, layer);

        _composed = true;
    }

    /// <summary>
    /// Provides additional layers when the renderer starts.
    /// </summary>
    protected virtual IEnumerable<DrawLayer> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        foreach (var (_, layer) in Layers.ToArray())
            layer.Destroy();

        Layers.Destroy();
        ClearColor.Destroy();
        _surface.Stop();
        Camera.Destroy();
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        EnsureComposed();
        _surface.Start();
    }

    /// <inheritdoc />
    protected override void OnStop() => _surface.Stop();

    /// <summary>
    /// Draws and presents one frame.
    /// </summary>
    public void Render(double deltaTime)
    {
        var context = _surface.BeginFrame(deltaTime, ClearColor.Get(), Camera.Get());

        try
        {
            foreach (var (_, layer) in Layers.OrderBy(pair => pair.Value.Priority))
                layer.Draw(context);
        }
        catch
        {
            _surface.AbortFrame();
            throw;
        }

        _surface.EndFrame();
    }
}
