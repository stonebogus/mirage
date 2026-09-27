using Mirage.Windowing;
using SDL3;

namespace Mirage.Handling;

/// <summary>
/// Initializes a new instance of the <see cref="InputContext"/> class.
/// </summary>
/// <remarks>
/// Carries the window and SDL events available to input devices for one update.
/// </remarks>
/// <param name="window">The window receiving the events.</param>
/// <param name="events">The events captured for the current frame.</param>
public class InputContext(Window window, IEnumerable<SDL.Event> events)
{
    /// <summary>
    /// Gets the events captured for the current window frame.
    /// </summary>
    public readonly IReadOnlyList<SDL.Event> FrameEvents = [.. events];

    /// <summary>
    /// Gets the window whose events are being processed.
    /// </summary>
    public readonly Window Window = window;
}
