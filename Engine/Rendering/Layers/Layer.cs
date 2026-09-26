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
public class DrawLayer : Destroyable
{
    private bool _composed;

    /// <summary>
    /// Gets the renderable objects added directly to this layer.
    /// </summary>
    public readonly List<IDrawable> Entries = [];

    /// <remarks>The layer stores references to its entries but does not own or destroy them.</remarks>
    /// <summary>
    /// Gets the unique identifier of this layer.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// Gets the priority that determines when this layer is drawn.
    /// </summary>
    public readonly DrawLayerPriority Priority;

    /// <summary>
    /// Initializes a rendering layer.
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
    /// <remarks>The default implementation adds no objects.</remarks>
    protected virtual IEnumerable<IDrawable> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    /// <remarks>Clears the entry list without destroying its drawable objects.</remarks>
    protected override void OnDestroy()
    {
        Entries.Clear();
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

        foreach (var entry in Entries)
            entry.Draw(context);

        OnDraw(context);
    }
}
