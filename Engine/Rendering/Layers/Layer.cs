using Mirage.Common.Lifecycle;
using Mirage.Graphics.Interfaces;

namespace Mirage.Rendering;

/// <summary>
/// Represents the rendering priority of a layer.
/// </summary>
public enum RenderLayerPriority
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
/// Groups renderable objects and draws them in a single layer.
/// </summary>
public class RenderLayer : Destroyable
{
    private bool _composed;

    /// <summary>
    /// Gets the renderable objects added directly to this layer.
    /// </summary>
    public readonly List<IDrawable> Entries = [];

    /// <summary>
    /// Gets the unique identifier of this layer.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// Gets the priority that determines when this layer is drawn.
    /// </summary>
    public readonly RenderLayerPriority Priority;

    /// <summary>
    /// Initializes a rendering layer.
    /// </summary>
    /// <param name="identifier">The layer's unique identifier.</param>
    /// <param name="priority">The layer's rendering priority.</param>
    /// <param name="entries">Objects initially contained in the layer.</param>
    public RenderLayer(
        string identifier,
        RenderLayerPriority priority = RenderLayerPriority.Normal,
        IEnumerable<IDrawable>? entries = null
    )
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Layer identifier cannot be empty.", nameof(identifier));

        Identifier = identifier;
        Priority = priority;

        foreach (var entry in entries ?? [])
        {
            ArgumentNullException.ThrowIfNull(entry);
            Entries.Add(entry);
        }
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        foreach (var entry in Compose())
        {
            ArgumentNullException.ThrowIfNull(entry);
            Entries.Add(entry);
        }

        _composed = true;
    }

    /// <summary>
    /// Supplies objects to add to this layer on its first rendering frame.
    /// </summary>
    /// <returns>The objects to add to the layer.</returns>
    protected virtual IEnumerable<IDrawable> Compose()
    {
        yield break;
    }

    /// <summary>
    /// Removes references to objects contained in this layer.
    /// </summary>
    protected override void OnDestroy()
    {
        Entries.Clear();
    }

    /// <summary>
    /// Draws additional content after this layer's entries.
    /// </summary>
    /// <param name="context">The active rendering context.</param>
    protected virtual void OnRender(RenderContext context) { }

    /// <summary>
    /// Draws this layer's entries and any additional content.
    /// </summary>
    /// <param name="context">The active rendering context.</param>
    public void Render(RenderContext context)
    {
        ThrowIfDestroyed();
        ArgumentNullException.ThrowIfNull(context);

        EnsureComposed();

        foreach (var entry in Entries)
            entry.Draw(context);

        OnRender(context);
    }
}
