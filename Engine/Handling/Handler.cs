using Mirage.Common;
using Mirage.Common.Collections;
using Mirage.Handling.Devices;
using Mirage.Scheduling.Interfaces;
using Mirage.Windowing;

namespace Mirage.Handling;

public class InputHandler : Module, IUpdatable
{
    private readonly ReactiveDictionary<string, InputDevice> _devices = [];
    private readonly Window _window;

    public InputHandler(Window window)
        : base("InputHandler")
    {
        ArgumentNullException.ThrowIfNull(window);
        _window = window;
    }

    public IReadOnlyReactiveDictionary<string, InputDevice> Devices => _devices;

    public void Update(double deltaTime)
    {
        ThrowIfDestroyed();
        _window.Process();
    }

    protected override void OnDestroy()
    {
        foreach (var device in _devices.Values)
            device.Destroy();

        _devices.Destroy();
    }
}
