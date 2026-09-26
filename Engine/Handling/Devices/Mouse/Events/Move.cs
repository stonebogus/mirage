using System.Numerics;
using Mirage.Common.Events;

namespace Mirage.Handling.Devices.Mouse.Events;

/// <summary>
/// Describes a change in the cursor position.
/// </summary>
/// <param name="Position">
/// The cursor position within the window after the movement.
/// </param>
/// <param name="Delta">
/// The movement relative to the previous mouse motion event.
/// </param>
public sealed record MouseMoveEventPayload(Vector2 Position, Vector2 Delta);

/// <summary>
/// Stores the latest mouse movement and notifies listeners when it changes.
/// </summary>
public sealed class MouseMoveEvent()
    : Store<MouseMoveEventPayload>(new MouseMoveEventPayload(Vector2.Zero, Vector2.Zero));
