using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Logging;
using Mirage.Rendering.Spaces;
using Mirage.Scheduling;
using Mirage.Scheduling.Interfaces;

namespace Mirage.Rendering;

/// <summary>
/// Manages the rendering spaces used to produce frames.
/// </summary>
/// <remarks>
/// The renderer owns and destroys its registered spaces.
/// Rendering infrastructure is provided separately by the active rendering implementation.
/// </remarks>
public class Renderer : Module, IUpdatable
{
    private bool _composed;
    private bool _compositionStarted;
    private bool _configurationStarted;
    private bool _configured;

    /// <summary>
    /// Gets the identifiable set of rendering spaces managed by the renderer.
    /// </summary>
    /// <remarks>
    /// The renderer owns and destroys its registered spaces.
    /// Registration transfers ownership to the renderer. Removing or clearing
    /// entries returns ownership to the caller without destroying them.
    /// Do not register an object owned elsewhere.
    /// </remarks>
    public readonly IdentifiableSet<string, RenderSpace> Spaces = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Renderer"/> class.
    /// </summary>
    /// <param name="spaces">
    /// The initial rendering spaces, or <see langword="null"/> for no spaces.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the initial spaces contain duplicate identifiers.
    /// </exception>
    public Renderer(IEnumerable<RenderSpace>? spaces = null)
        : base("Renderer", ["Logger"])
    {
        foreach (var space in spaces ?? [])
            Spaces.Add(space);
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

        foreach (var space in Compose())
        {
            try
            {
                Spaces.Add(space);
            }
            catch
            {
                if (!Spaces.Contains(space) && !space.Destroyed)
                    space.Destroy();

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
    /// Composes the rendering spaces managed by this renderer.
    /// </summary>
    /// <returns>
    /// The spaces to register, in enumeration order.
    /// </returns>
    /// <remarks>
    /// Composition occurs once when the renderer first starts.
    /// Constructor-provided spaces are registered before composed spaces.
    /// All composed spaces are registered before configuration occurs.
    ///
    /// The renderer owns and destroys its spaces.
    ///
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<RenderSpace> Compose()
    {
        yield break;
    }

    /// <summary>
    /// Configures relationships and behavior after composition.
    /// </summary>
    /// <remarks>
    /// All constructor-provided and composed spaces are available when this
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
        foreach (var space in Spaces.ToArray())
        {
            space.Destroy();

            if (InjectedDependencies is not null)
            {
                Logger.Log(
                    $"Destroyed rendering space '{space.Identifier}'.",
                    LogMessageKind.Debug
                );
            }
        }

        Spaces.Destroy();

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

        Logger.Log($"Module started with {Spaces.Count} rendering spaces.");
    }

    /// <inheritdoc />
    protected override void OnStop()
    {
        Logger.Log("Module stopped.");
    }
}
