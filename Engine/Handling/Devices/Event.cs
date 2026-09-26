using Mirage.Common.Events;

namespace Mirage.Handling.Devices;

public abstract class InputEvent<TPayload, TSource> : Signal<TPayload>
{
    public readonly string Identifier;
    public readonly Store<TSource> Source;

    protected InputEvent(string identifier, TSource source)
    {
        Identifier = identifier;
        Source = new Store<TSource>(source);
    }

    /// <inheritdoc/>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Source.Destroy();
    }
}
