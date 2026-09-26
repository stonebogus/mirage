using System.Numerics;
using Mirage.Common.Events;

namespace Mirage.Handling.Devices.Mouse.Events;

/// <summary>
/// Describes a mouse wheel movement.
/// </summary>
/// <param name="Delta">
/// The horizontal and vertical scroll amounts reported by SDL3.
/// </param>
/// <param name="Position">
/// The cursor position within the window when scrolling occurred.
/// </param>
public sealed record MouseWheelEventPayload(Vector2 Delta, Vector2 Position);

/// <summary>
/// Notifies listeners when the mouse wheel moves.
/// </summary>
public sealed class MouseWheelEvent : Signal<MouseWheelEventPayload>;
