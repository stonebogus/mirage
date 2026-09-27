using System.Numerics;
using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Common.Events;
using Mirage.Graphics.Interfaces;
using Mirage.Graphics.Primitives;
using Mirage.Scheduling;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing;

namespace Mirage.Rendering;

/// <summary>
/// Draws rendering layers to a window.
/// </summary>
/// <remarks>
/// The renderer owns and destroys its layers. It uses but does not own the window or camera.
/// </remarks>
public class Renderer : Module, IUpdatable
{
    private readonly RenderSurface _surface;
    private readonly Window _window;
    private bool _composed;
    private bool _compositionStarted;
    private bool _configurationStarted;
    private bool _configured;

    /// <summary>
    /// Gets the camera used to draw world coordinates, or <see langword="null"/>
    /// to draw directly in screen coordinates.
    /// </summary>
    public readonly Store<ICamera?> Camera;

    /// <summary>
    /// Gets the color used to clear each frame.
    /// </summary>
    public readonly Store<Color> ClearColor;

    /// <summary>
    /// Gets the identifiable set of rendering layers.
    /// </summary>
    public readonly IdentifiableSet<string, DrawLayer> Layers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Renderer"/> class.
    /// </summary>
    /// <param name="window">The window that receives rendered frames.</param>
    /// <param name="clearColor">The frame clear color, or <see langword="null"/> for <see cref="Color.Black"/>.</param>
    /// <param name="layers">The initial layers, or <see langword="null"/> for no layers.</param>
    /// <param name="camera">The initial camera, or <see langword="null"/> for screen coordinates.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the initial layers contain duplicate identifiers.
    /// </exception>
    public Renderer(
        Window window,
        Color? clearColor = null,
        IEnumerable<DrawLayer>? layers = null,
        ICamera? camera = null
    )
        : base("Renderer")
    {
        _window = window;
        _window.DeferVisibilityUntilFirstFrame();
        _surface = new RenderSurface(window);
        ClearColor = new Store<Color>(clearColor ?? Color.Black);
        Camera = new Store<ICamera?>(camera);

        foreach (var layer in layers ?? [])
        {
            Layers.Add(layer);
        }
    }

    /// <inheritdoc />
    public virtual void Update(UpdateContext context) => Render(context);

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        Layers.Add(Compose().ToArray());

        _composed = true;
    }

    private void EnsureConfigured()
    {
        if (_configured)
            return;

        if (_configurationStarted)
            throw new InvalidOperationException("Configuration has already started or failed.");

        _configurationStarted = true;
        Configure();
        _configured = true;
    }

    /// <summary>
    /// Composes the layers managed by this object.
    /// </summary>
    /// <returns>The layers to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once when the renderer first starts, before its render surface starts.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The renderer owns and destroys its layers, but not its window or camera.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<DrawLayer> Compose()
    {
        yield break;
    }

    /// <summary>
    /// Configures relationships and behavior after composition, before startup or first use.
    /// </summary>
    /// <remarks>
    /// All constructor-provided and composed objects are available here.
    /// This hook is invoked at most once, including across later lifecycle cycles.
    /// If configuration throws, later lifecycle calls reject further initialization
    /// rather than repeating configuration side effects.
    /// </remarks>
    protected virtual void Configure() { }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var layer in Layers.ToArray())
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
        EnsureConfigured();
        _surface.Start();
    }

    /// <inheritdoc />
    protected override void OnStop() => _surface.Stop();

    /// <summary>
    /// Draws and presents one frame.
    /// </summary>
    /// <param name="updateContext">
    /// The context describing the current update, including its delta time
    /// and target update rate.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the renderer is not started or a frame is already active.
    /// </exception>
    public void Render(UpdateContext updateContext)
    {
        var context = _surface.BeginFrame(updateContext.DeltaTime, ClearColor.Get(), Camera.Get());

        try
        {
            foreach (var layer in Layers.OrderBy(layer => layer.Priority))
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
