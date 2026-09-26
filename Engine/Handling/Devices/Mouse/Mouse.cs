using System.Numerics;
using Mirage.Common.Collections;
using Mirage.Common.Events;
using Mirage.Handling.Devices.Mouse.Events;
using SDL3;

namespace Mirage.Handling.Devices.Mouse;

/// <summary>
/// Processes mouse input for a window.
/// </summary>
public class Mouse : InputDevice
{
    private readonly Store<Vector2> _position = new(Vector2.Zero);
    private bool _composed;

    /// <summary>
    /// Gets the registered button actions by identifier.
    /// </summary>
    public readonly ReactiveDictionary<string, MouseButtonEvent> Events = [];

    /// <summary>
    /// Stores the latest mouse movement and notifies listeners of changes.
    /// </summary>
    public readonly MouseMoveEvent OnMove = new();

    /// <summary>
    /// Notifies listeners when the mouse wheel moves.
    /// </summary>
    public readonly MouseWheelEvent OnWheel = new();

    /// <summary>
    /// Gets the latest cursor position in window coordinates.
    /// </summary>
    public readonly IReadOnlyStore<Vector2> Position;

    /// <summary>
    /// Initializes a mouse with the specified button actions.
    /// </summary>
    /// <param name="events">The button actions to register.</param>
    public Mouse(params MouseButtonEvent[] events)
        : base("Mouse")
    {
        foreach (var @event in events)
            Events.Add(@event.Identifier, @event);

        Position = _position;
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        var composedEvents = Compose().ToArray();
        HashSet<string> identifiers = [];

        foreach (var @event in composedEvents)
        {
            ArgumentNullException.ThrowIfNull(@event);

            if (Events.ContainsKey(@event.Identifier) || !identifiers.Add(@event.Identifier))
                throw new InvalidOperationException(
                    $"Duplicate mouse event identifier found: '{@event.Identifier}'."
                );
        }

        foreach (var @event in composedEvents)
            Events.Add(@event.Identifier, @event);

        _composed = true;
    }

    /// <summary>
    /// Composes the button events handled by this mouse.
    /// </summary>
    /// <returns>
    /// An enumerable sequence containing the button events to register.
    /// </returns>
    /// <remarks>
    /// Composition occurs once before the mouse processes input. Events
    /// supplied to the constructor are registered before composed events.
    /// </remarks>
    protected virtual IEnumerable<MouseButtonEvent> Compose()
    {
        yield break;
    }

    private static MouseButton? FromSdlButton(byte button)
    {
        return button switch
        {
            1 => MouseButton.Left,
            2 => MouseButton.Middle,
            3 => MouseButton.Right,
            4 => MouseButton.X1,
            5 => MouseButton.X2,
            _ => null,
        };
    }

    private void ProcessButton(SDL.Event sdlEvent, SDL.EventType type)
    {
        var button = FromSdlButton(sdlEvent.Button.Button);

        if (button is null)
            return;

        var position = new Vector2(sdlEvent.Button.X, sdlEvent.Button.Y);

        _position.Set(position);

        var payload = new MouseButtonEventPayload(
            button.Value,
            type == SDL.EventType.MouseButtonDown,
            sdlEvent.Button.Clicks,
            position
        );

        foreach (var action in Events.Values)
        {
            if (action.Source.Get() == button.Value)
                action.Fire(payload);
        }
    }

    private void ProcessMotion(SDL.Event sdlEvent)
    {
        var position = new Vector2(sdlEvent.Motion.X, sdlEvent.Motion.Y);

        var delta = new Vector2(sdlEvent.Motion.XRel, sdlEvent.Motion.YRel);

        _position.Set(position);
        OnMove.Set(new MouseMoveEventPayload(position, delta));
    }

    private void ProcessWheel(SDL.Event sdlEvent)
    {
        var position = new Vector2(sdlEvent.Wheel.MouseX, sdlEvent.Wheel.MouseY);

        var delta = new Vector2(sdlEvent.Wheel.X, sdlEvent.Wheel.Y);

        _position.Set(position);
        OnWheel.Fire(new MouseWheelEventPayload(delta, position));
    }

    /// <inheritdoc/>
    protected override void OnDestroy()
    {
        foreach (var action in Events.Values)
            action.Destroy();

        Events.Destroy();
        OnMove.Destroy();
        OnWheel.Destroy();
        _position.Destroy();
    }

    /// <inheritdoc/>
    protected override void OnProcess(InputContext context)
    {
        EnsureComposed();

        if (!context.Window.Opened.Get())
            return;

        var windowId = SDL.GetWindowID(context.Window.Native);

        if (windowId == 0)
        {
            throw new InvalidOperationException(
                $"Getting SDL window identifier failed: {SDL.GetError()}"
            );
        }

        foreach (var sdlEvent in context.FrameEvents)
        {
            var type = (SDL.EventType)sdlEvent.Type;

            switch (type)
            {
                case SDL.EventType.MouseButtonDown:
                case SDL.EventType.MouseButtonUp:
                    if (sdlEvent.Button.WindowID == windowId)
                        ProcessButton(sdlEvent, type);

                    break;

                case SDL.EventType.MouseMotion:
                    if (sdlEvent.Motion.WindowID == windowId)
                        ProcessMotion(sdlEvent);

                    break;

                case SDL.EventType.MouseWheel:
                    if (sdlEvent.Wheel.WindowID == windowId)
                        ProcessWheel(sdlEvent);

                    break;
            }
        }
    }
}
