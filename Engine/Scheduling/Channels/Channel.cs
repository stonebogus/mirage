using Mirage.Common.Lifecycle;
using Mirage.Scheduling.Interfaces;

namespace Mirage.Scheduling.Channels;

/// <summary>
/// Represents the priority of a channel. Higher priorities run first.
/// </summary>
public enum UpdateChannelPriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority.
    /// </summary>
    Critical,
}

/// <summary>
/// Represents a prioritized collection of updatable entries.
/// </summary>
/// <remarks>The channel clears its entry references when destroyed but does not destroy the entries.</remarks>
public class UpdateChannel : Destroyable
{
    private bool _composed;

    /// <summary>
    /// Gets the updatable entries contained in this channel.
    /// </summary>
    public readonly HashSet<IUpdatable> Entries = [];

    /// <summary>
    /// Gets the unique identifier for this channel.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// Gets the priority of this channel.
    /// </summary>
    public readonly UpdateChannelPriority Priority;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateChannel"/> class.
    /// </summary>
    /// <param name="identifier">The unique identifier for the channel.</param>
    /// <param name="priority">The priority of the channel; the default is <see cref="UpdateChannelPriority.Normal"/>.</param>
    /// <param name="entries">The initial entries, or <see langword="null"/> for none.</param>
    public UpdateChannel(
        string identifier,
        UpdateChannelPriority priority = UpdateChannelPriority.Normal,
        IEnumerable<IUpdatable>? entries = null
    )
    {
        Identifier = identifier;
        Priority = priority;

        foreach (var entry in entries ?? [])
            Entries.Add(entry);
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        var composedEntries = Compose().ToArray();

        foreach (var entry in composedEntries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            Entries.Add(entry);
        }

        _composed = true;
    }

    /// <summary>
    /// Composes the updatable entries contained in this channel.
    /// </summary>
    /// <returns>
    /// An enumerable sequence containing the entries to add to the channel.
    /// </returns>
    /// <remarks>
    /// The default implementation does not compose any entries. Composition
    /// occurs once before the first update.
    /// </remarks>
    protected virtual IEnumerable<IUpdatable> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Entries.Clear();
    }

    /// <summary>
    /// Called after all entries in the channel have been updated.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the previous update, in seconds.</param>
    protected virtual void OnUpdate(double deltaTime) { }

    /// <summary>
    /// Updates all entries in the channel.
    /// </summary>
    /// <param name="deltaTime">The elapsed time since the previous update.</param>
    /// <exception cref="DestroyedObjectException">
    /// Thrown when the channel has already been destroyed.
    /// </exception>
    public void Update(double deltaTime)
    {
        ThrowIfDestroyed();
        EnsureComposed();

        foreach (var entry in Entries)
            entry.Update(deltaTime);

        OnUpdate(deltaTime);
    }
}
