using System.Diagnostics;
using Mirage.Common;
using Mirage.Common.Collections;

namespace Mirage.Scheduler;

/// <summary>
/// Manages the update loop and channels for the game.
/// </summary>
public class Scheduler : Module
{
    private bool _composed;

    /// <summary>
    /// Gets the channels managed by the scheduler.
    /// </summary>
    public readonly ReactiveDictionary<string, Channel> Channels = [];

    /// <summary>
    /// Gets the target number of frames per second.
    /// </summary>
    public readonly int TargetFramerate;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheduler"/> class.
    /// </summary>
    /// <param name="targetFramerate">The target number of frames per second.</param>
    /// <param name="channels">The channels managed by the scheduler.</param>
    public Scheduler(int targetFramerate = 60, IEnumerable<Channel>? channels = null)
        : base("Scheduler")
    {
        TargetFramerate = targetFramerate;

        foreach (var channel in channels ?? [])
        {
            if (Channels.ContainsKey(channel.Identifier))
                throw new InvalidOperationException(
                    $"Duplicate channel identifier found: '{channel.Identifier}'"
                );
            Channels.Add(channel.Identifier, channel);
        }
    }

    /// <summary>
    /// Gets the amount of time elapsed since the previous frame, in seconds.
    /// </summary>
    public double DeltaTime { get; private set; }

    /// <summary>
    /// Gets the current measured framerate of the game.
    /// </summary>
    public double Framerate { get; private set; }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        var composedChannels = Compose().ToArray();

        foreach (var channel in composedChannels)
        {
            ArgumentNullException.ThrowIfNull(channel);

            if (Channels.ContainsKey(channel.Identifier))
                throw new InvalidOperationException(
                    $"Duplicate channel identifier found: '{channel.Identifier}'"
                );
            Channels.Add(channel.Identifier, channel);
        }

        _composed = true;
    }

    /// <summary>
    /// Composes the channels managed by this scheduler.
    /// </summary>
    /// <returns>
    /// An enumerable sequence containing the channels to register.
    /// </returns>
    /// <remarks>
    /// The default implementation does not compose any channels. Composition
    /// occurs once when the scheduler starts, before the update loop can run.
    /// Channels supplied to the constructor are registered before composed
    /// channels.
    /// </remarks>
    protected virtual IEnumerable<Channel> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        EnsureComposed();
    }

    /// <summary>
    /// Runs the update loop.
    /// </summary>
    /// <remarks>
    /// The loop measures the elapsed time between frames, updates all channels
    /// according to their priority, and waits for the remaining frame time
    /// required to approach <see cref="TargetFramerate"/>.
    ///
    /// The loop ends when the service state is no longer
    /// <see cref="GameState.Running"/>.
    /// </remarks>
    public void Run()
    {
        var stopwatch = Stopwatch.StartNew();
        var previousFrameTime = stopwatch.Elapsed.TotalSeconds;

        while (State.Get() == ModuleState.Running)
        {
            var frameDuration = TargetFramerate > 0 ? 1.0 / TargetFramerate : 0;

            var frameStartTime = stopwatch.Elapsed.TotalSeconds;

            DeltaTime = frameStartTime - previousFrameTime;
            previousFrameTime = frameStartTime;

            Framerate = DeltaTime > 0 ? 1.0 / DeltaTime : TargetFramerate;

            foreach (var channel in Channels.OrderByDescending(entry => entry.Value.Priority))
            {
                channel.Value.Update((float)DeltaTime);
            }

            var elapsedFrameTime = stopwatch.Elapsed.TotalSeconds - frameStartTime;

            var remainingFrameTime = frameDuration - elapsedFrameTime;

            if (remainingFrameTime > 0)
                Thread.Sleep(TimeSpan.FromSeconds(remainingFrameTime));
        }
    }
}
