using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Common.Events;
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
    private bool _composed;

    /// <summary>
    /// Gets the color used to clear each frame.
    /// </summary>
    public readonly Store<Color> ClearColor;

    /// <summary>
    /// Gets the rendering layers.
    /// </summary>
    public readonly ReactiveDictionary<string, RenderLayer> Layers = [];

    /// <summary>
    /// Initializes a renderer for the specified window.
    /// </summary>
    public Renderer(
        Window window,
        Color? clearColor = null,
        IEnumerable<RenderLayer>? layers = null
    )
        : base("Renderer")
    {
        ArgumentNullException.ThrowIfNull(window);

        _surface = new RenderSurface(window);
        ClearColor = new Store<Color>(clearColor ?? Color.Black);

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
    protected virtual IEnumerable<RenderLayer> Compose()
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
        var context = _surface.BeginFrame(deltaTime, ClearColor.Get());

        try
        {
            foreach (var (_, layer) in Layers.OrderBy(pair => pair.Value.Priority))
                layer.Render(context);
        }
        catch
        {
            _surface.AbortFrame();
            throw;
        }

        _surface.EndFrame();
    }
}
