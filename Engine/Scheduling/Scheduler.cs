using System.Diagnostics;
using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Scheduling.Channels;

namespace Mirage.Scheduling;

/// <summary>
/// Manages the main update loop and its update channels.
/// </summary>
/// <remarks>
/// The scheduler defines the maximum rate at which the main update loop runs.
/// Each registered <see cref="UpdateChannel"/> may optionally impose a lower
/// update rate on its own entries.
/// </remarks>
public class Scheduler : Module
{
    private bool _composed;
    private bool _compositionStarted;
    private bool _configurationStarted;
    private bool _configured;

    /// <summary>
    /// Gets the identifiable set of channels managed by the scheduler.
    /// </summary>
    public readonly IdentifiableSet<string, UpdateChannel> Channels = [];

    /// <summary>
    /// Gets the target number of scheduler iterations per second.
    /// </summary>
    /// <remarks>
    /// A positive value limits the maximum rate of the scheduler and,
    /// consequently, the maximum rate at which any channel can be updated.
    ///
    /// A non-positive value disables scheduler iteration pacing and allows the
    /// main loop to run as quickly as possible.
    /// </remarks>
    public readonly int TargetUpdateRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheduler"/> class.
    /// </summary>
    /// <param name="targetUpdateRate">
    /// The target number of scheduler iterations per second.
    /// The default is <c>60</c>. A non-positive value disables iteration pacing.
    /// </param>
    /// <param name="channels">
    /// The initial channels, or <see langword="null"/> for none.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when two initial channels have the same identifier.
    /// </exception>
    public Scheduler(int targetUpdateRate = 60, IEnumerable<UpdateChannel>? channels = null)
        : base("Scheduler")
    {
        TargetUpdateRate = targetUpdateRate;

        foreach (var channel in channels ?? [])
        {
            Channels.Add(channel);
        }

        Channels.OnAdd.Connect(channel =>
        {
            if (State.Get() == ModuleState.Running)
                channel.Prepare();
        });
    }

    /// <summary>
    /// Gets the amount of real time elapsed since the previous scheduler
    /// iteration, in seconds.
    /// </summary>
    public double DeltaTime { get; private set; }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        Channels.Add([.. Compose()]);

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
    /// Composes the channels managed by this object.
    /// </summary>
    /// <returns>The channels to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once when the scheduler first starts, before channels are prepared.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The scheduler references channels without owning or destroying them.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<UpdateChannel> Compose()
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
        Channels.Destroy();
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        EnsureComposed();
        EnsureConfigured();
        foreach (var channel in Channels)
        {
            channel.Prepare();
        }
        base.OnStart();
    }

    /// <summary>
    /// Runs the main scheduling loop.
    /// </summary>
    /// <remarks>
    /// Each iteration measures the real elapsed time and forwards it to every
    /// registered channel in descending priority order.
    ///
    /// A channel may update at most once during each scheduler iteration.
    /// Therefore, the scheduler's own target update rate acts as the maximum
    /// possible update rate for all channels when iteration pacing is enabled.
    ///
    /// Each channel uses the lower positive target configured by itself and the
    /// scheduler, and reports that effective target in its update context.
    /// Non-positive targets impose no limit.
    ///
    /// When <see cref="TargetUpdateRate"/> is non-positive, the scheduler runs
    /// without an explicit rate limit.
    ///
    /// The loop ends when the scheduler is no longer running.
    /// </remarks>
    public void Run()
    {
        var stopwatch = Stopwatch.StartNew();
        var previousIterationTime = stopwatch.Elapsed.TotalSeconds;

        while (State.Get() == ModuleState.Running)
        {
            var iterationStartTime = stopwatch.Elapsed.TotalSeconds;

            DeltaTime = iterationStartTime - previousIterationTime;
            previousIterationTime = iterationStartTime;

            foreach (var channel in Channels.OrderByDescending(channel => channel.Priority))
            {
                channel.Update(DeltaTime, TargetUpdateRate);
            }

            if (TargetUpdateRate <= 0)
                continue;

            var targetIterationDuration = 1.0 / TargetUpdateRate;

            var elapsedIterationTime = stopwatch.Elapsed.TotalSeconds - iterationStartTime;

            var remainingIterationTime = targetIterationDuration - elapsedIterationTime;

            while (remainingIterationTime > 0)
            {
                // Sleep may return before the target interval, especially for sub-millisecond waits.
                if (remainingIterationTime >= 0.001)
                    Thread.Sleep(TimeSpan.FromSeconds(remainingIterationTime));
                else
                    Thread.SpinWait(1);

                elapsedIterationTime = stopwatch.Elapsed.TotalSeconds - iterationStartTime;
                remainingIterationTime = targetIterationDuration - elapsedIterationTime;
            }
        }
    }
}
