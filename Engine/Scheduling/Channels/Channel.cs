using Mirage.Common.Lifecycle;
using Mirage.Scheduling.Interfaces;

namespace Mirage.Scheduling.Channels;

/// <summary>
/// Represents the priority of an update channel.
/// Higher priorities are processed first.
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
/// Represents a prioritized collection of objects updated at a configurable
/// rate.
/// </summary>
/// <remarks>
/// A channel may impose its own target update rate, but it can never execute
/// more than once during a single scheduler iteration.
///
/// Consequently, the effective update rate of a channel cannot exceed the
/// rate at which its scheduler is running.
///
/// A non-positive <see cref="UpdateRate"/> disables the channel's own rate
/// limit, causing it to update once during every scheduler iteration.
///
/// Destroying a channel clears its entry references but does not destroy the
/// entries themselves.
/// </remarks>
public class UpdateChannel : Destroyable
{
    private double _accumulator;
    private bool _composed;

    /// <summary>
    /// Gets the updatable entries contained in this channel.
    /// </summary>
    public readonly HashSet<IUpdatable> Entries = [];

    /// <summary>
    /// Gets the unique identifier of this channel.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// Gets the priority used to order this channel relative to other channels.
    /// </summary>
    public readonly UpdateChannelPriority Priority;

    /// <summary>
    /// Gets the target number of updates per second.
    /// </summary>
    /// <remarks>
    /// A positive value limits how frequently this channel updates.
    ///
    /// A non-positive value disables the channel's own rate limit, causing it
    /// to update once per scheduler iteration.
    ///
    /// The channel can never update more frequently than the scheduler that
    /// drives it.
    /// </remarks>
    public readonly double UpdateRate;

    /// <summary>
    /// Initializes an update channel.
    /// </summary>
    /// <param name="identifier">
    /// The unique identifier of the channel.
    /// </param>
    /// <param name="updateRate">
    /// The target number of updates per second.
    /// A non-positive value disables the channel's own rate limit.
    /// </param>
    /// <param name="priority">
    /// The priority of the channel.
    /// </param>
    /// <param name="entries">
    /// The initial entries, or <see langword="null"/> for none.
    /// </param>
    public UpdateChannel(
        string identifier,
        double updateRate = 0,
        UpdateChannelPriority priority = UpdateChannelPriority.Normal,
        IEnumerable<IUpdatable>? entries = null
    )
    {
        Identifier = identifier;
        UpdateRate = updateRate;
        Priority = priority;

        foreach (var entry in entries ?? [])
            Entries.Add(entry);
    }

    /// <summary>
    /// Ensures that entries provided through <see cref="Compose"/> have been
    /// registered.
    /// </summary>
    private void EnsureComposed()
    {
        if (_composed)
            return;

        foreach (var entry in Compose())
            Entries.Add(entry);

        _composed = true;
    }

    /// <summary>
    /// Performs one channel update.
    /// </summary>
    /// <param name="context">
    /// The timing information associated with the update.
    /// </param>
    private void PerformUpdate(UpdateContext context)
    {
        foreach (var entry in Entries)
            entry.Update(context);

        OnUpdate(context);
    }

    /// <summary>
    /// Composes the updatable entries contained in this channel.
    /// </summary>
    /// <returns>
    /// An enumerable sequence containing the entries to register.
    /// </returns>
    /// <remarks>
    /// The default implementation does not compose any entries.
    /// Composition occurs once before the channel performs its first update.
    /// </remarks>
    protected virtual IEnumerable<IUpdatable> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        Entries.Clear();

        _accumulator = 0;

        base.OnDestroy();
    }

    /// <summary>
    /// Called after all entries have been updated for a channel tick.
    /// </summary>
    /// <param name="context">
    /// The timing information associated with the update.
    /// </param>
    protected virtual void OnUpdate(UpdateContext context) { }

    /// <summary>
    /// Receives elapsed scheduler time and updates this channel when required.
    /// </summary>
    /// <param name="deltaTime">
    /// The real elapsed time since the previous scheduler iteration, in
    /// seconds.
    /// </param>
    /// <remarks>
    /// A channel without its own update limit executes once per scheduler
    /// iteration using the supplied delta time.
    ///
    /// A rate-limited channel accumulates elapsed time until its target
    /// interval has been reached. It then performs one update using the real
    /// accumulated elapsed time.
    ///
    /// A channel never performs more than one update during a single scheduler
    /// iteration.
    /// </remarks>
    /// <exception cref="DestroyedObjectException">
    /// Thrown when the channel has already been destroyed.
    /// </exception>
    public void Update(double deltaTime)
    {
        ThrowIfDestroyed();
        EnsureComposed();

        if (UpdateRate <= 0)
        {
            PerformUpdate(new UpdateContext(deltaTime, 0));

            return;
        }

        _accumulator += deltaTime;

        var updateInterval = 1.0 / UpdateRate;

        if (_accumulator < updateInterval)
            return;

        var elapsedTime = _accumulator;

        _accumulator = 0;

        PerformUpdate(new UpdateContext(elapsedTime, UpdateRate));
    }
}
