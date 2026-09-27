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
    private bool _compositionStarted;
    private bool _configured;
    private bool _configurationStarted;

    /// <summary>
    /// Gets the identifiable set of registered button actions.
    /// </summary>
    public readonly IdentifiableSet<string, MouseButtonEvent> Events = [];

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
    /// Initializes a new instance of the <see cref="Mouse"/> class.
    /// </summary>
    /// <param name="events">The button actions to register.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the initial events contain duplicate identifiers.
    /// </exception>
    public Mouse(params MouseButtonEvent[] events)
        : base("Mouse")
    {
        foreach (var @event in events)
        {
            Events.Add(@event);
        }

        Position = _position;
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        if (_compositionStarted)
            throw new InvalidOperationException("Composition has already started or failed.");

        _compositionStarted = true;

        Events.Add(Compose().ToArray());

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
    /// The mouse owns and destroys its registered events.
    /// If composition fails, later lifecycle calls reject further initialization.
    /// </remarks>
    protected virtual IEnumerable<MouseButtonEvent> Compose()
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

        foreach (var action in Events)
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

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        foreach (var action in Events)
            action.Destroy();

        Events.Destroy();
        OnMove.Destroy();
        OnWheel.Destroy();
        _position.Destroy();
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
