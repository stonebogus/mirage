using Mirage.Common.Events;

namespace Mirage.Handling.Devices;

public abstract class InputEvent<TPayload> : Signal<TPayload>
{
    public readonly string Identifier;
    public readonly Store<string> Source;

    protected InputEvent(string identifier, string source)
    {
        Identifier = identifier;
        Source = new Store<string>(source);
    }
}
