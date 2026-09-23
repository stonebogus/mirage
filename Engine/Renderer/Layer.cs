using Mirage.Common.Lifecycle;
using Mirage.Graphics.Interfaces;

namespace Mirage.Renderer;

/// <summary>
/// Represents the rendering priority of a layer.
/// </summary>
public enum LayerPriority
{
    /// <summary>
    /// Low rendering priority.
    /// </summary>
    Low,

    /// <summary>
    /// Normal rendering priority.
    /// </summary>
    Normal,

    /// <summary>
    /// High rendering priority.
    /// </summary>
    High,

    /// <summary>
    /// Critical rendering priority.
    /// </summary>
    Critical,
}

/// <summary>
/// Represents a prioritized collection of renderable entries.
/// </summary>
public class Layer : Destroyable
{
    private bool _composed;

    /// <summary>
    /// Gets the renderable entries contained in this layer.
    /// </summary>
    public readonly List<IRenderable> Entries = [];

    /// <summary>
    /// Gets the unique identifier of this layer.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// Gets the rendering priority of this layer.
    /// </summary>
    public readonly LayerPriority Priority;

    /// <summary>
    /// Initializes a new rendering layer.
    /// </summary>
    /// <param name="identifier">
    /// The unique identifier of the layer.
    /// </param>
    /// <param name="priority">
    /// The rendering priority of the layer.
    /// </param>
    /// <param name="entries">
    /// The initial renderable entries contained in the layer.
    /// </param>
    public Layer(
        string identifier,
        LayerPriority priority = LayerPriority.Normal,
        IEnumerable<IRenderable>? entries = null
    )
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Layer identifier cannot be empty.", nameof(identifier));
        }

        Identifier = identifier;
        Priority = priority;

        foreach (var entry in entries ?? [])
            Entries.Add(entry);
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        foreach (var entry in Compose().ToArray())
            Entries.Add(entry);

        _composed = true;
    }

    /// <summary>
    /// Composes the renderable entries contained in this layer.
    /// </summary>
    /// <returns>
    /// The entries to add to the layer.
    /// </returns>
    protected virtual IEnumerable<IRenderable> Compose()
    {
        yield break;
    }

    /// <summary>
    /// Called after the layer has collected its entries.
    /// </summary>
    /// <param name="entries">
    /// The collected entries.
    /// </param>
    protected virtual void OnCollect(IReadOnlyList<IRenderable> entries) { }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Entries.Clear();
    }

    /// <summary>
    /// Collects the renderable entries contained in this layer.
    /// </summary>
    /// <returns>
    /// The entries in their rendering order.
    /// </returns>
    public IReadOnlyList<IRenderable> Collect()
    {
        ThrowIfDestroyed();
        EnsureComposed();

        OnCollect(Entries);

        return Entries;
    }
}
