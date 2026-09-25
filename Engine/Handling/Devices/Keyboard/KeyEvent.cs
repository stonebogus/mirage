namespace Mirage.Handling.Devices.Keyboard;

public record class KeyboardEventPayload { }

public class KeyboardKeyEvent(string identifier, string source)
    : InputEvent<KeyboardKeyEvent>(identifier, source) { }
