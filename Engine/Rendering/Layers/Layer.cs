using Mirage.Common.Interfaces;
using Mirage.Common.Lifecycle;
using Mirage.Graphics.Interfaces;

namespace Mirage.Rendering;

/// <summary>
/// Represents the rendering priority of a layer.
/// </summary>
public enum DrawLayerPriority
{
    /// <summary>Drawn before normal-priority layers.</summary>
    Low,

    /// <summary>The default rendering priority.</summary>
    Normal,

    /// <summary>Drawn after normal-priority layers.</summary>
    High,

    /// <summary>Drawn after all other priorities.</summary>
    Critical,
}

/// <summary>
/// Groups drawable objects and draws them in a single layer.
/// </summary>
public class DrawLayer : Destroyable, IIdentifiable<string>
{
    private bool _composed;
    private bool _compositionStarted;
    private bool _configurationStarted;
    private bool _configured;

    /// <summary>
    /// Gets the renderable objects added directly to this layer.
    /// </summary>
    /// <remarks>The layer references its entries without owning or destroying them.</remarks>
    public readonly List<IDrawable> Entries = [];

    /// <summary>
    /// Gets the priority that determines when this layer is drawn.
    /// </summary>
    public readonly DrawLayerPriority Priority;

    /// <summary>
    /// Initializes a new instance of the <see cref="DrawLayer"/> class.
    /// </summary>
    /// <param name="identifier">The layer's unique identifier.</param>
    /// <param name="priority">The rendering priority. The default is <see cref="DrawLayerPriority.Normal"/>.</param>
    /// <param name="entries">The initial drawable references, or <see langword="null"/> for none.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="identifier"/> is empty or whitespace.
    /// </exception>
    public DrawLayer(
        string identifier,
        DrawLayerPriority priority = DrawLayerPriority.Normal,
        IEnumerable<IDrawable>? entries = null
    )
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Layer identifier cannot be empty.", nameof(identifier));

        Identifier = identifier;
        Priority = priority;

        foreach (var entry in entries ?? [])
        {
            Entries.Add(entry);
        }
    }

    /// <inheritdoc />
    public string Identifier { get; }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        var composedObjects = Compose().ToArray();

        foreach (var entry in composedObjects)
            Entries.Add(entry);

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
    /// Composes the entries managed by this object.
    /// </summary>
    /// <returns>The entries to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once before the first draw.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The layer borrows entries, including those returned here, because drawable scene objects
    /// have independent owners and may be drawn by multiple layers. Return objects owned elsewhere;
    /// composition does not transfer their lifetime to this processing collection.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<IDrawable> Compose()
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
    /// <remarks>Clears the entry list without destroying its drawable objects.</remarks>
    protected override void OnDestroy()
    {
        Entries.Clear();

        base.OnDestroy();
    }

    /// <summary>
    /// Draws additional content after this layer's entries.
    /// </summary>
    /// <param name="context">The active rendering context.</param>
    protected virtual void OnDraw(RenderContext context) { }

    /// <summary>
    /// Draws this layer's entries and any additional content.
    /// </summary>
    /// <param name="context">The active rendering context.</param>
    /// <exception cref="DestroyedObjectException">
    /// Thrown when this layer has been destroyed.
    /// </exception>
    public void Draw(RenderContext context)
    {
        ThrowIfDestroyed();

        EnsureComposed();
        EnsureConfigured();

        foreach (var entry in Entries)
            entry.Draw(context);

        OnDraw(context);
    }
}
