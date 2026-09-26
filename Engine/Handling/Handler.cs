using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Handling.Devices;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing;

namespace Mirage.Handling;

public class InputHandler : Module, IUpdatable
{
    private readonly ReactiveDictionary<string, InputDevice> _devices = [];
    private bool _composed;

    public readonly IReadOnlyReactiveDictionary<string, InputDevice> Devices;
    public readonly Window Window;

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
