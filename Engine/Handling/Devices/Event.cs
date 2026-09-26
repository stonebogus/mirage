using Mirage.Common.Events;

namespace Mirage.Handling.Devices;

/// <summary>
/// Associates an input source with a signal that publishes its payloads.
/// </summary>
/// <typeparam name="TPayload">The payload delivered to subscribers.</typeparam>
/// <typeparam name="TSource">The type identifying the source of this action.</typeparam>
public abstract class InputEvent<TPayload, TSource> : Signal<TPayload>
{
    /// <summary>
    /// Gets the stable identifier used to register this action.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// Gets the reactive source associated with this action.
    /// </summary>
    public readonly Store<TSource> Source;

    /// <summary>
    /// Creates an action associated with the specified source.
    /// </summary>
    /// <param name="identifier">The stable registration identifier.</param>
    /// <param name="source">The source that activates this action.</param>
    protected InputEvent(string identifier, TSource source)
    {
        Identifier = identifier;
        Source = new Store<TSource>(source);
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Source.Destroy();
    }
}
