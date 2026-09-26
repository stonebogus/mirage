using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Handling.Devices;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing;

namespace Mirage.Handling;

/// <summary>
/// Registers input devices and updates them with events from one window.
/// </summary>
public class InputHandler : Module, IUpdatable
{
    private readonly ReactiveDictionary<string, InputDevice> _devices = [];
    private bool _composed;

    /// <summary>
    /// Gets the devices owned by this handler, indexed by identifier.
    /// </summary>
    public readonly IReadOnlyReactiveDictionary<string, InputDevice> Devices;

    /// <summary>
    /// Gets the window whose frame events are processed by this handler.
    /// </summary>
    public readonly Window Window;

    /// <summary>
    /// Initializes a handler for the specified window and devices.
    /// </summary>
    /// <param name="index">The index used to distinguish the module identifier.</param>
    /// <param name="window">The window supplying input events.</param>
    /// <param name="devices">The devices to register initially, or <see langword="null"/>.</param>
    public InputHandler(int index, Window window, IEnumerable<InputDevice>? devices = null)
        : base($"InputHandler-{index}")
    {
        Window = window;
        foreach (var device in devices ?? [])
        {
            _devices.Add(device.Identifier, device);
        }
        Devices = _devices;
    }

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        var context = new InputContext(Window, Window.FrameEvents);
        foreach (var device in _devices.Values)
        {
            device.Process(context);
        }
    }

    private void EnsureComposed()
    {
        if (_composed)
            return;

        var composedDevices = Compose().ToArray();
        HashSet<string> identifiers = [];

        foreach (var device in composedDevices)
        {
            identifiers.Add(device.Identifier);
        }

        foreach (var device in composedDevices)
            _devices.Add(device.Identifier, device);

        _composed = true;
    }

    /// <summary>
    /// Composes the devices managed by this handler.
    /// </summary>
    /// <returns>
    /// An enumerable sequence containing the devices to register.
    /// </returns>
    /// <remarks>
    /// Composition occurs once when the handler starts. Devices supplied to
    /// the constructor are registered before composed devices.
    /// </remarks>
    protected virtual IEnumerable<InputDevice> Compose()
    {
        yield break;
    }

    /// <inheritdoc />
    /// <remarks>This handler destroys its registered devices.</remarks>
    protected override void OnDestroy()
    {
        base.OnDestroy();

        foreach (var device in _devices.Values)
            device.Destroy();

        _devices.Destroy();
    }

    /// <inheritdoc />
    protected override void OnStart()
    {
        EnsureComposed();
    }
}
