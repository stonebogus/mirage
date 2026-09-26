namespace Mirage.Handling.Devices.Keyboard;

/// <summary>
/// Describes a keyboard action triggered by a physical key.
/// </summary>
public sealed record KeyboardEventPayload(KeyboardKey Key, bool Down, bool Repeat);

public class KeyboardEvent(string identifier, KeyboardKey source)
    : InputEvent<KeyboardEventPayload, KeyboardKey>(identifier, source) { }
