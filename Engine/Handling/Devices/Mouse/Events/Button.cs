using System.Numerics;

namespace Mirage.Handling.Devices.Mouse.Events;

/// <summary>
/// Describes a mouse button press or release.
/// </summary>
/// <param name="Button">The button that changed state.</param>
/// <param name="Down">
/// Whether the button was pressed; <see langword="false"/> means it was released.
/// </param>
/// <param name="Clicks">The number of consecutive clicks reported for the event.</param>
/// <param name="Position">
/// The cursor position within the window when the event occurred.
/// </param>
public sealed record MouseButtonEventPayload(
    MouseButton Button,
    bool Down,
    int Clicks,
    Vector2 Position
);

/// <summary>
/// Represents an action triggered by a mouse button.
/// </summary>
/// <param name="identifier">The stable action identifier.</param>
/// <param name="source">The button that activates this action.</param>
public sealed class MouseButtonEvent(string identifier, MouseButton source)
    : InputEvent<MouseButtonEventPayload, MouseButton>(identifier, source);
