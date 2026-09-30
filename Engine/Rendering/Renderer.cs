using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Logging;
using Mirage.Rendering.Layers;
using Mirage.Scheduling;
using Mirage.Scheduling.Interfaces;

namespace Mirage.Rendering;

/// <summary>
/// Manages the rendering layers used to produce frames.
/// </summary>
/// <remarks>
/// The renderer owns and destroys its registered layers.
/// Rendering infrastructure is provided separately by the active rendering implementation.
/// </remarks>
public class Renderer : Module, IUpdatable
{
    private bool _composed;
    private bool _compositionStarted;
    private bool _configurationStarted;
    private bool _configured;

    /// <summary>
    /// Gets the identifiable set of rendering layers managed by the renderer.
    /// </summary>
    /// <remarks>
    /// The renderer owns and destroys its registered layers.
    /// Registration transfers ownership to the renderer. Removing or clearing
    /// entries returns ownership to the caller without destroying them.
    /// Do not register an object owned elsewhere.
    /// </remarks>
    public readonly IdentifiableSet<string, RenderLayer> Layers = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Renderer"/> class.
    /// </summary>
    /// <param name="layers">
    /// The initial rendering layers, or <see langword="null"/> for no layers.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the initial layers contain duplicate identifiers.
    /// </exception>
    public Renderer(IEnumerable<RenderLayer>? layers = null)
        : base("Renderer", ["Logger"])
    {
        foreach (var layer in layers ?? [])
            Layers.Add(layer);
    }

    private SourcedLogger Logger => Require<Logger>("Logger").From(Identifier);

    /// <inheritdoc />
    public virtual void Update(UpdateContext context) { }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        Logger.Log("Composing module contents.", LogMessageKind.Debug);
        _compositionStarted = true;

        foreach (var layer in Compose())
        {
            try
            {
                Layers.Add(layer);
            }
            catch
            {
                if (!Layers.Contains(layer) && !layer.Destroyed)
                    layer.Destroy();

                throw;
            }
        }

        _composed = true;

        Logger.Log("Composition completed.", LogMessageKind.Debug);
    }

    private void EnsureConfigured()
    {
        if (_configured)
            return;

        if (_configurationStarted)
            throw new InvalidOperationException("Configuration has already started or failed.");

        Logger.Log("Configuring module.", LogMessageKind.Debug);
        _configurationStarted = true;

        Configure();

        _configured = true;

        Logger.Log("Configuration completed.", LogMessageKind.Debug);
    }

    /// <summary>
    /// Composes the rendering layers managed by this renderer.
    /// </summary>
    /// <returns>
    /// The layers to register, in enumeration order.
    /// </returns>
    /// <remarks>
    /// Composition occurs once when the renderer first starts.
    /// Constructor-provided layers are registered before composed layers.
    /// All composed layers are registered before configuration occurs.
    ///
    /// The renderer owns and destroys its layers.
    ///
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<RenderLayer> Compose()
    {
        yield break;
    }

    /// <summary>
    /// Configures relationships and behavior after composition.
    /// </summary>
    /// <remarks>
    /// All constructor-provided and composed layers are available when this
    /// hook is invoked.
    ///
    /// This hook is invoked at most once, including across later lifecycle cycles.
    /// If configuration throws, later lifecycle calls reject further initialization
    /// rather than repeating configuration side effects.
    /// </remarks>
    protected virtual void Configure() { }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        foreach (var layer in Layers.ToArray())
        {
            layer.Destroy();

            if (InjectedDependencies is not null)
            {
                Logger.Log(
                    $"Destroyed rendering layer '{layer.Identifier}'.",
                    LogMessageKind.Debug
                );
            }
        }

        Layers.Destroy();

        if (InjectedDependencies is not null)
            Logger.Log("Module resources destroyed.");

        base.OnDestroy();
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        Logger.Log("Starting module.");

        EnsureComposed();
        EnsureConfigured();

        Logger.Log($"Module started with {Layers.Count} rendering layers.");
    }

    /// <inheritdoc />
    protected override void OnStop()
    {
        Logger.Log("Module stopped.");
    }
}
