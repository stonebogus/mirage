using Mirage.Common.Collections;
using SDL3;

namespace Mirage.Handling.Devices.Keyboard;

/// <summary>
/// Processes SDL keyboard events for a window.
/// </summary>
public class Keyboard : InputDevice
{
    private bool _composed;
    private bool _compositionStarted;
    private bool _configured;
    private bool _configurationStarted;

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
        {
            Events.Add(@event.Identifier, @event);
        }
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        var composedObjects = Compose().ToArray();
        HashSet<string> identifiers = [];

        foreach (var @event in composedObjects)
        {
            if (Events.ContainsKey(@event.Identifier) || !identifiers.Add(@event.Identifier))
                throw new InvalidOperationException(
                    $"Duplicate event identifier found: '{@event.Identifier}'."
                );
        }

        foreach (var @event in composedObjects)
            Events.Add(@event.Identifier, @event);

        _composed = true;
    }

    private void EnsureConfigured()
    {
        if (_configured)
            return;

        if (_configurationStarted)
            throw new InvalidOperationException("Configuration has already started or failed.");

        _configurationStarted = true;
        Configure();
        _configured = true;
    }

    /// <summary>
    /// Composes the events managed by this object.
    /// </summary>
    /// <returns>The events to register, in enumeration order.</returns>
    /// <remarks>
    /// Composition occurs once before the first input processing call.
    /// Constructor-provided objects are registered before composed objects.
    /// All composed objects are registered before configuration occurs.
    /// The keyboard owns and destroys its registered events.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<InputEvent<KeyboardEventPayload, KeyboardKey>> Compose()
    {
        yield break;
    }

    /// <summary>
    /// Configures relationships and behavior after composition, before startup or first use.
    /// </summary>
    /// <remarks>
    /// All constructor-provided and composed objects are available here.
    /// This hook is invoked at most once, including across later lifecycle cycles.
    /// If configuration throws, later lifecycle calls reject further initialization
    /// rather than repeating configuration side effects.
    /// </remarks>
    protected virtual void Configure() { }

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
        EnsureConfigured();

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
