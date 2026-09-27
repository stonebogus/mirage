using Mirage.Common.Collections;
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
    private bool _compositionStarted;
    private bool _configured;
    private bool _configurationStarted;

    /// <summary>
    /// Gets the updatable entries contained in this channel.
    /// </summary>
    public readonly ReactiveSet<IUpdatable> Entries = [];

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
    /// Initializes a new instance of a <see cref="UpdateChannel"/>.
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
        {
            ArgumentNullException.ThrowIfNull(entry);
            Entries.Add(entry);
        }
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        var composedObjects = Compose().ToArray();
        HashSet<IUpdatable> objects = [];

        foreach (var entry in composedObjects)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (Entries.Contains(entry) || !objects.Add(entry))
                throw new InvalidOperationException("Duplicate composed object found.");
        }

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

    private void PerformUpdate(UpdateContext context)
    {
        foreach (var entry in Entries)
            entry.Update(context);

        OnUpdate(context);
    }

    /// <summary>
    /// Composes the entries managed by this object.
    /// </summary>
    /// <returns>The entries to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once when prepared by the scheduler or before the first update.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The channel references entries without owning or destroying them.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<IUpdatable> Compose()
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
        Entries.Destroy();

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
        EnsureConfigured();

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

    internal void Prepare()
    {
        ThrowIfDestroyed();
        EnsureComposed();
        EnsureConfigured();
    }
}
