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
    private bool _configured;
    private bool _configurationStarted;

    /// <summary>
    /// Gets the channels managed by the scheduler.
    /// </summary>
    public readonly ReactiveDictionary<string, UpdateChannel> Channels = [];

    /// <summary>
    /// Gets the target number of scheduler iterations per second.
    /// </summary>
    /// <remarks>
    /// A positive value limits the maximum rate of the scheduler and,
    /// consequently, the maximum rate at which any channel can be updated.
    ///
    /// A non-positive value disables scheduler frame pacing and allows the
    /// main loop to run as quickly as possible.
    /// </remarks>
    public readonly int TargetFramerate;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheduler"/> class.
    /// </summary>
    /// <param name="targetFramerate">
    /// The target number of scheduler iterations per second.
    /// The default is <c>60</c>. A non-positive value disables frame pacing.
    /// </param>
    /// <param name="channels">
    /// The initial channels, or <see langword="null"/> for none.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when two initial channels have the same identifier.
    /// </exception>
    public Scheduler(int targetFramerate = 60, IEnumerable<UpdateChannel>? channels = null)
        : base("Scheduler")
    {
        TargetFramerate = targetFramerate;

        foreach (var channel in channels ?? [])
        {
            if (Channels.ContainsKey(channel.Identifier))
            {
                throw new InvalidOperationException(
                    $"Duplicate channel identifier found: '{channel.Identifier}'"
                );
            }

            Channels.Add(channel.Identifier, channel);
        }

        Channels.OnAdd.Connect(entry =>
        {
            if (State.Get() == ModuleState.Running)
                entry.Value.Prepare();
        });
    }

    /// <summary>
    /// Gets the amount of real time elapsed since the previous scheduler
    /// iteration, in seconds.
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    /// Gets the currently measured scheduler iteration rate.
    /// </summary>
    public double Framerate { get; private set; }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        var composedObjects = Compose().ToArray();
        HashSet<string> identifiers = [];

        foreach (var channel in composedObjects)
        {
            if (Channels.ContainsKey(channel.Identifier) || !identifiers.Add(channel.Identifier))
                throw new InvalidOperationException(
                    $"Duplicate channel identifier found: '{channel.Identifier}'."
                );
        }

        foreach (var channel in composedObjects)
            Channels.Add(channel.Identifier, channel);

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
    protected override void OnStart()
    {
        EnsureComposed();
        EnsureConfigured();
        foreach (var channel in Channels.Values)
        {
            channel.Prepare();
        }
        base.OnStart();
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Channels.Destroy();
    }

    /// <summary>
    /// Runs the main scheduling loop.
    /// </summary>
    /// <remarks>
    /// Each iteration measures the real elapsed time and forwards it to every
    /// registered channel in descending priority order.
    ///
    /// A channel may update at most once during each scheduler iteration.
    /// Therefore, the scheduler's own target framerate acts as the maximum
    /// possible update rate for all channels when frame pacing is enabled.
    ///
    /// Channels may independently target a lower update rate.
    ///
    /// When <see cref="TargetFramerate"/> is non-positive, the scheduler runs
    /// without an explicit rate limit.
    ///
    /// The loop ends when the scheduler is no longer running.
    /// </remarks>
    public void Run()
    {
        var stopwatch = Stopwatch.StartNew();
        var previousFrameTime = stopwatch.Elapsed.TotalSeconds;

        while (State.Get() == ModuleState.Running)
        {
            var frameStartTime = stopwatch.Elapsed.TotalSeconds;

            DeltaTime = frameStartTime - previousFrameTime;
            previousFrameTime = frameStartTime;

            Framerate = DeltaTime > 0 ? 1.0 / DeltaTime : 0;

            foreach (var channel in Channels.OrderByDescending(entry => entry.Value.Priority))
            {
                channel.Value.Update(DeltaTime);
            }

            if (TargetFramerate <= 0)
                continue;

            var targetFrameDuration = 1.0 / TargetFramerate;

            var elapsedFrameTime = stopwatch.Elapsed.TotalSeconds - frameStartTime;

            var remainingFrameTime = targetFrameDuration - elapsedFrameTime;

            if (remainingFrameTime > 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(remainingFrameTime));
            }
        }
    }
}
