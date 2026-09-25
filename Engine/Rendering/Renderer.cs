using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Common.Events;
using Mirage.Graphics;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Primitives;
using Mirage.Rendering.Backends;
using Mirage.Scheduling.Interfaces;

namespace Mirage.Rendering;

/// <summary>
/// Orders renderable entries and submits them to a rendering backend.
/// </summary>
public class Renderer : Module, IUpdatable
{
    private readonly List<IRenderable> _entries = [];
    private bool _composed;

    /// <summary>
    /// Gets the rendering backend.
    /// </summary>
    public readonly RendererBackend Backend;

    /// <summary>
    /// Gets the store containing the frame clear color.
    /// </summary>
    public readonly Store<Color> ClearColor;

    /// <summary>
    /// Gets the layers managed by the renderer.
    /// </summary>
    public readonly ReactiveDictionary<string, RenderLayer> Layers = [];

    /// <summary>
    /// Initializes a new renderer.
    /// </summary>
    /// <param name="backend">
    /// The rendering backend to use.
    /// </param>
    /// <param name="clearColor">
    /// The initial frame clear color, or <see langword="null"/> to use black.
    /// </param>
    /// <param name="layers">
    /// The initial rendering layers.
    /// </param>
    public Renderer(
        RendererBackend backend,
        Color? clearColor = null,
        IEnumerable<RenderLayer>? layers = null
    )
        : base("Renderer")
    {
        ArgumentNullException.ThrowIfNull(backend);

        Backend = backend;
        ClearColor = new Store<Color>(clearColor ?? Color.Black);

        foreach (var layer in layers ?? [])
            Layers.Add(layer.Identifier, layer);
    }

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        Render(deltaTime);
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        foreach (var layer in Compose().ToArray())
            Layers.Add(layer.Identifier, layer);

        _composed = true;
    }

    /// <summary>
    /// Composes the layers managed by this renderer.
    /// </summary>
    /// <returns>
    /// The layers to register.
    /// </returns>
    /// <remarks>
    /// Composition occurs once when the renderer starts. Layers supplied to
    /// the constructor are registered before composed layers.
    /// </remarks>
    protected virtual IEnumerable<RenderLayer> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        foreach (var (_, layer) in Layers.ToArray())
            layer.Destroy();

        _entries.Clear();

        Layers.Destroy();
        ClearColor.Destroy();
        Backend.Destroy();
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        EnsureComposed();
        Backend.Start();
    }

    /// <inheritdoc />
    protected override void OnStop()
    {
        Backend.Stop();
    }

    /// <summary>
    /// Renders a frame using the configured backend.
    /// </summary>
    /// <param name="deltaTime">
    /// The elapsed time since the previous frame, in seconds.
    /// </param>
    public void Render(double deltaTime)
    {
        var context = new RenderContext(deltaTime, ClearColor.Get());

        _entries.Clear();

        // Lower-priority layers render first. Higher-priority layers render
        // later and therefore appear above them when blending is enabled.
        foreach (var (_, layer) in Layers.OrderBy(pair => pair.Value.Priority))
        {
            _entries.AddRange(layer.Collect());
        }

        Backend.RenderFrame(context, _entries);
    }
}
