using Mirage.Common.Collections;
using SDL3;

namespace Mirage.Handling.Devices.Keyboard;

/// <summary>
/// Processes SDL keyboard events for a window.
/// </summary>
public class Keyboard : InputDevice
{
    private bool _composed;

    /// <summary>
    /// Gets the keyboard actions registered by identifier.
    /// </summary>
    public readonly ReactiveDictionary<
        string,
        InputEvent<KeyboardEventPayload, KeyboardKey>
    > Events = [];

    /// <summary>
    /// Creates a keyboard with the specified actions.
    /// </summary>
    /// <param name="events">The actions to register initially.</param>
    public Keyboard(params InputEvent<KeyboardEventPayload, KeyboardKey>[] events)
        : base("Keyboard")
    {
        foreach (var @event in events)
            Events.Add(@event.Identifier, @event);
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
                    $"Duplicate keyboard event identifier found: '{@event.Identifier}'."
                );
        }

        foreach (var @event in composedEvents)
            Events.Add(@event.Identifier, @event);

        _composed = true;
    }

    /// <summary>
    /// Composes the events handled by this keyboard.
    /// </summary>
    /// <returns>
    /// An enumerable sequence containing the events to register.
    /// </returns>
    /// <remarks>
    /// Composition occurs once before the keyboard processes input. Events
    /// supplied to the constructor are registered before composed events.
    /// </remarks>
    protected virtual IEnumerable<InputEvent<KeyboardEventPayload, KeyboardKey>> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        foreach (var action in Events.Values)
            action.Destroy();

        Events.Destroy();
    }

    /// <inheritdoc />
    protected override void OnProcess(InputContext context)
    {
        EnsureComposed();

        if (!context.Window.Opened.Get())
            return;

        var windowId = SDL.GetWindowID(context.Window.Native);

        if (windowId == 0)
            throw new InvalidOperationException(
                $"Getting SDL window identifier failed: {SDL.GetError()}"
            );

        foreach (var sdlEvent in context.FrameEvents)
        {
            var type = (SDL.EventType)sdlEvent.Type;

            if (type is not (SDL.EventType.KeyDown or SDL.EventType.KeyUp))
                continue;

            if (sdlEvent.Key.WindowID != windowId)
                continue;

            var key = KeyboardKeyMapper.FromScancode(sdlEvent.Key.Scancode);

            if (key == KeyboardKey.Unknown)
                continue;

            var payload = new KeyboardEventPayload(
                key,
                type == SDL.EventType.KeyDown,
                sdlEvent.Key.Repeat
            );

            foreach (var action in Events.Values)
            {
                if (action.Source.Get() == key)
                    action.Fire(payload);
            }
        }
    }
}
