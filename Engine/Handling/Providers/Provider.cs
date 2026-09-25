using Mirage.Common.Collections;
using Mirage.Common.Lifecycle;
using Mirage.Handling.Devices;
using Mirage.Scheduling.Interfaces;

namespace Mirage.Handling.Providers;

public abstract class InputProvider : Destroyable, IUpdatable
{
    public abstract void Update(double deltaTime);

    protected void AddDevice(InputDevice device)
    {
        _devices.Add(device.Identifier, device);
    }

    protected override void OnDestroy()
    {
        foreach (var device in _devices.Values())
            device.Destroy();

        _devices.Destroy();
        base.OnDestroy();
    }
}
