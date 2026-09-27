namespace Mirage.Scheduling;

/// <summary>
/// Provides timing information for a scheduled update.
/// </summary>
/// <remarks>
/// An update context describes the timing of the channel update currently
/// being executed. Its values belong to the channel rather than necessarily
/// to the scheduler's main loop.
/// </remarks>
public readonly record struct UpdateContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateContext"/> struct.
    /// </summary>
    /// <param name="deltaTime">
    /// The amount of simulated time represented by this update, in seconds.
    /// </param>
    /// <param name="updateRate">
    /// The target number of updates per second for the channel, or
    /// <c>0</c> when the channel updates every scheduler iteration.
    /// </param>
    public UpdateContext(double deltaTime, double updateRate)
    {
        DeltaTime = deltaTime;
        UpdateRate = updateRate;
    }

    /// <summary>
    /// Gets the amount of simulated time represented by this update,
    /// in seconds.
    /// </summary>
    public double DeltaTime { get; }

    /// <summary>
    /// Gets the target number of updates per second for the channel.
    /// </summary>
    /// <remarks>
    /// A value of <c>0</c> indicates that the channel updates every scheduler
    /// iteration instead of targeting a specific update rate.
    /// </remarks>
    public double UpdateRate { get; }
}
