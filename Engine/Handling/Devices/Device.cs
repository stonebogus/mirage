using Mirage.Common.Collections;
using Mirage.Common.Lifecycle;

namespace Mirage.Handling.Devices;

/// <summary>
/// Provides a lifecycle and processing hook for a window input source.
/// </summary>
/// <param name="identifier">The stable identifier of this device.</param>
public abstract class InputDevice(string identifier) : Destroyable
{
    /// <summary>
    /// Gets the stable identifier used to register this device.
    /// </summary>
    public string Identifier { get; } = identifier;

    /// <summary>
    /// Processes input from the supplied frame context.
    /// </summary>
    /// <param name="context">The window and events available for this update.</param>
    protected virtual void OnProcess(InputContext context) { }

    /// <summary>
    /// Processes this device once and dispatches its input events.
    /// </summary>
    /// <param name="context">The window and events available for this update.</param>
    /// <exception cref="DestroyedObjectException">
    /// Thrown when this device has been destroyed.
    /// </exception>
    public void Process(InputContext context)
    {
        ThrowIfDestroyed();
        OnProcess(context);
    }
}
