using System.Numerics;
using Mirage.Common.Events;

namespace Mirage.Handling.Devices.Mouse.Events;

/// <summary>
/// Initializes a new instance of the <see cref="MouseWheelEventPayload"/> class.
/// </summary>
/// <remarks>
/// Describes a mouse wheel movement.
/// </remarks>
/// <param name="Delta">
/// The horizontal and vertical scroll amounts reported by SDL3.
/// </param>
/// <param name="Position">
/// The cursor position within the window when scrolling occurred.
/// </param>
public record MouseWheelEventPayload(Vector2 Delta, Vector2 Position);

/// <summary>
/// Notifies listeners when the mouse wheel moves.
/// </summary>
public class MouseWheelEvent : Signal<MouseWheelEventPayload>;
