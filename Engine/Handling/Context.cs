using Mirage.Windowing;
using SDL3;

namespace Mirage.Handling;

/// <summary>
/// Carries the window and SDL events available to input devices for one update.
/// </summary>
public class InputContext
{
    /// <summary>
    /// Gets the events captured for the current window frame.
    /// </summary>
    public readonly IReadOnlyList<SDL.Event> FrameEvents;

    /// <summary>
    /// Gets the window whose events are being processed.
    /// </summary>
    public readonly Window Window;

    /// <summary>
    /// Creates a snapshot of the events available to input devices.
    /// </summary>
    /// <param name="window">The window receiving the events.</param>
    /// <param name="events">The events captured for the current frame.</param>
    public InputContext(Window window, IEnumerable<SDL.Event> events)
    {
        Window = window;
        FrameEvents = [.. events];
    }
}
