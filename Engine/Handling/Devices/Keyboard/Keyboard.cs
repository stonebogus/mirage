using Mirage.Common.Collections;

namespace Mirage.Handling.Devices.Keyboard;

public sealed class Keyboard : InputDevice
{
    public readonly ReactiveDictionary<string, KeyboardKeyEvent> Keys = [];

    public Keyboard()
        : base("Keyboard") { }

    protected override void OnDestroy()
    {
        Keys.Destroy();
        base.OnDestroy();
    }

    public void AddKey(KeyboardKeyEvent key)
    {
        Keys.Add(key.Identifier, key);
    }
}
